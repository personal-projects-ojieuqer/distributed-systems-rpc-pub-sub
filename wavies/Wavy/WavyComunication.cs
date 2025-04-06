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
                "AGG_01" => 13311,
                "AGG_02" => 13312,
                "AGG_03" => 13313,
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
                string[] lines = File.ReadAllLines(csvPath).Skip(1).ToArray();

                foreach (string line in lines)
                {
                    string message = $"{wavyId}:{line}";
                    byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                    await stream.WriteAsync(data);
                }

                Console.WriteLine($"[{wavyId}] Dados enviados com sucesso para {aggregatorId}.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{wavyId}Erro ao enviar para {aggregatorId}: {ex.Message}");
                return false;
            }
        }
    }
}
