// Program.cs adaptado para usar HPC após guardar os dados
// Program.cs adaptado para guardar previsões e dados RAW em tabelas separadas por tipo de sensor
using Grpc.Net.Client;
using HpcService;
using MySql.Data.MySqlClient;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    private static readonly SemaphoreSlim dbSemaphore = new(1, 1);
    private static string centralConnStr = string.Empty;
    private static HpcAnalysisService.HpcAnalysisServiceClient? hpcClient;

    static async Task Main()
    {
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

    static async Task HandleAggregatorAsync(TcpClient client)
    {
        try
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var conn = new MySqlConnection(centralConnStr);
            await conn.OpenAsync();

            string? line;
            string? currentAgg = null;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                //AQUIIII
                Console.WriteLine($"[SERVIDOR] Linha recebida: {line}");
                //if (line.StartsWith("HANDSHAKE:"))
                //{
                //    var handshakeParts = line.Split(':');
                //    string id = handshakeParts[1];
                //    string token = handshakeParts[2];
                //    string? expected = Environment.GetEnvironmentVariable($"AGG_TOKEN_{id}");
                //    if (expected is null || expected != token)
                //    {
                //        Console.WriteLine($"Token inválido para {id}. Ligação recusada.");
                //        client.Close(); return;
                //    }
                //    Console.WriteLine($"Handshake com {id} verificado.");
                //    continue;
                //}

                if (line.StartsWith("START:")) { currentAgg = line.Split(':')[1]; Console.WriteLine($"Início de {currentAgg}"); continue; }
                if (line.StartsWith("END:")) { Console.WriteLine($" Fim de {line.Split(':')[1]}"); continue; }

                var parts = line.Split(',', 5);
                if (parts.Length != 5) { Console.WriteLine("Linha mal formatada: " + line); continue; }

                string aggId = parts[0];
                string wavyId = parts[1];
                if (!DateTime.TryParse(parts[2], out var timestamp)) { Console.WriteLine("Timestamp inválido: " + parts[2]); continue; }
                string sensor = parts[3];
                string value = parts[4];

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

                var historico = await ObterHistoricoAsync(conn, rawTable, wavyId, 5);
                if (historico.Count >= 2 && double.TryParse(value, out _))
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
            if (double.TryParse(reader["value"].ToString(), out double val))
                lista.Add(val);
        }
        lista.Reverse();
        return lista;
    }
}

