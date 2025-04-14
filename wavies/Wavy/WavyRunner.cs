using System.Net.Sockets;
using System.Text;

public class WavyRunner
{
    private readonly string wavyId;
    private readonly string csvPath;
    private readonly string aggregatorId;
    private readonly Mutex fileMutex = new();

    private bool stopRequested = false;

    public string WavyId { get; }
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
        new Thread(SenderLoop).Start();
    }

    public void Stop() => stopRequested = true;

    private void TemperatureLoop()
    {
        double temp = 15.0;
        Random rnd = new();

        while (!stopRequested)
        {
            temp += rnd.NextDouble() * 0.3 - 0.15;
            string line = $"{DateTime.UtcNow:o},Temperature,{Math.Round(temp, 2)}";

            WriteToFile(line);
            Thread.Sleep(5000);
        }
    }

    private void AccelerometerLoop()
    {
        double x = 0, y = 9.8, z = 0;
        Random rnd = new();

        while (!stopRequested)
        {
            x += rnd.NextDouble() * 0.1 - 0.05;
            y += rnd.NextDouble() * 0.1 - 0.05;
            z += rnd.NextDouble() * 0.1 - 0.05;

            string value = $"\"X:{x:F2},Y:{y:F2},Z:{z:F2}\"";
            string line = $"{DateTime.UtcNow:o},Accelerometer,{value}";

            WriteToFile(line);
            Thread.Sleep(1000);
        }
    }

    private void GyroscopeLoop()
    {
        double x = 0, y = 0, z = 0;
        Random rnd = new();

        while (!stopRequested)
        {
            x += rnd.NextDouble() * 0.1 - 0.05;
            y += rnd.NextDouble() * 0.1 - 0.05;
            z += rnd.NextDouble() * 0.1 - 0.05;

            string value = $"\"X:{x:F2},Y:{y:F2},Z:{z:F2}\"";
            string line = $"{DateTime.UtcNow:o},Gyroscope,{value}";

            WriteToFile(line);
            Thread.Sleep(1000);
        }
    }

    private void HydrophoneLoop()
    {
        double val = 120;
        Random rnd = new();

        while (!stopRequested)
        {
            val += rnd.NextDouble() * 1.5 - 0.75;
            string line = $"{DateTime.UtcNow:o},Hydrophone,{Math.Round(val, 1)}";

            WriteToFile(line);
            Thread.Sleep(10000);
        }
    }

    private void SenderLoop()
    {
        while (!stopRequested)
        {
            Thread.Sleep(7000);

            string[] lines;
            fileMutex.WaitOne();
            try
            {
                if (!File.Exists(csvPath))
                {
                    Console.WriteLine($"{wavyId}: CSV não encontrado. Aguardando novo ciclo.");
                    Thread.Sleep(3000);
                    continue;
                }

                lines = File.ReadAllLines(csvPath).Skip(1).ToArray();
                File.WriteAllText(csvPath, "Timestamp,SensorType,Value\n"); // reset
            }
            finally { fileMutex.ReleaseMutex(); }

            foreach (var line in lines)
            {
                string message = $"{wavyId}:{line}";
                try
                {
                    using TcpClient client = new();
                    client.Connect("127.0.0.1", GetPort(aggregatorId));
                    using var stream = client.GetStream();
                    byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                    stream.Write(data, 0, data.Length);

                    Console.WriteLine($"[{wavyId}] Enviado para {aggregatorId}: {line}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{wavyId}] Erro ao enviar para {aggregatorId}: {ex.Message}");
                }
            }

            Console.WriteLine($"\n\n[{wavyId}] Envio completo de {lines.Length} linhas para {aggregatorId}.");
        }
    }


    private void WriteToFile(string line)
    {
        fileMutex.WaitOne();
        try { File.AppendAllText(csvPath, line + "\n"); }
        finally { fileMutex.ReleaseMutex(); }
    }

    private int GetPort(string id) => id switch
    {
        "AGG_01" => 5001,
        "AGG_02" => 5002,
        "AGG_03" => 5003,
        _ => throw new Exception("Porta inválida")
    };
}
