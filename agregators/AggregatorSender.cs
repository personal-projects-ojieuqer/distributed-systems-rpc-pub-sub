using MySql.Data.MySqlClient;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Classe responsável por enviar dados de um agregador para o servidor central via TCP,
/// utilizando encriptação híbrida (AES simétrico + RSA assimétrico).
/// </summary>
public static class AggregatorSender
{
    /// <summary>
    /// Envia os dados de sensores registados localmente para o servidor central, 
    /// cifrando-os com AES e transmitindo-os através de uma ligação TCP segura.
    /// </summary>
    /// <param name="aggregatorId">Identificador do agregador.</param>
    /// <param name="localConnStr">String de ligação à base de dados local.</param>
    /// <param name="servidorIp">Endereço IP ou nome do servidor de destino.</param>
    /// <param name="servidorPorta">Porta TCP do servidor.</param>
    public static async Task EnviarDadosParaServidorAsync(string aggregatorId, string localConnStr, string servidorIp, int servidorPorta)
    {
        var aesKey = Encoding.UTF8.GetBytes("sistem-dist-2025"); // Chave AES-128 (16 bytes)

        Console.WriteLine($"[AGGREGADOR {aggregatorId}] 🟡 Início do envio para o servidor {servidorIp}:{servidorPorta}");

        try
        {
            using var conn = new MySqlConnection(localConnStr);
            await conn.OpenAsync();

            // Define o tempo mínimo de sincronização (último minuto)
            DateTime ultimoSync = DateTime.Now.AddMinutes(-1);
            var linhasParaEnviar = new List<string>();

            // Tabelas associadas aos diferentes sensores
            string[] tabelas = new[]
            {
                "temperature_data_raw",
                "hydrophone_data_raw",
                "gyroscope_data_raw",
                "accelerometer_data_raw"
            };

            // Recolhe os dados mais recentes de cada sensor
            foreach (var tabela in tabelas)
            {
                string sensor = tabela.Replace("_data_raw", "", StringComparison.OrdinalIgnoreCase);
                string query = $@"SELECT wavy_id, timestamp, value 
                                  FROM {tabela}
                                  WHERE timestamp > @lastSync 
                                  ORDER BY timestamp";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@lastSync", ultimoSync);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string wavyId = reader.GetString(0);
                    DateTime timestamp = reader.GetDateTime(1);
                    string value = reader.GetString(2);

                    string linha = $"{aggregatorId},{wavyId},{timestamp:o},{sensor},{value}";
                    linhasParaEnviar.Add(linha);
                }
                await reader.CloseAsync();
            }

            // Se não houver dados a enviar, termina
            if (linhasParaEnviar.Count == 0)
            {
                Console.WriteLine($"[AGGREGADOR {aggregatorId}] Nenhum dado novo para enviar.");
                return;
            }

            // Obtém o token de autenticação do agregador
            string token = Environment.GetEnvironmentVariable($"AGG_TOKEN_{aggregatorId}") ?? "";
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine($"[AGGREGADOR {aggregatorId}] Token não encontrado. A abortar o envio.");
                return;
            }

            // Lê a chave pública RSA do ficheiro XML
            string publicKeyXml = File.ReadAllText("autorizacoes/publicKey.xml");
            using var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicKeyXml);

            // Encripta a chave AES com RSA (encriptação assimétrica)
            byte[] encryptedKey = rsa.Encrypt(aesKey, false);
            string encryptedKeyBase64 = Convert.ToBase64String(encryptedKey);

            // Estabelece a ligação TCP ao servidor
            using TcpClient client = new();
            await client.ConnectAsync(servidorIp, servidorPorta);
            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            // Envia a chave AES encriptada (primeira linha)
            await writer.WriteLineAsync(encryptedKeyBase64);
            Console.WriteLine($"[AGGREGADOR {aggregatorId}] 🔐 Chave AES cifrada enviada");

            // Frases de controlo de sessão
            string handshake = $"HANDSHAKE:{aggregatorId}:{token}";
            string start = $"START:{aggregatorId}";
            string end = $"END:{aggregatorId}";

            // Envia handshake cifrado
            Console.WriteLine($"[DEBUG] Enviando handshake: {handshake}");
            await writer.WriteLineAsync(AesEncryptionAg.EncryptStringAes(handshake, aesKey));

            // Sinaliza início da transmissão
            Console.WriteLine($"[DEBUG] Enviando início da transmissão: {start}");
            await writer.WriteLineAsync(AesEncryptionAg.EncryptStringAes(start, aesKey));

            // Envia cada linha cifrada com AES
            foreach (var linha in linhasParaEnviar)
            {
                string cifrada = AesEncryptionAg.EncryptStringAes(linha, aesKey);
                Console.WriteLine($"[AGGREGADOR {aggregatorId}] 🔒 Original: {linha}");
                Console.WriteLine($"[AGGREGADOR {aggregatorId}] 🔐 Cifrada: {cifrada}");
                await writer.WriteLineAsync(cifrada);
            }

            // Finaliza transmissão com marca de fim
            Console.WriteLine($"[DEBUG] Enviando fim da transmissão: {end}");
            await writer.WriteLineAsync(AesEncryptionAg.EncryptStringAes(end, aesKey));

            Console.WriteLine($"[AGGREGADOR {aggregatorId}] Enviados {linhasParaEnviar.Count} registos ao servidor.");
        }
        catch (Exception ex)
        {
            // Registo de falha durante o envio
            Console.WriteLine($"[AGGREGADOR {aggregatorId}] Erro ao enviar dados: {ex.Message}");
        }
    }
}
