using RabbitMQ.Client;
using System.Text;

namespace Wavies.Wavy
{
    public static class RabbitPublisher
    {
        private static IConnection? connection;
        private static IModel? channel;

        public static void Initialize()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "sdtp2",
                Password = "sdtp2"
            };

            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            channel.ExchangeDeclare(exchange: "sensores", type: ExchangeType.Topic, durable: true);


            Console.WriteLine("[RabbitMQ] Publisher inicializado.");
        }

        public static async Task PublishAsync(string wavyId, string sensorType, string message)
        {
            if (channel == null)
                throw new InvalidOperationException("RabbitMQ channel não inicializado.");

            var body = Encoding.UTF8.GetBytes(message);
            string routingKey = $"sensor.{wavyId.ToLower()}.{sensorType.ToLower()}";

            await Task.Run(() =>
            {
                channel.BasicPublish(
                    exchange: "sensores",
                    routingKey: routingKey,
                    basicProperties: null,
                    body: body
                );
            });

            Console.WriteLine($"[WAVY] Publicado: {routingKey} => {message}");
        }





        public static void Close()
        {
            channel?.Close();
            connection?.Close();
        }
    }
}
