using MySql.Data.MySqlClient;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Classe principal que representa o servidor TCP que escuta ligações de agregadores e armazena os dados recebidos numa base de dados MySQL.
/// </summary>
class Program
{
    // Semáforo utilizado para controlar o acesso concorrente à base de dados
    private static readonly SemaphoreSlim dbSemaphore = new(1, 1);

    // String de ligação à base de dados central
    private static string centralConnStr = string.Empty;

    /// <summary>
    /// Ponto de entrada do programa. Inicializa o servidor TCP e começa a escutar ligações de agregadores.
    /// </summary>
    static async Task Main()
    {
        Console.WriteLine("Servidor iniciado e pronto para receber dados dos agregadores via TCP.");

        // Obtém variáveis de ambiente para configurar a string de ligação
        string mysqlPort = Environment.GetEnvironmentVariable("MYSQL_PORT")!;
        string mysqlUser = Environment.GetEnvironmentVariable("MYSQL_USER")!;
        string mysqlPass = Environment.GetEnvironmentVariable("MYSQL_PASS")!;
        string centralHost = Environment.GetEnvironmentVariable("MYSQL_HOST_SERVER")!;
        string centralDb = Environment.GetEnvironmentVariable("MYSQL_DB_SERVER")!;

        // Monta a string de ligação à base de dados
        centralConnStr = $"Server={centralHost};Port={mysqlPort};Database={centralDb};Uid={mysqlUser};Pwd={mysqlPass};Pooling=true;Min Pool Size=0;Max Pool Size=100;";

        // Porta onde o servidor irá escutar as ligações
        int port = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "15000");

        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"[SERVIDOR] A escutar na porta {port}...");

        // Ciclo principal para aceitar novas ligações de clientes
        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleAggregatorAsync(client)); // Trata cada ligação num processo separado
        }
    }

    /// <summary>
    /// Trata a ligação de um agregador, processa as mensagens recebidas e insere os dados na base de dados.
    /// </summary>
    /// <param name="client">Instância de TcpClient que representa a ligação com o agregador.</param>
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

            // Lê as mensagens linha a linha enquanto houver dados
            while ((line = await reader.ReadLineAsync()) != null)
            {
                // Validação do handshake com token do agregador
                if (line.StartsWith("HANDSHAKE:"))
                {
                    var handshakeParts = line.Split(':');
                    string handshakeAggId = handshakeParts[1];
                    string receivedToken = handshakeParts[2];

                    // Procura o token esperado nas variáveis de ambiente
                    string? expectedToken = Environment.GetEnvironmentVariable($"AGG_TOKEN_{handshakeAggId}");

                    // Verifica se o token é válido
                    if (expectedToken is null || expectedToken != receivedToken)
                    {
                        Console.WriteLine($"\nToken inválido para {handshakeAggId}. Ligação recusada.");
                        client.Close(); // Encerra a ligação se o token não for válido
                        return;
                    }

                    Console.WriteLine($"\nHandshake com {handshakeAggId} verificado com sucesso.");
                    continue;
                }

                // Indica o início da transmissão de dados pelo agregador
                if (line.StartsWith("START:"))
                {
                    currentAgg = line.Split(':')[1];
                    Console.WriteLine($"\n🟢 Início do envio de dados de {currentAgg}");
                    continue;
                }

                // Indica o fim da transmissão de dados
                if (line.StartsWith("END:"))
                {
                    string endAgg = line.Split(':')[1];
                    Console.WriteLine($"🔴 Fim do envio de dados de {endAgg}\n");
                    continue;
                }

                // Espera-se que os dados tenham o formato: aggId,wavyId,timestamp,sensor,value
                var parts = line.Split(',', 5);
                if (parts.Length != 5)
                {
                    Console.WriteLine("Linha mal formatada: " + line);
                    continue;
                }

                string aggId = parts[0];
                string wavyId = parts[1];

                // Tenta converter o timestamp para DateTime
                if (!DateTime.TryParse(parts[2], out var timestamp))
                {
                    Console.WriteLine($"Timestamp inválido: {parts[2]}");
                    continue;
                }

                string sensor = parts[3];
                string value = parts[4];

                // Aguarda acesso exclusivo à base de dados
                await dbSemaphore.WaitAsync();
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"INSERT INTO central_sensor_data (wavy_id, timestamp, sensor, value, aggregator)
                                        VALUES (@wavy_id, @timestamp, @sensor, @value, @aggregator)";
                    cmd.Parameters.AddWithValue("@wavy_id", wavyId);
                    cmd.Parameters.AddWithValue("@timestamp", timestamp);
                    cmd.Parameters.AddWithValue("@sensor", sensor);
                    cmd.Parameters.AddWithValue("@value", value);
                    cmd.Parameters.AddWithValue("@aggregator", aggId);

                    await cmd.ExecuteNonQueryAsync(); // Executa a inserção de forma assíncrona
                }
                finally
                {
                    dbSemaphore.Release(); // Liberta o semáforo para que outro processo possa aceder
                }

                Console.WriteLine($"[{aggId}] Inserido: {wavyId} | {sensor} = {value}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SERVIDOR] Erro ao processar dados do agregador: {ex.Message}");
        }
        finally
        {
            client.Close(); // Garante que a ligação seja encerrada
        }
    }
}
