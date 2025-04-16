using System.Net;
using System.Net.Sockets;

namespace agregators
{
    /// <summary>
    /// Classe principal do agregador. Este programa escuta conexões dos dispositivos "WAVIES"
    /// e periodicamente envia os dados recolhidos para o servidor central via TCP.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Método principal do agregador. Inicializa as configurações, arranca o servidor TCP para os WAVIES
        /// e inicia o ciclo de envio de dados para o servidor central.
        /// </summary>
        /// <param name="args">Argumentos da linha de comandos (não utilizados neste contexto).</param>
        static void Main(string[] args)
        {
            // Leitura das variáveis de ambiente para configurar o agregador e a base de dados local
            string aggregatorId = Environment.GetEnvironmentVariable("AGGREGATOR_ID") ?? "AGG_01";
            int listenPort = int.Parse(Environment.GetEnvironmentVariable("LISTEN_PORT") ?? "13311");

            string dbHost = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "localhost";
            string dbPort = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
            string dbUser = Environment.GetEnvironmentVariable("MYSQL_USER") ?? "root";
            string dbPass = Environment.GetEnvironmentVariable("MYSQL_PASS") ?? "root";
            string dbName = Environment.GetEnvironmentVariable("MYSQL_DB") ?? "agregator1_db";

            string serverIp = Environment.GetEnvironmentVariable("SERVER_IP") ?? "127.0.0.1";
            int serverPort = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "15000");

            // Monta a string de ligação à base de dados local do agregador e configura a Thread Pool
            string connString = $"Server={dbHost};Port={dbPort};Database={dbName};Uid={dbUser};Pwd={dbPass};Pooling=true;Min Pool Size=0;Max Pool Size=100;";

            var listener = new TcpListener(IPAddress.Any, listenPort);
            listener.Start();
            Console.WriteLine($"{aggregatorId} a escutar WAVIES na porta {listenPort}...");

            /// <summary>
            /// Thread dedicada a aceitar ligações de dispositivos WAVIES
            /// e a encaminhá-las para o handler apropriado.
            /// </summary>
            _ = Task.Run(() =>
            {
                while (true)
                {
                    var client = listener.AcceptTcpClient();
                    // Cada ligação de WAVY é tratada numa nova thread
                    _ = AggregatorHandler.HandleClientAsync(client, connString, aggregatorId);
                }
            });

            /// <summary>
            /// Thread responsável por enviar os dados da base de dados local
            /// para o servidor central a cada 10 segundos.
            /// </summary>
            _ = Task.Run(() =>
            {
                while (true)
                {
                    // Envia dados para o servidor central
                    AggregatorSender.EnviarDadosParaServidorAsync(aggregatorId, connString, serverIp, serverPort);
                    Thread.Sleep(10000); // envia de 10 em 10 segundos
                }
            });

            Console.WriteLine($"[{aggregatorId}] Agregador iniciado. A receber WAVIES e a sincronizar com servidor...");

            // Mantém a aplicação viva indefinidamente
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
