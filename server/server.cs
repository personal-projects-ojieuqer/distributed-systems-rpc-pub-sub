using MySql.Data.MySqlClient;
using System.Collections.Concurrent;

class Program
{
    private static readonly object centralDbLock = new();
    private static readonly ConcurrentDictionary<string, DateTime> lastSyncTimestamps = new();

    static void Main()
    {
        Console.WriteLine("Servidor iniciado e pronto para sincronizar com múltiplos agregadores.");

        // Carrega as configs dos agregadores a partir das env vars
        string[] aggregators = Environment.GetEnvironmentVariable("AGG_IDS")!.Split(',');

        string mysqlPort = Environment.GetEnvironmentVariable("MYSQL_PORT")!;
        string mysqlUser = Environment.GetEnvironmentVariable("MYSQL_USER")!;
        string mysqlPass = Environment.GetEnvironmentVariable("MYSQL_PASS")!;
        string centralHost = Environment.GetEnvironmentVariable("MYSQL_HOST_SERVER")!;
        string centralDb = Environment.GetEnvironmentVariable("MYSQL_DB_SERVER")!;
        string centralConnStr = $"Server={centralHost};Port={mysqlPort};Database={centralDb};Uid={mysqlUser};Pwd={mysqlPass};";


        foreach (string aggId in aggregators)
        {
            string handshake = $"{aggId}:{Environment.GetEnvironmentVariable($"AGG_TOKEN_{aggId}")}";
            string aggHost = Environment.GetEnvironmentVariable($"MYSQL_HOST_{aggId}")!;
            string aggDb = Environment.GetEnvironmentVariable($"MYSQL_DB_{aggId}")!;
            string aggConnStr = $"Server={aggHost};Port={mysqlPort};Database={aggDb};Uid={mysqlUser};Pwd={mysqlPass};";

            lastSyncTimestamps.TryAdd(aggId, DateTime.MinValue);

            var thread = new Thread(() => SyncAggregator(aggId, aggConnStr, centralConnStr, handshake));
            thread.Start();
        }

        // Mantém o programa vivo
        while (true) Thread.Sleep(10000);
    }

    static void SyncAggregator(string aggId, string aggConnStr, string centralConnStr, string handshake)
    {
        while (true)
        {
            int registrosSincronizados = 0;

            try
            {
                if (!ValidateAggregatorToken(aggId, handshake))
                {
                    Console.WriteLine($"Token inválido para {aggId}. Sincronização recusada.");
                    return;
                }

                Console.WriteLine($"Token Válido para {aggId}. Sincronização Aceite.");

                using var aggConn = new MySqlConnection(aggConnStr);
                aggConn.Open();

                DateTime lastSync = lastSyncTimestamps[aggId];

                string query = @"SELECT wavy_id, timestamp, sensor, value 
                             FROM sensor_data 
                             WHERE timestamp > @lastSync 
                             ORDER BY timestamp";

                using var cmd = new MySqlCommand(query, aggConn);
                cmd.Parameters.AddWithValue("@lastSync", lastSync);

                using var reader = cmd.ExecuteReader();
                DateTime maxTimestamp = lastSync;

                while (reader.Read())
                {
                    try
                    {
                        string wavyId = reader.GetString(0);
                        DateTime timestamp = reader.GetDateTime(1);
                        string sensor = reader.GetString(2);
                        string value = reader.GetString(3);

                        if (string.IsNullOrWhiteSpace(wavyId) || string.IsNullOrWhiteSpace(sensor) || string.IsNullOrWhiteSpace(value))
                        {
                            Console.WriteLine($"[{aggId}] Ignorado dado inválido (campos em branco)");
                            continue;
                        }

                        if (timestamp > maxTimestamp)
                            maxTimestamp = timestamp;

                        lock (centralDbLock)
                        {
                            using var centralConn = new MySqlConnection(centralConnStr);
                            centralConn.Open();

                            string insert = @"INSERT INTO central_sensor_data (wavy_id, timestamp, sensor, value, aggregator)
                                          VALUES (@wavy_id, @timestamp, @sensor, @value, @aggregator)";

                            using var insertCmd = new MySqlCommand(insert, centralConn);
                            insertCmd.Parameters.AddWithValue("@wavy_id", wavyId);
                            insertCmd.Parameters.AddWithValue("@timestamp", timestamp);
                            insertCmd.Parameters.AddWithValue("@sensor", sensor);
                            insertCmd.Parameters.AddWithValue("@value", value);
                            insertCmd.Parameters.AddWithValue("@aggregator", aggId);

                            insertCmd.ExecuteNonQuery();
                            registrosSincronizados++;
                        }
                    }
                    catch (Exception exInner)
                    {
                        Console.WriteLine($"[{aggId}] Erro ao processar registo: {exInner.Message}");
                    }
                }

                lastSyncTimestamps[aggId] = maxTimestamp;
                Console.WriteLine($"[{aggId}] {registrosSincronizados} registos novos recebidos até {maxTimestamp:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{aggId}] Erro geral de sincronização: {ex.Message}");
            }

            Thread.Sleep(10000); // Sincroniza de 10 em 10 segundos
        }
    }
    static bool ValidateAggregatorToken(string aggId, string receivedHandshake)
    {
        string? expectedToken = Environment.GetEnvironmentVariable($"AGG_TOKEN_{aggId}");
        string expectedHandshake = $"{aggId}:{expectedToken}";
        return receivedHandshake == expectedHandshake;
    }
}
