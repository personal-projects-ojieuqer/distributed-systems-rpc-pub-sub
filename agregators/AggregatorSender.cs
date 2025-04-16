using MySql.Data.MySqlClient;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Classe responsável por enviar os dados recolhidos pelo agregador para o servidor central.
/// </summary>
public static class AggregatorSender
{
    /// <summary>
    /// Envia os dados registados localmente (na base de dados do agregador) para o servidor central via TCP.
    /// Os dados enviados são aqueles que foram registados após a última sincronização (últimos 60 segundos).
    /// Inclui o envio de um handshake com token para autenticação, seguido dos dados formatados.
    /// </summary>
    /// <param name="aggregatorId">Identificador do agregador.</param>
    /// <param name="localConnStr">String de ligação à base de dados local do agregador.</param>
    /// <param name="servidorIp">Endereço IP do servidor central (não utilizado diretamente neste exemplo).</param>
    /// <param name="servidorPorta">Porta TCP na qual o servidor central está a escutar.</param>
    public static void EnviarDadosParaServidor(string aggregatorId, string localConnStr, string servidorIp, int servidorPorta)
    {
        try
        {
            // Estabelece ligação à base de dados local do agregador
            using var conn = new MySqlConnection(localConnStr);
            conn.Open();

            // Define o intervalo temporal para sincronização (último minuto)
            DateTime ultimoSync = DateTime.UtcNow.AddMinutes(-1);
            string query = @"SELECT wavy_id, timestamp, sensor, value 
                             FROM sensor_data 
                             WHERE timestamp > @lastSync 
                             ORDER BY timestamp";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@lastSync", ultimoSync);

            using var reader = cmd.ExecuteReader();
            var linhasParaEnviar = new List<string>();

            // Extrai os registos e formata para envio
            while (reader.Read())
            {
                string wavyId = reader.GetString(0);
                DateTime timestamp = reader.GetDateTime(1);
                string sensor = reader.GetString(2);
                string value = reader.GetString(3);

                // Formato: agregador,wavy,timestamp,sensor,valor
                string linha = $"{aggregatorId},{wavyId},{timestamp:o},{sensor},{value}";
                linhasParaEnviar.Add(linha);
            }

            // Caso não haja novos dados, informa e termina
            if (linhasParaEnviar.Count == 0)
            {
                Console.WriteLine($"[AGGREGADOR {aggregatorId}] Nenhum dado novo para enviar.");
                return;
            }

            // Lê o token necessário para autenticação com o servidor (definido como variável de ambiente)
            string token = Environment.GetEnvironmentVariable($"AGG_TOKEN_{aggregatorId}") ?? "";
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine($"[AGGREGADOR {aggregatorId}] Token não encontrado. A abortar o envio.");
                return;
            }

            // Estabelece ligação com o servidor central via TCP
            using TcpClient client = new TcpClient();
            client.Connect("server_app", servidorPorta); // "server_app" é o nome DNS ou IP do servidor

            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            // Envia handshake com token e marca o início da transmissão
            writer.WriteLine($"HANDSHAKE:{aggregatorId}:{token}");
            writer.WriteLine($"START:{aggregatorId}");

            // Envia cada linha de dados
            foreach (var linha in linhasParaEnviar)
            {
                writer.WriteLine(linha);
            }

            // Marca o fim da transmissão
            writer.WriteLine($"END:{aggregatorId}");

            Console.WriteLine($"[AGGREGADOR {aggregatorId}] Enviados {linhasParaEnviar.Count} registos ao servidor.");
        }
        catch (Exception ex)
        {
            // Em caso de erro, escreve mensagem no log
            Console.WriteLine($"[AGGREGADOR {aggregatorId}] Erro ao enviar dados: {ex.Message}");
        }
    }
}
