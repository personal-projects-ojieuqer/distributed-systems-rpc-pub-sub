using Grpc.Net.Client;
using GrpcClient;

namespace agregators
{
    public class GrpcClient
    {
        private readonly PreprocessService.PreprocessServiceClient _client;

        public GrpcClient(string grpcAddress)
        {
            var channel = GrpcChannel.ForAddress(grpcAddress);
            _client = new PreprocessService.PreprocessServiceClient(channel);
        }

        public async Task<string> FiltrarOuNormalizarAsync(string wavyId, string sensor, string value)
        {
            var request = new SensorRequest
            {
                WavyId = wavyId,
                Sensor = sensor,
                Value = value
            };

            try
            {
                var response = await _client.FilterSensorAsync(request);
                if (!response.IsValid)
                {
                    Console.WriteLine($"[gRPC] Valor inválido filtrado: {value}");
                    return "Dado Ignorado";
                }

                return response.ProcessedValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[gRPC] Falha na chamada RPC: {ex.Message}");
                return "Dado Ignorado";
            }
        }
    }
}
