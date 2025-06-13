using Grpc.Net.Client;
using GrpcClient;

namespace agregators
{
    /// <summary>
    /// Cliente gRPC que comunica com o serviço de pré-processamento (PreprocessService),
    /// responsável por validar e enriquecer dados de sensores recebidos de dispositivos WAVY.
    /// </summary>
    public class GrpcClient
    {
        // Cliente gRPC gerado automaticamente a partir do ficheiro .proto
        private readonly PreprocessService.PreprocessServiceClient _client;

        /// <summary>
        /// Construtor que cria o canal de comunicação gRPC com o serviço remoto.
        /// </summary>
        /// <param name="grpcAddress">Endereço do servidor gRPC (ex: http://localhost:5000).</param>
        public GrpcClient(string grpcAddress)
        {
            // Criação do canal para o endereço fornecido
            var channel = GrpcChannel.ForAddress(grpcAddress);

            // Instanciação do cliente gerado para o serviço
            _client = new PreprocessService.PreprocessServiceClient(channel);
        }

        /// <summary>
        /// Envia dados de um sensor para o serviço gRPC para validação e enriquecimento estatístico.
        /// </summary>
        /// <param name="wavyId">Identificador do dispositivo WAVY.</param>
        /// <param name="sensor">Nome do sensor (ex: Temperature, Gyroscope).</param>
        /// <param name="value">Valor atual lido do sensor.</param>
        /// <param name="timestamp">Momento da leitura no formato ISO 8601.</param>
        /// <param name="recentValues">Lista de valores recentes do mesmo sensor para contexto.</param>
        /// <returns>Objeto <see cref="SensorResponse"/> se o valor for válido; caso contrário, null.</returns>
        public async Task<SensorResponse?> ProcessarAsync(string wavyId, string sensor, string value, string timestamp, List<double> recentValues)
        {
            // Cria o pedido com os dados a enviar ao serviço
            var request = new SensorRequest
            {
                WavyId = wavyId,
                Sensor = sensor,
                Value = value,
                Timestamp = timestamp
            };

            // Adiciona os valores recentes para análise de tendências
            request.RecentValues.AddRange(recentValues);

            try
            {
                // Envio assíncrono do pedido e receção da resposta
                var response = await _client.FilterSensorAsync(request);

                // Caso o valor seja considerado inválido pelo serviço
                if (!response.IsValid)
                {
                    Console.WriteLine($"[gRPC] Valor inválido filtrado: {value}");
                    return null;
                }

                return response;
            }
            catch (Exception ex)
            {
                // Tratamento de falha na comunicação gRPC
                Console.WriteLine($"[gRPC] Falha na chamada RPC: {ex.Message}");
                return null;
            }
        }
    }
}
