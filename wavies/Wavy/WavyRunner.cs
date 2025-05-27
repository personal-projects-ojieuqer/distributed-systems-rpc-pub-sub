using System.Globalization;
using System.Text;
using Wavies.Wavy;

public class WavyRunner
{
    private readonly string wavyId;
    private readonly string csvPath;
    private readonly string aggregatorId;
    private readonly Mutex fileMutex = new();

    private bool stopRequested = false;
    public string WavyId => wavyId;


    public WavyRunner(string wavyId, string folderPath, string aggregatorId)
    {
        this.wavyId = wavyId;
        this.csvPath = Path.Combine(folderPath, $"{wavyId}.csv");
        this.aggregatorId = aggregatorId;
    }

    public void Start()
    {
        new Thread(TemperatureLoop).Start();
        new Thread(AccelerometerLoop).Start();
        new Thread(GyroscopeLoop).Start();
        new Thread(HydrophoneLoop).Start();
        Task.Run(SenderLoop);
    }

    public void Stop() => stopRequested = true;

    private void TemperatureLoop()
    {
        Random rnd = new();
        double baseTemp = rnd.NextDouble() * 15 + 10; // [10, 25]
        double amplitude = 5.0;
        DateTime startTime = DateTime.Now;

        while (!stopRequested)
        {
            double timeHours = (DateTime.Now - startTime).TotalHours;
            double diurnal = amplitude * Math.Sin((2 * Math.PI / 24) * timeHours);
            double noise = rnd.NextDouble() * 0.5 - 0.25;
            double temp = baseTemp + diurnal + noise;

            string line = $"{DateTime.Now:o},Temperature,{Math.Round(temp, 2).ToString(CultureInfo.InvariantCulture)}";
            WriteToFile(line);
            Thread.Sleep(10000);
        }
    }


    private void AccelerometerLoop()
    {
        Random rnd = new();
        double x = rnd.NextDouble() * 0.2 - 0.1;
        double y = 9.8 + rnd.NextDouble() * 0.2 - 0.1;
        double z = rnd.NextDouble() * 0.2 - 0.1;

        while (!stopRequested)
        {
            bool spike = rnd.NextDouble() < 0.05;

            x += (spike ? rnd.NextDouble() * 2 - 1 : rnd.NextDouble() * 0.1 - 0.05);
            y += (spike ? rnd.NextDouble() * 2 - 1 : rnd.NextDouble() * 0.1 - 0.05);
            z += (spike ? rnd.NextDouble() * 2 - 1 : rnd.NextDouble() * 0.1 - 0.05);

            string value = $"\"X:{x:F2},Y:{y:F2},Z:{z:F2}\"";
            string line = $"{DateTime.Now:o},Accelerometer,{value}";

            WriteToFile(line);
            Thread.Sleep(10000);
        }
    }
    private void GyroscopeLoop()
    {
        Random rnd = new();
        double x = rnd.NextDouble() * 0.2 - 0.1;
        double y = rnd.NextDouble() * 0.2 - 0.1;
        double z = rnd.NextDouble() * 0.2 - 0.1;

        while (!stopRequested)
        {
            x += rnd.NextDouble() * 0.1 - 0.05;
            y += rnd.NextDouble() * 0.1 - 0.05;
            z += rnd.NextDouble() * 0.1 - 0.05;

            string value = $"\"X:{x:F2},Y:{y:F2},Z:{z:F2}\"";
            string line = $"{DateTime.Now:o},Gyroscope,{value}";

            WriteToFile(line);
            Thread.Sleep(10000);
        }
    }

    private void HydrophoneLoop()
    {
        Random rnd = new();
        double val = rnd.NextDouble() * 20 + 110; // [110,130]
        double trend = 0;

        while (!stopRequested)
        {
            trend += rnd.NextDouble() * 0.2 - 0.1;
            double noise = rnd.NextDouble() * 1.5 - 0.75;
            if (rnd.NextDouble() < 0.03) noise += rnd.NextDouble() * 10;

            val = Math.Clamp(val + trend + noise, 100, 140);

            string line = $"{DateTime.Now:o},Hydrophone,{Math.Round(val, 1).ToString(CultureInfo.InvariantCulture)}";
            WriteToFile(line);
            Thread.Sleep(30000);
        }
    }


    private async Task SenderLoop()
    {
        RabbitPublisher.Initialize();

        while (!stopRequested)
        {
            await Task.Delay(9000);

            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"{wavyId}: CSV não encontrado.");
                continue;
            }

            string[] allLines;
            fileMutex.WaitOne();
            try
            {
                allLines = File.ReadAllLines(csvPath);
            }
            finally { fileMutex.ReleaseMutex(); }

            if (allLines.Length <= 1) continue; // Só header

            var header = allLines[0];
            var dataLines = allLines.Skip(1).ToList();
            var linesToKeep = new List<string>();

            foreach (var line in dataLines)
            {
                try
                {
                    var parts = SplitCsvLine(line);
                    if (parts.Length != 3) { linesToKeep.Add(line); continue; }

                    string timestamp = parts[0];
                    string sensor = parts[1];
                    string value = parts[2];

                    string message = $"{wavyId};{sensor};{timestamp};{value}";

                    await RabbitPublisher.PublishAsync(wavyId, sensor, message);
                }
                catch (Exception ex)
                {
                    linesToKeep.Add(line); // Mantém se falhar
                    Console.WriteLine($"[{wavyId}] Erro ao publicar: {ex.Message}");
                }
            }

            fileMutex.WaitOne();
            try
            {
                File.WriteAllLines(csvPath, new[] { header }.Concat(linesToKeep));
            }
            finally { fileMutex.ReleaseMutex(); }
        }

        RabbitPublisher.Close();
    }




    private void WriteToFile(string line)
    {
        fileMutex.WaitOne();
        try { File.AppendAllText(csvPath, line + "\n"); }
        finally { fileMutex.ReleaseMutex(); }
    }

    private string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool insideQuotes = false;

        foreach (char c in line)
        {
            if (c == '"' && !insideQuotes)
            {
                insideQuotes = true;
            }
            else if (c == '"' && insideQuotes)
            {
                insideQuotes = false;
            }
            else if (c == ',' && !insideQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }

}
