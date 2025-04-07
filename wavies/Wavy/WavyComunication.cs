using System.Net.Sockets;
using System.Text;

namespace wavies.Wavy
{
    public static class WavyComunication
    {
        public static async Task<bool> SendToAggregatorAsync(string wavyId, string csvPath, string aggregatorId)
        {
            Console.WriteLine($"[{wavyId}] A tentar enviar dados para {aggregatorId}...");

            string ip = "127.0.0.1";
            int port = aggregatorId switch
            {
                "AGG_01" => 5001,
                "AGG_02" => 5002,
                "AGG_03" => 5003,
                _ => -1
            };

            if (port == -1)
            {
                Console.WriteLine($"[{wavyId}] ❌ Agregador desconhecido: {aggregatorId}");
                return false;
            }

            string mutexName = $"Global\\AGGREGATOR_MUTEX_{aggregatorId}";
            using var mutex = new Mutex(false, mutexName);
            bool hasHandle = false;

            try
            {
                hasHandle = mutex.WaitOne(3000); // tenta adquirir o mutex

                if (!hasHandle)
                {
                    Console.WriteLine($"[{wavyId}] 🚫 Outro WAVY está a enviar para {aggregatorId}, vou esperar para tentar novamente depois.");
                    return false;
                }

                using TcpClient client = new TcpClient();
                await client.ConnectAsync(ip, port);
                Console.WriteLine($"[{wavyId}] ✅ Ligação estabelecida com {aggregatorId}.");

                using var stream = client.GetStream();
                string[] lines = File.ReadAllLines(csvPath).Skip(1).ToArray(); // Ignora cabeçalho

                foreach (string line in lines)
                {
                    string message = $"{wavyId}:{line}";
                    byte[] data = Encoding.UTF8.GetBytes(message + "\n");

                    Console.WriteLine($"[{wavyId}] 📨 A enviar linha: {message}");
                    await stream.WriteAsync(data);
                }

                Console.WriteLine($"[{wavyId}] ✅ Todos os dados enviados com sucesso para {aggregatorId}.");
                return true;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
            {
                Console.WriteLine($"[{wavyId}] ❌ Ligação recusada por {aggregatorId}. Está ativo?");
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                Console.WriteLine($"[{wavyId}] ⌛ Timeout na ligação com {aggregatorId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{wavyId}] ❗ Erro inesperado: {ex.Message}");
            }
            finally
            {
                if (hasHandle)
                    mutex.ReleaseMutex();
            }

            return false;
        }
    }
}
