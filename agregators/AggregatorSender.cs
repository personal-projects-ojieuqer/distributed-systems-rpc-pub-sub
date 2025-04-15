using MySql.Data.MySqlClient;
using System.Net.Sockets;
using System.Text;

public static class AggregatorSender
{
    public static void EnviarDadosParaServidor(string aggregatorId, string localConnStr, string servidorIp, int servidorPorta)
    {
        try
        {
            using var conn = new MySqlConnection(localConnStr);
            conn.Open();

            DateTime ultimoSync = DateTime.UtcNow.AddMinutes(-1); // ou recuperar de ficheiro/config
            string query = @"SELECT wavy_id, timestamp, sensor, value 
                             FROM sensor_data 
                             WHERE timestamp > @lastSync 
                             ORDER BY timestamp";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@lastSync", ultimoSync);

            using var reader = cmd.ExecuteReader();
            var linhasParaEnviar = new List<string>();

            while (reader.Read())
            {
                string wavyId = reader.GetString(0);
                DateTime timestamp = reader.GetDateTime(1);
                string sensor = reader.GetString(2);
                string value = reader.GetString(3);

                string linha = $"{aggregatorId},{wavyId},{timestamp:o},{sensor},{value}";
                linhasParaEnviar.Add(linha);
            }

            if (linhasParaEnviar.Count == 0)
            {
                Console.WriteLine($"[AGGREGADOR {aggregatorId}] Nenhum dado novo para enviar.");
                return;
            }

            string token = Environment.GetEnvironmentVariable($"AGG_TOKEN_{aggregatorId}") ?? "";
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine($"[AGGREGADOR {aggregatorId}] Token não encontrado. Abortando envio.");
                return;
            }

            using TcpClient client = new TcpClient();
            client.Connect("server_app", servidorPorta);

            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            //Handshake token +  dados
            writer.WriteLine($"HANDSHAKE:{aggregatorId}:{token}");
            writer.WriteLine($"START:{aggregatorId}");


            foreach (var linha in linhasParaEnviar)
            {
                writer.WriteLine(linha);
            }

            writer.WriteLine($"END:{aggregatorId}");

            Console.WriteLine($"[AGGREGADOR {aggregatorId}] Enviados {linhasParaEnviar.Count} registos ao servidor.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AGGREGADOR {aggregatorId}] Erro ao enviar dados: {ex.Message}");
        }
    }
}
