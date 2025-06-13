namespace agregators
{
    internal class Program
    {
        /// <summary>
        /// Ponto de entrada principal da aplicação agregadora.
        /// Responsável por configurar variáveis, iniciar ligação ao RabbitMQ,
        /// criar o cliente gRPC, subscrever sensores e sincronizar dados com o servidor.
        /// </summary>
        static void Main(string[] args)
        {
            // Leitura de variáveis de ambiente com valores por defeito
            string aggregatorId = Environment.GetEnvironmentVariable("AGGREGATOR_ID") ?? "AGG_01";
            string subscriptionKey = Environment.GetEnvironmentVariable("SUBSCRIPTION_KEY") ?? "sensor.#";

            string dbHost = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "localhost";
            string dbPort = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
            string dbUser = Environment.GetEnvironmentVariable("MYSQL_USER") ?? "root";
            string dbPass = Environment.GetEnvironmentVariable("MYSQL_PASS") ?? "root";
            string dbName = Environment.GetEnvironmentVariable("MYSQL_DB") ?? "agregator1_db_tp2";

            string serverIp = Environment.GetEnvironmentVariable("SERVER_IP") ?? "server_app"; // Nome do container
            int serverPort = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "15000");

            // Construção da string de ligação à base de dados MySQL
            string connString = $"Server={dbHost};Port={dbPort};Database={dbName};Uid={dbUser};Pwd={dbPass}";

            // Aguarda que o RabbitMQ esteja disponível antes de continuar
            bool connected = false;
            while (!connected)
            {
                try
                {
                    var factory = new RabbitMQ.Client.ConnectionFactory()
                    {
                        HostName = "rabbitmq",
                        Port = 5672,
                        UserName = "sdtp2",
                        Password = "sdtp2"
                    };

                    using var conn = factory.CreateConnection();
                    connected = true;
                }
                catch
                {
                    Console.WriteLine($"[{aggregatorId}] RabbitMQ não disponível... a aguardar...");
                    Thread.Sleep(3000); // Espera 3 segundos antes de tentar novamente
                }
            }

            // Inicialização do cliente gRPC e subscrição ao RabbitMQ
            string grpcAddress = "http://preprocessrpc:8080";
            var grpcClient = new GrpcClient(grpcAddress);
            var subscriber = new RabbitSubscriber(aggregatorId, connString, subscriptionKey, grpcClient);
            subscriber.Start();

            Console.WriteLine($"[{aggregatorId}] Agregador iniciado com RabbitMQ. A receber sensores e a sincronizar com servidor...");

            // Tarefa assíncrona para enviar dados periodicamente ao servidor central
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await AggregatorSender.EnviarDadosParaServidorAsync(aggregatorId, connString, serverIp, serverPort);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{aggregatorId}] Falha no envio de dados: {ex.Message}");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(30)); // Frequência de envio: 30 segundos
                }
            });

            // Mantém a aplicação ativa indefinidamente
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
