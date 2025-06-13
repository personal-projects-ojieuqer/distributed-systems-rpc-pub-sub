using RabbitMQ.Client;
using System.Text;

namespace Wavies.Wavy
{
    /// <summary>
    /// Classe responsável por publicar mensagens no sistema de filas RabbitMQ,
    /// utilizando um exchange do tipo 'topic' com o nome 'sensores'.
    /// </summary>
    public static class RabbitPublisher
    {
        // Representa a ligação ativa ao broker RabbitMQ.
        private static IConnection? connection;

        // Representa o canal de comunicação com o broker.
        private static IModel? channel;

        /// <summary>
        /// Inicializa a ligação e o canal com o broker RabbitMQ,
        /// bem como a definição do exchange utilizado para publicação.
        /// </summary>
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

            // Declara um exchange do tipo 'topic' com persistência (durable)
            channel.ExchangeDeclare(exchange: "sensores", type: ExchangeType.Topic, durable: true);

            Console.WriteLine("[RabbitMQ] Publisher inicializado.");
        }

        /// <summary>
        /// Publica uma mensagem de forma assíncrona no exchange configurado,
        /// utilizando uma routing key baseada no identificador do dispositivo e tipo de sensor.
        /// </summary>
        /// <param name="wavyId">Identificador único do dispositivo Wavy.</param>
        /// <param name="sensorType">Tipo de sensor a reportar (ex: temperatura, humidade).</param>
        /// <param name="message">Mensagem codificada a ser enviada.</param>
        /// <returns>Tarefa representando a operação assíncrona.</returns>
        public static async Task PublishAsync(string wavyId, string sensorType, string message)
        {
            if (channel == null)
                throw new InvalidOperationException("RabbitMQ channel não inicializado.");

            // Codifica a mensagem em bytes UTF-8
            var body = Encoding.UTF8.GetBytes(message);

            // Gera a routing key no formato: sensor.{id}.{tipo}
            string routingKey = $"sensor.{wavyId.ToLower()}.{sensorType.ToLower()}";

            // Publica a mensagem num contexto paralelo
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

        /// <summary>
        /// Encerra de forma segura o canal e a ligação ao broker RabbitMQ.
        /// </summary>
        public static void Close()
        {
            channel?.Close();
            connection?.Close();
        }
    }
}
