using MySql.Data.MySqlClient;
using System.Net.Sockets;
using System.Text;

namespace agregators
{
    public static class AggregatorHandler
    {
        private static readonly object dbLock = new();

        public static void HandleClient(TcpClient client, string connString, string aggregatorId)
        {
            try
            {
                string authFile = Path.Combine("autorizacoes", $"{aggregatorId}.txt");
                HashSet<string> autorizados = File.Exists(authFile)
                    ? new HashSet<string>(File.ReadAllLines(authFile))
                    : new HashSet<string>();

                HashSet<string> avisados = new(); // Regista os wavies já verificados nesta sessão

                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    Console.WriteLine($"Linha recebida: {line}");

                    var parts = line.Split(',', 3);
                    if (parts.Length == 3 && parts[0].Contains(":"))
                    {
                        var idSplit = parts[0].Split(':', 2);
                        string wavyId = idSplit[0];
                        string timestampRaw = idSplit[1];

                        // Verificação da autorização apenas uma vez por WAVY
                        if (!avisados.Contains(wavyId))
                        {
                            if (autorizados.Contains(wavyId))
                                Console.WriteLine($"{wavyId} está autorizado a comunicar com {aggregatorId}.");
                            else
                                Console.WriteLine($"{wavyId} NÃO está autorizado a comunicar com {aggregatorId}.");

                            avisados.Add(wavyId);
                        }

                        if (!autorizados.Contains(wavyId)) continue;

                        if (!DateTime.TryParse(timestampRaw, out DateTime timestamp))
                        {
                            Console.WriteLine($"Timestamp inválido: {timestampRaw}");
                            continue;
                        }

                        string sensor = parts[1];
                        string value = parts[2];

                        lock (dbLock)
                        {
                            using var connection = new MySqlConnection(connString);
                            connection.Open();

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
                                Console.WriteLine($"INSERT: {wavyId} | {timestamp} | {sensor} | {value}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"ERRO ao inserir no MySQL: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Linha ignorada (formato inválido).");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro na thread do cliente: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("Cliente desconectado.");
            }
        }
    }
}
