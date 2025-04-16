using MySql.Data.MySqlClient;
using System.Net.Sockets;
using System.Text;

namespace agregators
{
    /// <summary>
    /// Classe responsável por tratar a ligação entre dispositivos WAVIES e o agregador.
    /// Valida permissões, interpreta os dados recebidos e insere-os na base de dados local.
    /// </summary>
    public static class AggregatorHandler
    {
        private static readonly object dbLock = new();

        /// <summary>
        /// Trata a comunicação com um dispositivo Wavy ligado ao agregador via TCP.
        /// Lê os dados enviados, verifica se o dispositivo está autorizado e insere os dados na base de dados.
        /// </summary>
        /// <param name="client">Ligação TCP do dispositivo Wavy.</param>
        /// <param name="connString">String de ligação à base de dados local do agregador.</param>
        /// <param name="aggregatorId">Identificador do agregador que está a processar a ligação.</param>
        public static void HandleClient(TcpClient client, string connString, string aggregatorId)
        {
            try
            {
                // Caminho para o ficheiro que contém os IDs dos WAVIES autorizados
                string authFile = Path.Combine("autorizacoes", $"{aggregatorId}.txt");
                // Lê os autorizados ou inicia conjunto vazio
                HashSet<string> autorizados = File.Exists(authFile)
                    ? new HashSet<string>(File.ReadAllLines(authFile))
                    : new HashSet<string>();

                // Guarda os WAVIES já verificados para não repetir mensagens
                HashSet<string> avisados = new();

                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    Console.WriteLine($"Linha recebida: {line}");

                    // Indica o início de envio de dados de um WAVY
                    if (line.StartsWith("START:"))
                    {
                        string wavyStart = line.Split(':')[1];
                        Console.WriteLine($"🟢  INÍCIO de envio de {wavyStart}");
                        continue;
                    }

                    // Indica o fim do envio de dados
                    if (line.StartsWith("END:"))
                    {
                        string wavyEnd = line.Split(':')[1];
                        Console.WriteLine($"🔴  FIM de envio de {wavyEnd}");
                        continue;
                    }

                    // Espera-se que os dados venham no formato: wavyId:timestamp,sensor,value
                    var parts = line.Split(',', 3);
                    if (parts.Length == 3 && parts[0].Contains(":"))
                    {
                        var idSplit = parts[0].Split(':', 2);
                        string wavyId = idSplit[0];
                        string timestampRaw = idSplit[1];

                        // Informa uma só vez se o Wavy está autorizado ou não
                        if (!avisados.Contains(wavyId))
                        {
                            if (autorizados.Contains(wavyId))
                                Console.WriteLine($"{wavyId} está autorizado a comunicar com {aggregatorId}.");
                            else
                                Console.WriteLine($"{wavyId} NÃO está autorizado a comunicar com {aggregatorId}.");

                            avisados.Add(wavyId);
                        }

                        // Ignora se o Wavy não estiver autorizado
                        if (!autorizados.Contains(wavyId)) continue;

                        // Verifica validade do timestamp
                        if (!DateTime.TryParse(timestampRaw, out DateTime timestamp))
                        {
                            Console.WriteLine($"Timestamp inválido: {timestampRaw}");
                            continue;
                        }

                        string sensor = parts[1];
                        string value = parts[2];

                        // Insere os dados na base de dados local do agregador
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
                                Console.WriteLine($"{aggregatorId} recebeu de {wavyId} : {sensor}: {value}");
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
                // Fecha ligação TCP no final
                client.Close();
                Console.WriteLine("Cliente desconectado.");
            }
        }
    }
}
