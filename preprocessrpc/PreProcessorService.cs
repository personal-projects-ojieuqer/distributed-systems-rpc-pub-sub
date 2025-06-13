using Grpc.Core;
using PreProcessRPC;
using System.Globalization;

/// <summary>
/// Implementação do serviço gRPC para pré-processamento de dados de sensores.
/// Valida, calcula estatísticas e classifica os dados recebidos.
/// </summary>
public class PreprocessServiceImpl : PreprocessService.PreprocessServiceBase
{
    /// <summary>
    /// Método gRPC que processa dados de sensores com base no tipo recebido.
    /// Encaminha o pedido para o método de processamento correspondente.
    /// </summary>
    public override Task<SensorResponse> FilterSensor(SensorRequest request, ServerCallContext context)
    {
        SensorResponse response = request.Sensor switch
        {
            "Temperature" => ProcessTemperature(request),
            "Hydrophone" => ProcessHydrophone(request),
            "Accelerometer" => ProcessAccelerometer(request),
            "Gyroscope" => ProcessGyroscope(request),
            _ => new SensorResponse { IsValid = false }
        };

        // Log simples para monitorização
        Console.WriteLine($"[gRPC] {request.Sensor} ({request.Value}) → Valid: {response.IsValid}");

        return Task.FromResult(response);
    }

    /// <summary>
    /// Processamento específico para sensores de temperatura.
    /// Valida intervalos, calcula média, desvio padrão, tendência e risco.
    /// </summary>
    private SensorResponse ProcessTemperature(SensorRequest request)
    {
        if (!double.TryParse(request.Value.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            return new SensorResponse { IsValid = false };
        }

        // 1. Validação de intervalo plausível
        bool isValid = value >= 5 && value <= 35;

        // 2. Estatísticas básicas
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

        // 3. Outlier com base em 2 desvios padrão
        bool isOutlier = stddev > 0 && Math.Abs(value - mean) > 2 * stddev;

        // 4. Delta entre valor atual e último
        double delta = request.RecentValues.Count > 0 ? value - request.RecentValues.Last() : 0;

        // 5. Análise de tendência simples
        string trend = "unknown";
        if (request.RecentValues.Count >= 1)
        {
            double last = request.RecentValues.Last();
            trend = value > last ? "rising" :
                    value < last ? "falling" : "stable";
        }

        // 6. Classificação de risco
        string riskLevel = "green";
        if (value < 8 || value > 32) riskLevel = "red";
        else if (value < 10 || value > 30) riskLevel = "yellow";

        // 7. Normalização de timestamp
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

        // 8. Considera-se sempre "on schedule"
        bool onSchedule = true;

        return new SensorResponse
        {
            IsValid = isValid,
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

    /// <summary>
    /// Processa leituras de um hidrofone, classificando o nível de ruído.
    /// </summary>
    private SensorResponse ProcessHydrophone(SensorRequest request)
    {
        if (!double.TryParse(request.Value.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            return new SensorResponse { IsValid = false };

        bool isValid = value >= 100 && value <= 140;

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

        bool isOutlier = stddev > 0 && Math.Abs(value - mean) > 2 * stddev;
        double delta = request.RecentValues.Count > 0 ? value - request.RecentValues.Last() : 0;

        string trend = "unknown";
        if (request.RecentValues.Count >= 1)
        {
            double last = request.RecentValues.Last();
            trend = value > last ? "rising" :
                    value < last ? "falling" : "stable";
        }

        string riskLevel = value switch
        {
            >= 130 => "extreme",
            >= 120 => "high",
            >= 110 => "moderate",
            _ => "low"
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

    /// <summary>
    /// Processa dados do acelerómetro, interpretando o vetor XYZ como magnitude total.
    /// </summary>
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
                    string axis = parts[0].ToUpper();
                    string valStr = parts[1].Replace(",", ".");
                    if (double.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                    {
                        if (axis == "X") x = val;
                        else if (axis == "Y") y = val;
                        else if (axis == "Z") z = val;
                    }
                }
            }

            double magnitude = Math.Sqrt(x * x + y * y + z * z);
            bool isValid = magnitude < 20;

            double mean = request.RecentValues.Count > 0 ? request.RecentValues.Average() : magnitude;
            double stddev = request.RecentValues.Count > 1 ?
                Math.Sqrt(request.RecentValues.Sum(v => Math.Pow(v - mean, 2)) / request.RecentValues.Count) : 0;

            bool isOutlier = stddev > 0 && Math.Abs(magnitude - mean) > 2 * stddev;
            double delta = request.RecentValues.Count > 0 ? magnitude - request.RecentValues.Last() : 0;

            string trend = "unknown";
            if (request.RecentValues.Count >= 1)
            {
                double last = request.RecentValues.Last();
                trend = magnitude > last ? "rising" :
                        magnitude < last ? "falling" : "stable";
            }

            string riskLevel = magnitude switch
            {
                < 5 => "low",
                < 10 => "moderate",
                < 20 => "high",
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
            return new SensorResponse { IsValid = false };
        }
    }

    /// <summary>
    /// Processa dados do giroscópio, aplicando lógica semelhante ao acelerómetro.
    /// </summary>
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
                    string axis = parts[0].ToUpper();
                    string valStr = parts[1].Replace(",", ".");
                    if (double.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                    {
                        if (axis == "X") x = val;
                        else if (axis == "Y") y = val;
                        else if (axis == "Z") z = val;
                    }
                }
            }

            double magnitude = Math.Sqrt(x * x + y * y + z * z);
            bool isValid = magnitude <= 5;

            double mean = request.RecentValues.Count > 0 ? request.RecentValues.Average() : magnitude;
            double stddev = request.RecentValues.Count > 1 ?
                Math.Sqrt(request.RecentValues.Sum(v => Math.Pow(v - mean, 2)) / request.RecentValues.Count) : 0;

            bool isOutlier = stddev > 0 && Math.Abs(magnitude - mean) > 2 * stddev;
            double delta = request.RecentValues.Count > 0 ? magnitude - request.RecentValues.Last() : 0;

            string trend = "unknown";
            if (request.RecentValues.Count >= 1)
            {
                double last = request.RecentValues.Last();
                trend = magnitude > last ? "rising" :
                        magnitude < last ? "falling" : "stable";
            }

            string riskLevel = magnitude switch
            {
                < 1 => "stable",
                < 3 => "moderate",
                <= 5 => "unstable",
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
            return new SensorResponse { IsValid = false };
        }
    }
}
