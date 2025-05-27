using Grpc.Core;
using PreProcessRPC;
using System.Globalization;

public class PreprocessServiceImpl : PreprocessService.PreprocessServiceBase
{
    public override Task<SensorResponse> FilterSensor(SensorRequest request, ServerCallContext context)
    {
        SensorResponse response = request.Sensor switch
        {
            "Temperature" => ProcessTemperature(request),
            "Hydrophone" => ProcessHydrophone(request),
            "Accelerometer" => ProcessVector(request),
            "Gyroscope" => ProcessVector(request),
            _ => new SensorResponse { IsValid = false, ProcessedValue = $"UNSUPPORTED_SENSOR:{request.Sensor}" }
        };

        // Log opcional
        Console.WriteLine($"[gRPC] {request.Sensor} ({request.Value}) → Valid: {response.IsValid}, Processed: {response.ProcessedValue}");

        return Task.FromResult(response);
    }

    private SensorResponse ProcessTemperature(SensorRequest request)
    {
        if (double.TryParse(request.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double temp))
        {
            bool valid = temp >= 5 && temp <= 35;
            return new SensorResponse
            {
                IsValid = valid,
                ProcessedValue = Math.Round(temp, 2).ToString(CultureInfo.InvariantCulture)
            };
        }

        return new SensorResponse { IsValid = false, ProcessedValue = "INVALID_TEMPERATURE" };
    }

    private SensorResponse ProcessHydrophone(SensorRequest request)
    {
        if (double.TryParse(request.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double db))
        {
            bool valid = db >= 100 && db <= 140;
            return new SensorResponse
            {
                IsValid = valid,
                ProcessedValue = Math.Round(db, 1).ToString(CultureInfo.InvariantCulture)
            };
        }

        return new SensorResponse { IsValid = false, ProcessedValue = "INVALID_HYDROPHONE" };
    }

    private SensorResponse ProcessVector(SensorRequest request)
    {
        try
        {
            string cleaned = request.Value.Replace("\"", "").Replace(" ", "");
            var components = cleaned.Split(',');

            double x = 0, y = 0, z = 0;

            foreach (var comp in components)
            {
                var parts = comp.Split(':');
                if (parts.Length == 2)
                {
                    switch (parts[0])
                    {
                        case "X": x = double.Parse(parts[1], CultureInfo.InvariantCulture); break;
                        case "Y": y = double.Parse(parts[1], CultureInfo.InvariantCulture); break;
                        case "Z": z = double.Parse(parts[1], CultureInfo.InvariantCulture); break;
                    }
                }
            }

            bool valid = Math.Abs(x) < 20 && Math.Abs(y) < 20 && Math.Abs(z) < 20;

            return new SensorResponse
            {
                IsValid = valid,
                ProcessedValue = $"\"X:{x:F2},Y:{y:F2},Z:{z:F2}\""
            };
        }
        catch
        {
            return new SensorResponse { IsValid = false, ProcessedValue = "INVALID_VECTOR" };
        }
    }
}
