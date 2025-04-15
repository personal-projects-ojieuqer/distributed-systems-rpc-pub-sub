using MySql.Data.MySqlClient;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    private static readonly object centralDbLock = new();
    private static string centralConnStr = string.Empty;

    static void Main()
    {
        Console.WriteLine("🟢 Servidor iniciado e pronto para receber dados dos agregadores via TCP.");

        string mysqlPort = Environment.GetEnvironmentVariable("MYSQL_PORT")!;
        string mysqlUser = Environment.GetEnvironmentVariable("MYSQL_USER")!;
        string mysqlPass = Environment.GetEnvironmentVariable("MYSQL_PASS")!;
        string centralHost = Environment.GetEnvironmentVariable("MYSQL_HOST_SERVER")!;
        string centralDb = Environment.GetEnvironmentVariable("MYSQL_DB_SERVER")!;
        centralConnStr = $"Server={centralHost};Port={mysqlPort};Database={centralDb};Uid={mysqlUser};Pwd={mysqlPass};";

        int port = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "15000");
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"[SERVER] À escuta na porta {port}...");

        while (true)
        {
            var client = listener.AcceptTcpClient();
            _ = Task.Run(() => HandleAggregator(client));
        }
    }

    static void HandleAggregator(TcpClient client)
    {
        try
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var conn = new MySqlConnection(centralConnStr);
            conn.Open();

            string? line;
            string? currentAgg = null;

            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("HANDSHAKE:"))
                {
                    var handshakeParts = line.Split(':');
                    string handshakeAggId = handshakeParts[1];
                    string receivedToken = handshakeParts[2];
                    string? expectedToken = Environment.GetEnvironmentVariable($"AGG_TOKEN_{handshakeAggId}");

                    if (expectedToken is null || expectedToken != receivedToken)
                    {
                        Console.WriteLine($"Token inválido para {handshakeAggId}. Ligação recusada.");
                        client.Close();
                        return;
                    }

                    Console.WriteLine($"\nVerificação Handshake com {handshakeAggId} verificado com sucesso.");
                    continue;
                }

                if (line.StartsWith("START:"))
                {
                    currentAgg = line.Split(':')[1];
                    Console.WriteLine($"\n🟢 Início de envio do {currentAgg}");
                    continue;
                }

                if (line.StartsWith("END:"))
                {
                    string endAgg = line.Split(':')[1];
                    Console.WriteLine($"🔴 Fim de envio de {endAgg}\n");
                    continue;
                }

                var parts = line.Split(',', 5);
                if (parts.Length != 5)
                {
                    Console.WriteLine("Linha mal formatada: " + line);
                    continue;
                }

                string aggId = parts[0];
                string wavyId = parts[1];
                if (!DateTime.TryParse(parts[2], out var timestamp))
                {
                    Console.WriteLine($"Timestamp inválido: {parts[2]}");
                    continue;
                }
                string sensor = parts[3];
                string value = parts[4];

                lock (centralDbLock)
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = @"INSERT INTO central_sensor_data (wavy_id, timestamp, sensor, value, aggregator)
                                        VALUES (@wavy_id, @timestamp, @sensor, @value, @aggregator)";
                    cmd.Parameters.AddWithValue("@wavy_id", wavyId);
                    cmd.Parameters.AddWithValue("@timestamp", timestamp);
                    cmd.Parameters.AddWithValue("@sensor", sensor);
                    cmd.Parameters.AddWithValue("@value", value);
                    cmd.Parameters.AddWithValue("@aggregator", aggId);
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine($"[{aggId}] Inserido : {wavyId} | {sensor} = {value}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SERVER] Erro ao tratar dados do agregador: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }
}
