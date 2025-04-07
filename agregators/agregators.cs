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

                var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);

                using var connection = new MySqlConnection(connString);
                connection.Open();

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    Console.WriteLine($"📨 Linha recebida: {line}");

                    var parts = line.Split(',', 3);
                    if (parts.Length == 3 && parts[0].Contains(":"))
                    {
                        var idSplit = parts[0].Split(':', 2);
                        string wavyId = idSplit[0];
                        string timestampRaw = idSplit[1];

                        if (!DateTime.TryParse(timestampRaw, out DateTime timestamp))
                        {
                            Console.WriteLine($"⛔ Timestamp inválido: {timestampRaw}");
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

                        try
                        {
                            cmd.ExecuteNonQuery();
                            Console.WriteLine($"✅ INSERT: {wavyId} | {timestamp} | {sensor} | {value}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ ERRO ao inserir no MySQL: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Linha ignorada (formato inválido).");
                    }
                }

                connection.Close();
                client.Close();
            }
        }
    }
}
