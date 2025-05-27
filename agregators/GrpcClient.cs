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

        public async Task<SensorResponse?> ProcessarAsync(string wavyId, string sensor, string value, string timestamp, List<double> recentValues)
        {
            var request = new SensorRequest
            {
                WavyId = wavyId,
                Sensor = sensor,
                Value = value,
                Timestamp = timestamp
            };
            request.RecentValues.AddRange(recentValues);

            try
            {
                var response = await _client.FilterSensorAsync(request);
                if (!response.IsValid)
                {
                    Console.WriteLine($"[gRPC] Valor inválido filtrado: {value}");
                    return null;
                }

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[gRPC] Falha na chamada RPC: {ex.Message}");
                return null;
            }
        }

    }
}
