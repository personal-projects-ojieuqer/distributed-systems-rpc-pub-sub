using System.Net;
using System.Net.Sockets;

namespace agregators
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string aggregatorId = Environment.GetEnvironmentVariable("AGGREGATOR_ID") ?? "AGG_01";
            int listenPort = int.Parse(Environment.GetEnvironmentVariable("LISTEN_PORT") ?? "13311");

            string dbHost = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "localhost";
            string dbPort = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
            string dbUser = Environment.GetEnvironmentVariable("MYSQL_USER") ?? "root";
            string dbPass = Environment.GetEnvironmentVariable("MYSQL_PASS") ?? "root";
            string dbName = Environment.GetEnvironmentVariable("MYSQL_DB") ?? "agregator1_db";

            string serverIp = Environment.GetEnvironmentVariable("SERVER_IP") ?? "127.0.0.1";
            int serverPort = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "15000");

            string connString = $"Server={dbHost};Port={dbPort};Database={dbName};Uid={dbUser};Pwd={dbPass};";

            // THREAD 1: Servidor TCP que escuta os WAVIES
            var listener = new TcpListener(IPAddress.Any, listenPort);
            listener.Start();
            Console.WriteLine($"{aggregatorId} a escutar WAVIES na porta {listenPort}...");

            _ = Task.Run(() =>
            {
                while (true)
                {
                    var client = listener.AcceptTcpClient();
                    _ = Task.Run(() => AggregatorHandler.HandleClient(client, connString, aggregatorId));
                }
            });

            // THREAD 2: Envio periódico para o servidor central
            _ = Task.Run(() =>
            {
                while (true)
                {
                    AggregatorSender.EnviarDadosParaServidor(aggregatorId, connString, serverIp, serverPort);
                    Thread.Sleep(10000); // envia de 10 em 10 segundos
                }
            });

            Console.WriteLine($"[{aggregatorId}] Agregador iniciado. A receber WAVIES e a sincronizar com servidor...");

            // Mantém a aplicação viva
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
