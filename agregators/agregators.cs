using System.Net;
using System.Net.Sockets;

namespace agregators
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string aggregatorId = Environment.GetEnvironmentVariable("AGGREGATOR_ID") ?? "AGG_01";
            int port = int.Parse(Environment.GetEnvironmentVariable("LISTEN_PORT") ?? "13311");

            string dbHost = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "localhost";
            string dbPort = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
            string dbUser = Environment.GetEnvironmentVariable("MYSQL_USER") ?? "root";
            string dbPass = Environment.GetEnvironmentVariable("MYSQL_PASS") ?? "root";
            string dbName = Environment.GetEnvironmentVariable("MYSQL_DB") ?? "agregator1_db";

            string connString = $"Server={dbHost};Port={dbPort};Database={dbName};Uid={dbUser};Pwd={dbPass};";

            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"{aggregatorId} a escutar na porta {port}...");

            while (true)
            {
                var client = listener.AcceptTcpClient();
                Console.WriteLine("Conexão recebida!");

                // Cada conexão é processada numa thread separada
                _ = Task.Run(() => AggregatorHandler.HandleClient(client, connString, aggregatorId));
            }
        }
    }
}