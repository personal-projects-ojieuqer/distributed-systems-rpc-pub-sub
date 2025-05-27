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
            "Accelerometer" => ProcessAccelerometer(request),
            "Gyroscope" => ProcessGyroscope(request),
            _ => new SensorResponse { IsValid = false, ProcessedValue = $"UNSUPPORTED_SENSOR:{request.Sensor}" }
        };

        // Log opcional
        Console.WriteLine($"[gRPC] {request.Sensor} ({request.Value}) → Valid: {response.IsValid}, Processed: {response.ProcessedValue}");

        return Task.FromResult(response);
    }

    private SensorResponse ProcessTemperature(SensorRequest request)
    {
        if (!double.TryParse(request.Value.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            return new SensorResponse
            {
                IsValid = false,
                ProcessedValue = "INVALID_TEMPERATURE"
            };
        }

        // 1. Valid range check
        bool isValid = value >= 5 && value <= 35;

        // 2. Mean and standard deviation
        double mean = value;
        double stddev = 0;
        if (request.RecentValues.Count > 0)
        {
            mean = request.RecentValues.Average();
            if (request.RecentValues.Count > 1)
            {
                double sumSq = request.RecentValues.Sum(v => Math.Pow(v - mean, 2));
                stddev = Math.Sqrt(sumSq / request.RecentValues.Count);
            }
        }

        // 3. Outlier detection
        bool isOutlier = stddev > 0 && Math.Abs(value - mean) > 2 * stddev;

        // 4. Delta from last
        double delta = request.RecentValues.Count > 0 ? value - request.RecentValues.Last() : 0;

        // 5. Trend analysis
        string trend = "unknown";
        if (request.RecentValues.Count >= 1)
        {
            double last = request.RecentValues.Last();
            if (value > last) trend = "rising";
            else if (value < last) trend = "falling";
            else trend = "stable";
        }

        // 6. Risk level
        string riskLevel = "green";
        if (value < 8 || value > 32) riskLevel = "red";
        else if ((value >= 8 && value < 10) || (value > 30 && value <= 32)) riskLevel = "yellow";

        // 7. Timestamp normalization
        string normalizedTimestamp;
        try
        {
            var localTime = DateTime.Parse(request.Timestamp, null, DateTimeStyles.RoundtripKind);
            normalizedTimestamp = localTime.ToUniversalTime().ToString("o");
        }
        catch
        {
            normalizedTimestamp = "INVALID_TIMESTAMP";
        }

        // 8. Schedule check (simplified as always true)
        bool onSchedule = true;

        return new SensorResponse
        {
            IsValid = isValid,
            ProcessedValue = value.ToString("F2", CultureInfo.InvariantCulture),
            Mean = Math.Round(mean, 2),
            Stddev = Math.Round(stddev, 2),
            IsOutlier = isOutlier,
            DeltaFromLast = Math.Round(delta, 2),
            Trend = trend,
            RiskLevel = riskLevel,
            NormalizedTimestamp = normalizedTimestamp,
            OnSchedule = onSchedule
        };
    }


    private SensorResponse ProcessHydrophone(SensorRequest request)
    {
        if (!double.TryParse(request.Value.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            return new SensorResponse
            {
                IsValid = false,
                ProcessedValue = "INVALID_HYDROPHONE"
            };
        }

        // 1. Validação do intervalo esperado
        bool isValid = value >= 100 && value <= 140;

        // 2. Estatísticas
        double mean = value;
        double stddev = 0;
        if (request.RecentValues.Count > 0)
        {
            mean = request.RecentValues.Average();
            if (request.RecentValues.Count > 1)
            {
                double sumSq = request.RecentValues.Sum(v => Math.Pow(v - mean, 2));
                stddev = Math.Sqrt(sumSq / request.RecentValues.Count);
            }
        }

        // 3. Outlier
        bool isOutlier = stddev > 0 && Math.Abs(value - mean) > 2 * stddev;

        // 4. Delta
        double delta = request.RecentValues.Count > 0 ? value - request.RecentValues.Last() : 0;

        // 5. Tendência
        string trend = "unknown";
        if (request.RecentValues.Count >= 1)
        {
            double last = request.RecentValues.Last();
            if (value > last) trend = "rising";
            else if (value < last) trend = "falling";
            else trend = "stable";
        }

        // 6. Classificação do ruído
        string riskLevel = "low";
        if (value >= 130) riskLevel = "extreme";
        else if (value >= 120) riskLevel = "high";
        else if (value >= 110) riskLevel = "moderate";

        // 7. Timestamp normalizado
        string normalizedTimestamp;
        try
        {
            var localTime = DateTime.Parse(request.Timestamp, null, DateTimeStyles.RoundtripKind);
            normalizedTimestamp = localTime.ToUniversalTime().ToString("o");
        }
        catch
        {
            normalizedTimestamp = "INVALID_TIMESTAMP";
        }

        // 8. Ritmo de leitura
        bool onSchedule = true; // a melhorar com timestamps históricos

        return new SensorResponse
        {
            IsValid = isValid,
            ProcessedValue = value.ToString("F1", CultureInfo.InvariantCulture),
            Mean = Math.Round(mean, 2),
            Stddev = Math.Round(stddev, 2),
            IsOutlier = isOutlier,
            DeltaFromLast = Math.Round(delta, 2),
            Trend = trend,
            RiskLevel = riskLevel,
            NormalizedTimestamp = normalizedTimestamp,
            OnSchedule = onSchedule
        };
    }

    private SensorResponse ProcessAccelerometer(SensorRequest request)
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
                    var axis = parts[0].ToUpper();
                    var valStr = parts[1].Replace(",", ".");
                    if (double.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                    {
                        if (axis == "X") x = val;
                        else if (axis == "Y") y = val;
                        else if (axis == "Z") z = val;
                    }
                }
            }

            double magnitude = Math.Sqrt(x * x + y * y + z * z);

            // Validação: magnitude aceitável
            bool isValid = magnitude < 20;

            // Média e desvio padrão (usando recentValues como magnitude)
            double mean = request.RecentValues.Count > 0 ? request.RecentValues.Average() : magnitude;
            double stddev = request.RecentValues.Count > 1 ?
                Math.Sqrt(request.RecentValues.Sum(v => Math.Pow(v - mean, 2)) / request.RecentValues.Count) : 0;

            bool isOutlier = stddev > 0 && Math.Abs(magnitude - mean) > 2 * stddev;
            double delta = request.RecentValues.Count > 0 ? magnitude - request.RecentValues.Last() : 0;

            string trend = "unknown";
            if (request.RecentValues.Count >= 1)
            {
                double last = request.RecentValues.Last();
                if (magnitude > last) trend = "rising";
                else if (magnitude < last) trend = "falling";
                else trend = "stable";
            }

            string riskLevel = magnitude switch
            {
                < 5 => "low",
                >= 5 and < 10 => "moderate",
                >= 10 and < 20 => "high",
                _ => "extreme"
            };

            string normalizedTimestamp;
            try
            {
                var localTime = DateTime.Parse(request.Timestamp, null, DateTimeStyles.RoundtripKind);
                normalizedTimestamp = localTime.ToUniversalTime().ToString("o");
            }
            catch
            {
                normalizedTimestamp = "INVALID_TIMESTAMP";
            }

            return new SensorResponse
            {
                IsValid = isValid,
                ProcessedValue = magnitude.ToString("F2", CultureInfo.InvariantCulture),
                Mean = Math.Round(mean, 2),
                Stddev = Math.Round(stddev, 2),
                IsOutlier = isOutlier,
                DeltaFromLast = Math.Round(delta, 2),
                Trend = trend,
                RiskLevel = riskLevel,
                NormalizedTimestamp = normalizedTimestamp,
                OnSchedule = true
            };
        }
        catch
        {
            return new SensorResponse
            {
                IsValid = false,
                ProcessedValue = "INVALID_ACCEL_DATA"
            };
        }
    }

    private SensorResponse ProcessGyroscope(SensorRequest request)
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
                    var axis = parts[0].ToUpper();
                    var valStr = parts[1].Replace(",", ".");
                    if (double.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                    {
                        if (axis == "X") x = val;
                        else if (axis == "Y") y = val;
                        else if (axis == "Z") z = val;
                    }
                }
            }

            double magnitude = Math.Sqrt(x * x + y * y + z * z);

            // Validação: rotação normal abaixo de 5 rad/s
            bool isValid = magnitude <= 5;

            // Estatísticas com recentValues
            double mean = request.RecentValues.Count > 0 ? request.RecentValues.Average() : magnitude;
            double stddev = request.RecentValues.Count > 1 ?
                Math.Sqrt(request.RecentValues.Sum(v => Math.Pow(v - mean, 2)) / request.RecentValues.Count) : 0;

            bool isOutlier = stddev > 0 && Math.Abs(magnitude - mean) > 2 * stddev;
            double delta = request.RecentValues.Count > 0 ? magnitude - request.RecentValues.Last() : 0;

            string trend = "unknown";
            if (request.RecentValues.Count >= 1)
            {
                double last = request.RecentValues.Last();
                if (magnitude > last) trend = "rising";
                else if (magnitude < last) trend = "falling";
                else trend = "stable";
            }

            string riskLevel = magnitude switch
            {
                < 1 => "stable",
                >= 1 and < 3 => "moderate",
                >= 3 and <= 5 => "unstable",
                _ => "dangerous"
            };

            string normalizedTimestamp;
            try
            {
                var localTime = DateTime.Parse(request.Timestamp, null, DateTimeStyles.RoundtripKind);
                normalizedTimestamp = localTime.ToUniversalTime().ToString("o");
            }
            catch
            {
                normalizedTimestamp = "INVALID_TIMESTAMP";
            }

            return new SensorResponse
            {
                IsValid = isValid,
                ProcessedValue = magnitude.ToString("F2", CultureInfo.InvariantCulture),
                Mean = Math.Round(mean, 2),
                Stddev = Math.Round(stddev, 2),
                IsOutlier = isOutlier,
                DeltaFromLast = Math.Round(delta, 2),
                Trend = trend,
                RiskLevel = riskLevel,
                NormalizedTimestamp = normalizedTimestamp,
                OnSchedule = true
            };
        }
        catch
        {
            return new SensorResponse
            {
                IsValid = false,
                ProcessedValue = "INVALID_GYRO_DATA"
            };
        }
    }


}
