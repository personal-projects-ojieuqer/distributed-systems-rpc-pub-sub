using MySql.Data.MySqlClient;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Classe responsável por enviar dados de um agregador para o servidor central via TCP.
/// </summary>
public static class AggregatorSender
{
    /// <summary>
    /// Envia dados do agregador para o servidor central. Apenas dados registados após o último minuto serão enviados.
    /// </summary>
    /// <param name="aggregatorId">Identificador do agregador.</param>
    /// <param name="localConnStr">String de ligação à base de dados local do agregador.</param>
    /// <param name="servidorIp">Endereço IP ou hostname do servidor central.</param>
    /// <param name="servidorPorta">Porta do servidor onde o serviço está a escutar.</param>
    public static async Task EnviarDadosParaServidorAsync(string aggregatorId, string localConnStr, string servidorIp, int servidorPorta)
    {
        Console.WriteLine($"[AGGREGADOR {aggregatorId}] 🟡 Início do envio para o servidor {servidorIp}:{servidorPorta}");
        try
        {
            // Abre ligação à base de dados local
            using var conn = new MySqlConnection(localConnStr);
            await conn.OpenAsync();

            // Define intervalo para sincronização (último minuto)
            DateTime ultimoSync = DateTime.Now.AddMinutes(-1);
            string query = @"SELECT wavy_id, timestamp, sensor, value 
                             FROM sensor_data 
                             WHERE timestamp > @lastSync 
                             ORDER BY timestamp";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@lastSync", ultimoSync);

            using var reader = await cmd.ExecuteReaderAsync();
            var linhasParaEnviar = new List<string>();

            // Recolhe todos os registos novos desde a última sincronização
            while (await reader.ReadAsync())
            {
                string wavyId = reader.GetString(0);
                DateTime timestamp = reader.GetDateTime(1);
                string sensor = reader.GetString(2);
                string value = reader.GetString(3);

                // Formata os dados para envio
                string linha = $"{aggregatorId},{wavyId},{timestamp:o},{sensor},{value}";
                linhasParaEnviar.Add(linha);
            }

            if (linhasParaEnviar.Count == 0)
            {
                Console.WriteLine($"[AGGREGADOR {aggregatorId}] Nenhum dado novo para enviar.");
                return; // Nada para enviar, termina aqui
            }

            // Obtém o token de autenticação do agregador a partir da variável de ambiente
            string token = Environment.GetEnvironmentVariable($"AGG_TOKEN_{aggregatorId}") ?? "";
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine($"[AGGREGADOR {aggregatorId}] Token não encontrado. A abortar o envio.");
                return; // Sem token, não prossegue
            }

            using TcpClient client = new();
            await client.ConnectAsync("server_app", servidorPorta); // Liga-se ao servidor

            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            // Realiza handshake com o servidor para autenticação
            await writer.WriteLineAsync($"HANDSHAKE:{aggregatorId}:{token}");

            // Inicia transmissão
            await writer.WriteLineAsync($"START:{aggregatorId}");

            // Envia todos os dados linha a linha
            foreach (var linha in linhasParaEnviar)
            {
                //AQUIII
                Console.WriteLine($"[AGGREGADOR {aggregatorId}] A enviar: {linha}");
                await writer.WriteLineAsync(linha);
            }

            // Finaliza transmissão
            Console.WriteLine($"[AGGREGADOR {aggregatorId}] Fim da transmissão.");

            await writer.WriteLineAsync($"END:{aggregatorId}");

            Console.WriteLine($"[AGGREGADOR {aggregatorId}] Enviados {linhasParaEnviar.Count} registos ao servidor.");
        }
        catch (Exception ex)
        {
            // Tratamento de erros de forma simples com mensagem no terminal
            Console.WriteLine($"[AGGREGADOR {aggregatorId}] Erro ao enviar dados: {ex.Message}");
        }
    }
}
