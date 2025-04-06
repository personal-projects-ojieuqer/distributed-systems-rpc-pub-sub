//using MySql.Data.MySqlClient;
//using System.Net.Sockets;
//using System.Text;

//namespace agregators
//{
//    public static class AggregatorHandler
//    {
//        private static readonly object dbLock = new();

//        private const string CONNECTION_STRING = "server=localhost;port=3311;user=root;password=root;database=agregator1-db;";

//        public static void HandleClient(TcpClient client)
//        {
//            try
//            {
//                using NetworkStream stream = client.GetStream();
//                using StreamReader reader = new StreamReader(stream, Encoding.UTF8);

//                string? line;
//                while ((line = reader.ReadLine()) != null)
//                {
//                    Console.WriteLine($"Recebido: {line}");
//                    ProcessLineToDatabase(line);
//                }

//                Console.WriteLine("Ligação terminada.");
//                client.Close();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Erro no cliente: {ex.Message}");
//            }
//        }

//        private static void ProcessLineToDatabase(string line)
//        {
//            try
//            {
//                var parts = line.Split(',');
//                if (parts.Length < 4) return;

//                string wavyId = parts[0];
//                string timestamp = parts[1];
//                string sensor = parts[2];
//                string value = parts[3];

//                lock (dbLock)
//                {
//                    using var conn = new MySqlConnection(CONNECTION_STRING);
//                    conn.Open();

//                    string sql = @"INSERT INTO sensor_data (wavy_id, timestamp, sensor, value)
//                                   VALUES (@wavy_id, @timestamp, @sensor, @value)";
//                    using var cmd = new MySqlCommand(sql, conn);
//                    cmd.Parameters.AddWithValue("@wavy_id", wavyId);
//                    cmd.Parameters.AddWithValue("@timestamp", DateTime.Parse(timestamp));
//                    cmd.Parameters.AddWithValue("@sensor", sensor);
//                    cmd.Parameters.AddWithValue("@value", value);
//                    cmd.ExecuteNonQuery();

//                    conn.Close();
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Erro ao inserir na BD: {ex.Message}");
//            }
//        }
//    }
//}
