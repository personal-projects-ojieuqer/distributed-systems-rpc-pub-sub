using MySql.Data.MySqlClient;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace agregators
{
    /// <summary>
    /// Responsável por subscrever tópicos do RabbitMQ e processar dados de sensores recebidos,
    /// armazenando-os em base de dados e utilizando gRPC para validação.
    /// </summary>
    public class RabbitSubscriber
    {
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly string connString;
        private readonly string aggregatorId;
        private readonly string subscriptionKey;
        private readonly GrpcClient grpcClient;

        /// <summary>
        /// Construtor que estabelece ligação ao RabbitMQ e prepara o canal de consumo.
        /// </summary>
        /// <param name="aggregatorId">Identificador único do agregador.</param>
        /// <param name="connString">String de ligação à base de dados MySQL.</param>
        /// <param name="subscriptionKey">Chaves de subscrição (routing keys) separadas por vírgulas.</param>
        /// <param name="grpcClient">Instância de cliente gRPC usada para validação dos dados.</param>
        public RabbitSubscriber(string aggregatorId, string connString, string subscriptionKey, GrpcClient grpcClient)
        {
            this.aggregatorId = aggregatorId;
            this.connString = connString;
            this.subscriptionKey = subscriptionKey;
            this.grpcClient = grpcClient;

            var factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                Port = 5672,
                UserName = "sdtp2",
                Password = "sdtp2",
                DispatchConsumersAsync = true
            };

            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            // Declaração do exchange e configuração de pré-busca (prefetch)
            channel.ExchangeDeclare(exchange: "sensores", type: ExchangeType.Topic, durable: true);
            channel.BasicQos(0, 10, false);
        }

        /// <summary>
        /// Inicia a escuta das mensagens no RabbitMQ e o processamento assíncrono das mesmas.
        /// </summary>
        public void Start()
        {
            string queueName = $"queue_{aggregatorId.ToLower()}";

            // Declaração da fila e ligação a cada routing key
            channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
            foreach (var key in subscriptionKey.Split(','))
            {
                channel.QueueBind(queue: queueName, exchange: "sensores", routingKey: key.Trim());
            }

            Console.WriteLine($"[{aggregatorId}] Subscrito a {subscriptionKey}");

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.Received += async (model, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                try
                {
                    var parts = message.Split(';');
                    if (parts.Length != 4)
                    {
                        Console.WriteLine($"[{aggregatorId}] Mensagem mal formatada: {message}");
                        channel.BasicReject(ea.DeliveryTag, false);
                        return;
                    }

                    string wavyId = parts[0];
                    string sensor = parts[1];
                    string timestampRaw = parts[2];
                    string value = parts[3];

                    if (!DateTime.TryParse(timestampRaw, out DateTime timestamp))
                    {
                        Console.WriteLine($"[{aggregatorId}] Timestamp inválido: {timestampRaw}");
                        channel.BasicReject(ea.DeliveryTag, false);
                        return;
                    }

                    timestamp = timestamp.ToUniversalTime();

                    // Tabela para dados crus
                    string rawTable = $"{sensor.Trim().ToLowerInvariant()}_data_raw";

                    // Determina a tabela de dados processados com base no tipo de sensor
                    string processedTable = sensor switch
                    {
                        "Temperature" => "temperature_data_processed",
                        "Hydrophone" => "hydrophone_data_processed",
                        "Accelerometer" => "accelerometer_data_processed",
                        "Gyroscope" => "gyroscope_data_processed",
                        _ => throw new Exception($"Sensor desconhecido: {sensor}")
                    };

                    // Ligação à base de dados
                    await using var dbConnection = new MySqlConnection(connString);
                    await dbConnection.OpenAsync();

                    // Inserção dos dados crus
                    await using var cmdRaw = dbConnection.CreateCommand();
                    cmdRaw.CommandText = $@"INSERT INTO {rawTable} (wavy_id, timestamp, sensor, value)
                                            VALUES (@wavy_id, @timestamp, @sensor, @value)";
                    cmdRaw.Parameters.AddWithValue("@wavy_id", wavyId);
                    cmdRaw.Parameters.AddWithValue("@timestamp", timestamp);
                    cmdRaw.Parameters.AddWithValue("@sensor", sensor);
                    cmdRaw.Parameters.AddWithValue("@value", value);
                    await cmdRaw.ExecuteNonQueryAsync();

                    // Recolha dos últimos valores para referência estatística
                    var recentValues = await ObterUltimosValoresAsync(wavyId, sensor, 5, connString, rawTable);

                    // Processamento dos dados via gRPC
                    var resposta = await grpcClient.ProcessarAsync(wavyId, sensor, value, timestampRaw, recentValues);

                    if (resposta == null)
                    {
                        channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    // Inserção dos dados processados na base de dados
                    await using var cmdProcessed = dbConnection.CreateCommand();
                    cmdProcessed.CommandText = $@"INSERT INTO {processedTable} (
                        wavy_id, timestamp, sensor, mean, stddev, is_outlier, delta, trend, risk_level, normalized_timestamp, on_schedule
                    ) VALUES (
                        @wavy_id, @timestamp, @sensor, @mean, @stddev, @is_outlier, @delta, @trend, @risk_level, @normalized_timestamp, @on_schedule
                    )";
                    cmdProcessed.Parameters.AddWithValue("@wavy_id", wavyId);
                    cmdProcessed.Parameters.AddWithValue("@timestamp", timestamp);
                    cmdProcessed.Parameters.AddWithValue("@sensor", sensor);
                    cmdProcessed.Parameters.AddWithValue("@mean", resposta.Mean);
                    cmdProcessed.Parameters.AddWithValue("@stddev", resposta.Stddev);
                    cmdProcessed.Parameters.AddWithValue("@is_outlier", resposta.IsOutlier);
                    cmdProcessed.Parameters.AddWithValue("@delta", resposta.DeltaFromLast);
                    cmdProcessed.Parameters.AddWithValue("@trend", resposta.Trend);
                    cmdProcessed.Parameters.AddWithValue("@risk_level", resposta.RiskLevel);
                    cmdProcessed.Parameters.AddWithValue("@normalized_timestamp", resposta.NormalizedTimestamp);
                    cmdProcessed.Parameters.AddWithValue("@on_schedule", resposta.OnSchedule);
                    await cmdProcessed.ExecuteNonQueryAsync();

                    channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{aggregatorId}] Erro ao processar mensagem: {ex.Message}");
                    channel.BasicNack(ea.DeliveryTag, false, true); // Rejeita mas reencaminha
                }
            };

            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }

        /// <summary>
        /// Encerra de forma segura a ligação e o canal RabbitMQ.
        /// </summary>
        public void Stop()
        {
            channel?.Close();
            connection?.Close();
        }

        /// <summary>
        /// Obtém os últimos N valores registados para um dado sensor e dispositivo.
        /// </summary>
        /// <param name="wavyId">Identificador do dispositivo.</param>
        /// <param name="sensor">Tipo de sensor.</param>
        /// <param name="n">Número de valores a obter.</param>
        /// <param name="connStr">String de ligação à base de dados.</param>
        /// <param name="tableName">Nome da tabela de onde extrair os valores.</param>
        /// <returns>Lista com os últimos valores numéricos.</returns>
        private async Task<List<double>> ObterUltimosValoresAsync(string wavyId, string sensor, int n, string connStr, string tableName)
        {
            var valores = new List<double>();

            try
            {
                await using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"SELECT value FROM {tableName} 
                                     WHERE wavy_id = @wavy_id AND sensor = @sensor 
                                     ORDER BY timestamp DESC LIMIT @n";
                cmd.Parameters.AddWithValue("@wavy_id", wavyId);
                cmd.Parameters.AddWithValue("@sensor", sensor);
                cmd.Parameters.AddWithValue("@n", n);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (double.TryParse(reader["value"].ToString(), out double val))
                        valores.Add(val);
                }

                valores.Reverse(); // Para manter ordem cronológica crescente
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Erro BD] Falha ao obter valores anteriores: {ex.Message}");
            }

            return valores;
        }
    }
}
