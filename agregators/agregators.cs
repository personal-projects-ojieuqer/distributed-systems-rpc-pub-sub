//namespace agregators
//{
//    internal class Program
//    {
//        static void Main(string[] args)
//        {
//            // Leitura das variáveis de ambiente
//            string aggregatorId = Environment.GetEnvironmentVariable("AGGREGATOR_ID") ?? "AGG_01";


//            string subscriptionKey = Environment.GetEnvironmentVariable("SUBSCRIPTION_KEY") ?? "sensor.#";

//            string dbHost = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "localhost";
//            string dbPort = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
//            string dbUser = Environment.GetEnvironmentVariable("MYSQL_USER") ?? "root";
//            string dbPass = Environment.GetEnvironmentVariable("MYSQL_PASS") ?? "root";
//            string dbName = Environment.GetEnvironmentVariable("MYSQL_DB") ?? "agregator1_db_tp2";

//            string serverIp = Environment.GetEnvironmentVariable("SERVER_IP") ?? "127.0.0.1";
//            int serverPort = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "15000");

//            // String de ligação à base de dados local
//            string connString = $"Server={dbHost};Port={dbPort};Database={dbName};Uid={dbUser};Pwd={dbPass}";

//            bool connected = false;
//            while (!connected)
//            {
//                try
//                {
//                    var factory = new RabbitMQ.Client.ConnectionFactory()
//                    {
//                        HostName = "rabbitmq",
//                        Port = 5672,
//                        UserName = "sdtp2",
//                        Password = "sdtp2"
//                    };
//                    using var conn = factory.CreateConnection();
//                    connected = true;
//                }
//                catch
//                {
//                    Console.WriteLine($"[{aggregatorId}] RabbitMQ não disponível... a aguardar...");
//                    Thread.Sleep(3000);
//                }
//            }

//            // Cria o cliente gRPC
//            string grpcAddress = "http://preprocessrpc:8080"; // nome do container + porta exposta interna
//            var grpcClient = new GrpcClient(grpcAddress);

//            // Inicializa o consumidor RabbitMQ com gRPC
//            var subscriber = new RabbitSubscriber(aggregatorId, connString, subscriptionKey, grpcClient);
//            subscriber.Start();

//            Console.WriteLine($"[{aggregatorId}] Agregador iniciado com RabbitMQ. A receber sensores e a sincronizar com servidor...");

//            _ = Task.Run(async () =>
//            {
//                while (true)
//                {
//                    await AggregatorSender.EnviarDadosParaServidorAsync(
//                        aggregatorId,
//                        connString,
//                        "server_app",
//                        serverPort
//                    );
//                    await Task.Delay(TimeSpan.FromSeconds(10));
//                }
//            });


//            Thread.Sleep(Timeout.Infinite);
//        }
//    }
//}

namespace agregators
{
    internal class Program
    {

        static void Main(string[] args)
        {
            string aggregatorId = Environment.GetEnvironmentVariable("AGGREGATOR_ID") ?? "AGG_01";
            string subscriptionKey = Environment.GetEnvironmentVariable("SUBSCRIPTION_KEY") ?? "sensor.#";

            string dbHost = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "localhost";
            string dbPort = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
            string dbUser = Environment.GetEnvironmentVariable("MYSQL_USER") ?? "root";
            string dbPass = Environment.GetEnvironmentVariable("MYSQL_PASS") ?? "root";
            string dbName = Environment.GetEnvironmentVariable("MYSQL_DB") ?? "agregator1_db_tp2";

            string serverIp = Environment.GetEnvironmentVariable("SERVER_IP") ?? "server_app"; // container name
            int serverPort = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "15000");

            string connString = $"Server={dbHost};Port={dbPort};Database={dbName};Uid={dbUser};Pwd={dbPass}";

            // Espera pelo RabbitMQ
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
                    Thread.Sleep(3000);
                }
            }

            // gRPC
            string grpcAddress = "http://preprocessrpc:8080";
            var grpcClient = new GrpcClient(grpcAddress);
            var subscriber = new RabbitSubscriber(aggregatorId, connString, subscriptionKey, grpcClient);
            subscriber.Start();

            Console.WriteLine($"[{aggregatorId}] Agregador iniciado com RabbitMQ. A receber sensores e a sincronizar com servidor...");

            // 🔁 Envio periódico de dados para o servidor
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

                    await Task.Delay(TimeSpan.FromSeconds(30)); // ajustar frequência aqui
                }
            });

            Thread.Sleep(Timeout.Infinite); // mantém app viva
        }
    }
}
