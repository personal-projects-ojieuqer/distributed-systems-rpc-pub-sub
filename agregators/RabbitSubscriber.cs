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
                DispatchConsumersAsync = true // IMPORTANT for async consumers
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
            channel.QueueBind(queue: queueName, exchange: "sensores", routingKey: subscriptionKey);

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
                    var normalizedValue = await grpcClient.FiltrarOuNormalizarAsync(wavyId, sensor, value);

                    if (normalizedValue == "Dado Ignorado")
                    {
                        channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    await using var dbConnection = new MySqlConnection(connString);
                    await dbConnection.OpenAsync();

                    await using var cmd = dbConnection.CreateCommand();
                    cmd.CommandText = @"INSERT INTO sensor_data (wavy_id, timestamp, sensor, value)
                                        VALUES (@wavy_id, @timestamp, @sensor, @value)";
                    cmd.Parameters.AddWithValue("@wavy_id", wavyId);
                    cmd.Parameters.AddWithValue("@timestamp", timestamp);
                    cmd.Parameters.AddWithValue("@sensor", sensor);
                    cmd.Parameters.AddWithValue("@value", normalizedValue);
                    await cmd.ExecuteNonQueryAsync();

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
    }
}
