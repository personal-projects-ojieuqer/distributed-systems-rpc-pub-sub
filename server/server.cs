using Grpc.Net.Client;
using HpcService;
using MySql.Data.MySqlClient;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

class Program
{
    private static readonly SemaphoreSlim dbSemaphore = new(1, 1);
    private static string centralConnStr = string.Empty;
    private static HpcAnalysisService.HpcAnalysisServiceClient? hpcClient;

    private static RSACryptoServiceProvider rsa;

    /// <summary>
    /// Ponto de entrada principal do servidor.
    /// Inicializa a chave RSA, configura a ligação ao serviço HPC, à base de dados,
    /// e começa a escutar conexões TCP dos agregadores.
    /// </summary>
    static async Task Main()
    {
        rsa = new RSACryptoServiceProvider(2048);
        string publicKeyXml = rsa.ToXmlString(false);

        Directory.CreateDirectory("/app/keys");
        File.WriteAllText("/app/keys/publicKey.xml", publicKeyXml);
        Console.WriteLine("[SERVIDOR] 🔐 Chave pública RSA gerada e exportada para /app/keys/publicKey.xml");

        Console.WriteLine("Servidor iniciado e pronto para receber dados dos agregadores via TCP.");

        string mysqlPort = Environment.GetEnvironmentVariable("MYSQL_PORT")!;
        string mysqlUser = Environment.GetEnvironmentVariable("MYSQL_USER")!;
        string mysqlPass = Environment.GetEnvironmentVariable("MYSQL_PASS")!;
        string centralHost = Environment.GetEnvironmentVariable("MYSQL_HOST_SERVER")!;
        string centralDb = Environment.GetEnvironmentVariable("MYSQL_DB_SERVER")!;
        centralConnStr = $"Server={centralHost};Port={mysqlPort};Database={centralDb};Uid={mysqlUser};Pwd={mysqlPass};";

        var hpcAddress = Environment.GetEnvironmentVariable("HPC_ADDRESS") ?? "https://hpc:5001";
        var channel = GrpcChannel.ForAddress(hpcAddress);
        hpcClient = new HpcAnalysisService.HpcAnalysisServiceClient(channel);

        int port = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "15000");
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"[SERVIDOR] A escutar na porta {port}...");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleAggregatorAsync(client));
        }
    }

    /// <summary>
    /// Lida com a comunicação de um agregador, realizando o handshake, recepção de dados,
    /// encriptação e armazenamento, e chamadas ao serviço de previsão HPC.
    /// </summary>
    static async Task HandleAggregatorAsync(TcpClient client)
    {
        try
        {
            string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();

            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            // 1. Receber chave AES encriptada com RSA
            string? encryptedAesKeyBase64 = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(encryptedAesKeyBase64))
            {
                Console.WriteLine($"[SERVIDOR] ❌ Chave AES não recebida de {clientIp}");
                client.Close(); return;
            }

            byte[] encryptedAesKey = Convert.FromBase64String(encryptedAesKeyBase64);
            byte[] aesKey;
            try
            {
                aesKey = rsa.Decrypt(encryptedAesKey, false);
                Console.WriteLine($"[SERVIDOR] 🔓 Chave AES recebida e decifrada com sucesso de {clientIp}");
            }
            catch
            {
                Console.WriteLine($"[SERVIDOR] ❌ Falha ao decifrar chave AES de {clientIp}");
                client.Close(); return;
            }

            using var conn = new MySqlConnection(centralConnStr);
            await conn.OpenAsync();

            string? line;
            string? currentAgg = null;
            bool handshakeVerificado = false;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                try
                {
                    line = AesEncryption.DecryptStringAes(line, aesKey);
                }
                catch
                {
                    Console.WriteLine($"[SERVIDOR] ❌ Linha inválida ou mal cifrada de {clientIp}");
                    continue;
                }

                Console.WriteLine($"[SERVIDOR] Linha recebida: {line}");

                if (line.StartsWith("HANDSHAKE:"))
                {
                    var handshakeParts = line.Split(':');
                    string handshakeAggId = handshakeParts[1];
                    string receivedToken = handshakeParts[2];

                    string? expectedToken = Environment.GetEnvironmentVariable($"AGG_TOKEN_{handshakeAggId}");

                    if (expectedToken is null || expectedToken != receivedToken)
                    {
                        Console.WriteLine($"[SERVIDOR] ❌ Token inválido para {handshakeAggId}. Ligação recusada.");
                        client.Close(); return;
                    }

                    Console.WriteLine($"[SERVIDOR] 🤝 Handshake com {handshakeAggId} verificado com sucesso.");
                    handshakeVerificado = true;
                    currentAgg = handshakeAggId;
                    continue;
                }

                if (!handshakeVerificado)
                {
                    Console.WriteLine($"[SERVIDOR] ❌ Dados recebidos antes de handshake válido. Ligação recusada.");
                    client.Close(); return;
                }

                if (line.StartsWith("START:")) { currentAgg = line.Split(':')[1]; Console.WriteLine($"Início de {currentAgg}"); continue; }
                if (line.StartsWith("END:")) { Console.WriteLine($"Fim de {line.Split(':')[1]}"); continue; }

                var parts = line.Split(',', 5);
                if (parts.Length != 5) { Console.WriteLine("Linha mal formatada: " + line); continue; }

                string aggId = parts[0];
                string wavyId = parts[1];
                if (!DateTime.TryParse(parts[2], out var timestamp)) { Console.WriteLine("Timestamp inválido: " + parts[2]); continue; }
                string sensor = parts[3];
                string value = parts[4];

                sensor = char.ToUpper(sensor[0]) + sensor.Substring(1).ToLower();

                string rawTable = sensor switch
                {
                    "Temperature" => "raw_temperature",
                    "Hydrophone" => "raw_hydrophone",
                    "Accelerometer" => "raw_accelerometer",
                    "Gyroscope" => "raw_gyroscope",
                    _ => null
                };

                string projectionTable = sensor switch
                {
                    "Temperature" => "projection_temperature",
                    "Hydrophone" => "projection_hydrophone",
                    "Accelerometer" => "projection_accelerometer",
                    "Gyroscope" => "projection_gyroscope",
                    _ => null
                };

                if (rawTable == null || projectionTable == null) continue;

                await dbSemaphore.WaitAsync();
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"INSERT INTO {rawTable} (wavy_id, timestamp, value, aggregator) VALUES (@w, @t, @v, @a)";
                    cmd.Parameters.AddWithValue("@w", wavyId);
                    cmd.Parameters.AddWithValue("@t", timestamp);
                    cmd.Parameters.AddWithValue("@v", value);
                    cmd.Parameters.AddWithValue("@a", aggId);
                    await cmd.ExecuteNonQueryAsync();
                }
                finally { dbSemaphore.Release(); }

                Console.WriteLine($"[{aggId}] Guardado: {wavyId} {sensor} = {value}");

                var historico = await ObterHistoricoAsync(conn, rawTable, wavyId, 15);
                bool isNumeric = double.TryParse(value, out _);

                bool isStructuredVector = sensor is "Accelerometer" or "Gyroscope";

                if (historico.Count >= 2 && (isNumeric || isStructuredVector))
                {
                    var resposta = await hpcClient!.PreverDadosAsync(new HpcForecastRequest
                    {
                        WavyId = wavyId,
                        Sensor = sensor,
                        Timestamp = timestamp.ToString("o"),
                        Historico = { historico }
                    });

                    for (int i = 0; i < resposta.PrevisoesModeloA.Count; i++)
                    {
                        using var insertPred = conn.CreateCommand();
                        insertPred.CommandText = $@"
                        INSERT INTO {projectionTable} (
                            wavy_id, base_timestamp, minuto_offset,
                            valor_previsto_modeloA, valor_previsto_modeloB,
                            modelo_mais_confiavel, classificacao, explicacao,
                            confianca, padrao_detectado, gerado_em
                        ) VALUES (
                            @w, @ts, @offset,
                            @valA, @valB,
                            @modelo, @classif, @explicacao,
                            @conf, @padrao, @now
                        )";

                        insertPred.Parameters.AddWithValue("@w", wavyId);
                        insertPred.Parameters.AddWithValue("@ts", timestamp);
                        insertPred.Parameters.AddWithValue("@offset", i + 1);
                        insertPred.Parameters.AddWithValue("@valA", resposta.PrevisoesModeloA[i]);
                        insertPred.Parameters.AddWithValue("@valB", resposta.PrevisoesModeloB[i]);
                        insertPred.Parameters.AddWithValue("@modelo", resposta.ModeloMaisConfiavel);
                        insertPred.Parameters.AddWithValue("@classif", resposta.Classificacao);
                        insertPred.Parameters.AddWithValue("@explicacao", resposta.Explicacao);
                        insertPred.Parameters.AddWithValue("@conf", resposta.Confianca);
                        insertPred.Parameters.AddWithValue("@padrao", resposta.PadraoDetectado);
                        insertPred.Parameters.AddWithValue("@now", DateTime.UtcNow);

                        await insertPred.ExecuteNonQueryAsync();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SERVIDOR] Erro: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    /// <summary>
    /// Obtém os últimos 'n' valores registados de um determinado sensor de um WAVY.
    /// Caso o valor seja vetorial (e.g. Acelerómetro), calcula a magnitude.
    /// </summary>
    static async Task<List<double>> ObterHistoricoAsync(MySqlConnection conn, string table, string wavyId, int n)
    {
        var lista = new List<double>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT value FROM {table} WHERE wavy_id=@w ORDER BY timestamp DESC LIMIT @n";
        cmd.Parameters.AddWithValue("@w", wavyId);
        cmd.Parameters.AddWithValue("@n", n);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            string raw = reader["value"].ToString()!;

            if (double.TryParse(raw, out double simpleVal))
            {
                lista.Add(simpleVal);
                continue;
            }

            try
            {
                raw = raw.Replace(" ", "").Replace("\"", "");
                var parts = raw.Split(',');

                double x = 0, y = 0, z = 0;
                foreach (var p in parts)
                {
                    var kv = p.Split(':');
                    if (kv.Length != 2) continue;
                    var axis = kv[0].ToUpper();
                    var valStr = kv[1].Replace(",", ".");

                    if (!double.TryParse(valStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
                        continue;

                    if (axis == "X") x = val;
                    else if (axis == "Y") y = val;
                    else if (axis == "Z") z = val;
                }

                double magnitude = Math.Sqrt(x * x + y * y + z * z);
                lista.Add(magnitude);
            }
            catch
            {
                continue;
            }
        }

        lista.Reverse();
        return lista;
    }
}