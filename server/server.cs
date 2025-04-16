using MySql.Data.MySqlClient;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Classe principal do servidor que escuta conexões TCP de agregadores,
/// realiza validação através de handshake e armazena os dados recebidos numa base de dados MySQL central.
/// </summary>
class Program
{
    // Objeto de bloqueio para evitar acesso simultâneo à base de dados central
    private static readonly object centralDbLock = new();
    // String de ligação à base de dados central
    private static string centralConnStr = string.Empty;

    /// <summary>
    /// Método principal que inicializa o servidor TCP, configura a string de ligação à base de dados MySQL
    /// e começa a escutar ligações de agregadores.
    /// </summary>
    static void Main()
    {
        Console.WriteLine("Servidor iniciado e pronto para receber dados dos agregadores via TCP.");

        // Lê variáveis de ambiente para configurar a ligação à base de dados central
        string mysqlPort = Environment.GetEnvironmentVariable("MYSQL_PORT")!;
        string mysqlUser = Environment.GetEnvironmentVariable("MYSQL_USER")!;
        string mysqlPass = Environment.GetEnvironmentVariable("MYSQL_PASS")!;
        string centralHost = Environment.GetEnvironmentVariable("MYSQL_HOST_SERVER")!;
        string centralDb = Environment.GetEnvironmentVariable("MYSQL_DB_SERVER")!;

        // Monta a connection string
        centralConnStr = $"Server={centralHost};Port={mysqlPort};Database={centralDb};Uid={mysqlUser};Pwd={mysqlPass};";

        // Lê a porta onde o servidor irá escutar (default: 15000)
        int port = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "15000");

        // Cria e inicia o listener TCP
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"[SERVIDOR] A escutar na porta {port}...");

        // Ciclo infinito que aceita conexões dos agregadores
        while (true)
        {
            var client = listener.AcceptTcpClient();
            _ = Task.Run(() => HandleAggregator(client));
        }
    }

    /// <summary>
    /// Método responsável por tratar a comunicação com um agregador específico.
    /// Valida o handshake, interpreta os dados recebidos e insere-os na base de dados central.
    /// </summary>
    /// <param name="client">Objeto TcpClient que representa a ligação com o agregador.</param>
    static void HandleAggregator(TcpClient client)
    {
        try
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var conn = new MySqlConnection(centralConnStr);
            conn.Open();   // Abre a ligação à base de dados central

            string? line;
            string? currentAgg = null;

            // Lê as linhas enviadas pelo agregador
            while ((line = reader.ReadLine()) != null)
            {
                // Validação do handshake
                if (line.StartsWith("HANDSHAKE:"))
                {
                    var handshakeParts = line.Split(':');
                    string handshakeAggId = handshakeParts[1];
                    string receivedToken = handshakeParts[2];

                    // Recupera o token esperado a partir das variáveis de ambiente
                    string? expectedToken = Environment.GetEnvironmentVariable($"AGG_TOKEN_{handshakeAggId}");

                    if (expectedToken is null || expectedToken != receivedToken)
                    {
                        Console.WriteLine($"\nToken inválido para {handshakeAggId}. Ligação recusada.");
                        client.Close();
                        return;
                    }

                    Console.WriteLine($"\nHandshake com {handshakeAggId} verificado com sucesso.");
                    continue;
                }

                // Início do envio de dados
                if (line.StartsWith("START:"))
                {
                    currentAgg = line.Split(':')[1];
                    Console.WriteLine($"\n🟢 Início do envio de dados de {currentAgg}");
                    continue;
                }

                // Fim do envio de dados
                if (line.StartsWith("END:"))
                {
                    string endAgg = line.Split(':')[1];
                    Console.WriteLine($"🔴 Fim do envio de dados de {endAgg}\n");
                    continue;
                }

                // Espera-se o formato: aggId,wavyId,timestamp,sensor,value
                var parts = line.Split(',', 5);
                if (parts.Length != 5)
                {
                    Console.WriteLine("Linha mal formatada: " + line);
                    continue;
                }

                string aggId = parts[0];
                string wavyId = parts[1];

                // Verifica se o timestamp é válido
                if (!DateTime.TryParse(parts[2], out var timestamp))
                {
                    Console.WriteLine($"Timestamp inválido: {parts[2]}");
                    continue;
                }
                string sensor = parts[3];
                string value = parts[4];

                // Insere os dados na base de dados com proteção contra acessos concorrentes
                lock (centralDbLock)
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = @"INSERT INTO central_sensor_data (wavy_id, timestamp, sensor, value, aggregator)
                                        VALUES (@wavy_id, @timestamp, @sensor, @value, @aggregator)";
                    cmd.Parameters.AddWithValue("@wavy_id", wavyId);
                    cmd.Parameters.AddWithValue("@timestamp", timestamp);
                    cmd.Parameters.AddWithValue("@sensor", sensor);
                    cmd.Parameters.AddWithValue("@value", value);
                    cmd.Parameters.AddWithValue("@aggregator", aggId);
                    cmd.ExecuteNonQuery();
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
            // Fecha a ligação com o agregador
            client.Close();
        }
    }
}
