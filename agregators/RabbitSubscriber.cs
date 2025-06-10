// RabbitSubscriber.cs
using MySql.Data.MySqlClient;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace agregators
{
    public class RabbitSubscriber
    {
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly string connString;
        private readonly string aggregatorId;
        private readonly string subscriptionKey;
        private readonly GrpcClient grpcClient;

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
            channel.ExchangeDeclare(exchange: "sensores", type: ExchangeType.Topic, durable: true);
            channel.BasicQos(0, 10, false);
        }

        public void Start()
        {
            string queueName = $"queue_{aggregatorId.ToLower()}";
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
                    string rawTable = $"{sensor.Trim().ToLowerInvariant()}_data_raw";
                    string processedTable = $"{sensor.ToLower()}_data_processed";

                    await using var dbConnection = new MySqlConnection(connString);
                    await dbConnection.OpenAsync();

                    // Inserir dados crus imediatamente
                    await using var cmdRaw = dbConnection.CreateCommand();
                    cmdRaw.CommandText = $@"INSERT INTO {rawTable} (wavy_id, timestamp, sensor, value)
                                           VALUES (@wavy_id, @timestamp, @sensor, @value)";
                    cmdRaw.Parameters.AddWithValue("@wavy_id", wavyId);
                    cmdRaw.Parameters.AddWithValue("@timestamp", timestamp);
                    cmdRaw.Parameters.AddWithValue("@sensor", sensor);
                    cmdRaw.Parameters.AddWithValue("@value", value);
                    await cmdRaw.ExecuteNonQueryAsync();

                    // Obter valores anteriores para processamento
                    var recentValues = await ObterUltimosValoresAsync(wavyId, sensor, 5, connString, rawTable);

                    // Processar via RPC
                    var resposta = await grpcClient.ProcessarAsync(wavyId, sensor, value, timestampRaw, recentValues);

                    if (resposta == null)
                    {
                        channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    // Inserir dados processados
                    await using var cmdProcessed = dbConnection.CreateCommand();
                    cmdProcessed.CommandText = $@"INSERT INTO {processedTable} (
                        wavy_id, timestamp, sensor, processed_value, mean, stddev, is_outlier, delta, trend, risk_level, normalized_timestamp, on_schedule
                    ) VALUES (
                        @wavy_id, @timestamp, @sensor, @processed_value, @mean, @stddev, @is_outlier, @delta, @trend, @risk_level, @normalized_timestamp, @on_schedule
                    )";
                    cmdProcessed.Parameters.AddWithValue("@wavy_id", wavyId);
                    cmdProcessed.Parameters.AddWithValue("@timestamp", timestamp);
                    cmdProcessed.Parameters.AddWithValue("@sensor", sensor);
                    cmdProcessed.Parameters.AddWithValue("@processed_value", resposta.ProcessedValue);
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
                    channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }

        public void Stop()
        {
            channel?.Close();
            connection?.Close();
        }

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

                valores.Reverse();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Erro BD] Falha ao obter valores anteriores: {ex.Message}");
            }

            return valores;
        }
    }
}
