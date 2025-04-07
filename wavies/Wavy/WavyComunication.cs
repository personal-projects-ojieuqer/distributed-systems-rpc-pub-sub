using System.Net.Sockets;
using System.Text;

namespace wavies.Wavy
{
    public static class WavyComunication
    {
        public static async Task<bool> SendToAggregatorAsync(string wavyId, string csvPath, string aggregatorId)
        {
            Console.WriteLine($"[{wavyId}] A enviar dados para {aggregatorId}...");

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
                Console.WriteLine($"SendToAggregator [{wavyId}]: Agregador desconhecido");
                return false;
            }

            try
            {
                using TcpClient client = new TcpClient();
                await client.ConnectAsync(ip, port);

                using var stream = client.GetStream();
                string[] lines = File.ReadAllLines(csvPath).Skip(1).ToArray(); // Ignora o cabeçalho do CSV

                foreach (string line in lines)
                {
                    string message = $"{wavyId}:{line}";
                    byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                    await stream.WriteAsync(data);
                }

                Console.WriteLine($"[{wavyId}] Dados enviados com sucesso para {aggregatorId}.");
                return true;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
            {
                Console.WriteLine($"[{wavyId}] Erro: A ligação foi recusada por {aggregatorId}. Verifica se o agregador está ativo.");
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Console.WriteLine($"[{wavyId}] Aviso: A porta {port} já está em uso. Outro WAVY pode estar ligado a {aggregatorId}.");
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                Console.WriteLine($"[{wavyId}] Erro: O tempo de ligação esgotou ao tentar ligar a {aggregatorId}. O agregador pode estar inativo ou sobrecarregado.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{wavyId}] Erro inesperado ao enviar para {aggregatorId}: {ex.Message}");
            }

            return false;
        }
    }
}

