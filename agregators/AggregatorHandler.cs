//using MySql.Data.MySqlClient;
//using System.Net.Sockets;
//using System.Text;

//namespace agregators
//{
//    /// <summary>
//    /// Classe responsável por tratar a receção de dados de clientes (wavy devices) ligados a um agregador.
//    /// </summary>
//    public static class AggregatorHandler
//    {
//        // Objeto de bloqueio para garantir acesso exclusivo à base de dados (evita concorrência)
//        private static readonly object dbLock = new();

//        /// <summary>
//        /// Processa a comunicação com um cliente TCP individual (dispositivo wavy).
//        /// </summary>
//        /// <param name="client">Cliente TCP ligado ao agregador.</param>
//        /// <param name="connString">String de ligação à base de dados local do agregador.</param>
//        /// <param name="aggregatorId">Identificador do agregador a que o cliente está ligado.</param>
//        public static async Task HandleClientAsync(TcpClient client, string connString, string aggregatorId)
//        {
//            try
//            {
//                // Caminho do ficheiro de autorização (lista de IDs wavy autorizados a comunicar)
//                string authFile = Path.Combine("autorizacoes", $"{aggregatorId}.txt");

//                // Lê os wavy_ids autorizados a comunicar com este agregador
//                HashSet<string> autorizados = File.Exists(authFile)
//                    ? new HashSet<string>(await File.ReadAllLinesAsync(authFile))
//                    : new HashSet<string>();

//                // Guarda os wavy_ids para os quais já se mostrou aviso (evita mensagens repetidas)
//                HashSet<string> avisados = new();

//                using var stream = client.GetStream();
//                using var reader = new StreamReader(stream, Encoding.UTF8);

//                string? line;
//                while ((line = await reader.ReadLineAsync()) != null)
//                {
//                    Console.WriteLine($"Linha recebida: {line}");

//                    // Início de envio de dados de um wavy
//                    if (line.StartsWith("START:"))
//                    {
//                        string wavyStart = line.Split(':')[1];
//                        Console.WriteLine($"🟢  INÍCIO de envio de {wavyStart}");
//                        continue;
//                    }

//                    // Fim de envio de dados de um wavy
//                    if (line.StartsWith("END:"))
//                    {
//                        string wavyEnd = line.Split(':')[1];
//                        Console.WriteLine($"🔴  FIM de envio de {wavyEnd}");
//                        continue;
//                    }

//                    // Espera-se um formato como: wavyId:timestamp,sensor,value
//                    var parts = line.Split(',', 3);
//                    if (parts.Length == 3 && parts[0].Contains(":"))
//                    {
//                        var idSplit = parts[0].Split(':', 2);
//                        string wavyId = idSplit[0];
//                        string timestampRaw = idSplit[1];

//                        // Mostra informação de autorização apenas uma vez por wavyId
//                        if (!avisados.Contains(wavyId))
//                        {
//                            Console.WriteLine(autorizados.Contains(wavyId)
//                                ? $"{wavyId} está autorizado a comunicar com {aggregatorId}."
//                                : $"{wavyId} NÃO está autorizado a comunicar com {aggregatorId}.");
//                            avisados.Add(wavyId);
//                        }

//                        // Ignora se não estiver autorizado
//                        if (!autorizados.Contains(wavyId)) continue;

//                        // Validação do timestamp
//                        if (!DateTime.TryParse(timestampRaw, out DateTime timestamp))
//                        {
//                            Console.WriteLine($"Timestamp inválido: {timestampRaw}");
//                            continue;
//                        }

//                        timestamp = timestamp.ToUniversalTime();

//                        string sensor = parts[1];
//                        string value = parts[2];

//                        // Acesso exclusivo à base de dados usando lock
//                        lock (dbLock)
//                        {
//                            using var connection = new MySqlConnection(connString);
//                            connection.Open();

//                            var cmd = connection.CreateCommand();
//                            cmd.CommandText = @"INSERT INTO sensor_data (wavy_id, timestamp, sensor, value)
//                                                VALUES (@wavy_id, @timestamp, @sensor, @value)";
//                            cmd.Parameters.AddWithValue("@wavy_id", wavyId);
//                            cmd.Parameters.AddWithValue("@timestamp", timestamp);
//                            cmd.Parameters.AddWithValue("@sensor", sensor);
//                            cmd.Parameters.AddWithValue("@value", value);

//                            try
//                            {
//                                cmd.ExecuteNonQuery();
//                                Console.WriteLine($"{aggregatorId} recebeu de {wavyId} : {sensor}: {value}");
//                            }
//                            catch (Exception ex)
//                            {
//                                Console.WriteLine($"ERRO ao inserir no MySQL: {ex.Message}");
//                            }
//                        }
//                    }
//                    else
//                    {
//                        // Linha ignorada se não estiver no formato esperado
//                        Console.WriteLine("Linha ignorada (formato inválido).");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                // Tratamento de erro da ligação com o cliente
//                Console.WriteLine($"Erro na thread do cliente: {ex.Message}");
//            }
//            finally
//            {
//                client.Close();
//                Console.WriteLine("Cliente desconectado.");
//            }
//        }
//    }
//}
