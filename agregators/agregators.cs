using MySql.Data.MySqlClient;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace agregators
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string aggregatorId = "AGG_01";
            int port = 13311;
            string dbName = "agregator1-db";

            // Usa a porta correta definida no docker-compose para o AGG_01
            string connString = "Server=127.0.0.1;Port=3311;Database=agregator1-db;Uid=root;Pwd=root;";

            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"{aggregatorId} a escutar na porta {port}...");

            while (true)
            {
                var client = listener.AcceptTcpClient();
                Console.WriteLine("Conexão recebida!");

                var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var data = reader.ReadToEnd();

                Console.WriteLine("Dados recebidos:\n" + data);

                using var connection = new MySqlConnection(connString);
                connection.Open();

                var lines = data.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var parts = line.Split(',', 3);
                    if (parts.Length == 3 && parts[0].Contains(":"))
                    {
                        var idSplit = parts[0].Split(':', 2);
                        string wavyId = idSplit[0];

                        string timestampRaw = idSplit[1];
                        if (!DateTime.TryParseExact(timestampRaw, "yyyy-MM-ddTHH:mm:ss.fffffffZ", null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime timestamp))
                        {
                            Console.WriteLine($"Timestamp inválido: {timestampRaw}");
                            continue;
                        }

                        string sensor = parts[1];
                        string value = parts[2];

                        var cmd = connection.CreateCommand();
                        cmd.CommandText = @"INSERT INTO sensor_data (wavy_id, timestamp, sensor, value)
                                            VALUES (@wavy_id, @timestamp, @sensor, @value)";
                        cmd.Parameters.AddWithValue("@wavy_id", wavyId);
                        cmd.Parameters.AddWithValue("@timestamp", timestamp);
                        cmd.Parameters.AddWithValue("@sensor", sensor);
                        cmd.Parameters.AddWithValue("@value", value);

                        Console.WriteLine($"INSERT: {wavyId} | {timestamp} | {sensor} | {value}");

                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"ERRO ao inserir no MySQL: {ex.Message}");
                        }
                    }
                }

                connection.Close();
                client.Close();
            }
        }
    }
}
