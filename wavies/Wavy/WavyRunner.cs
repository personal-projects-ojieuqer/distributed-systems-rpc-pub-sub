using System.Globalization;
using System.Text;
using Wavies.Wavy;

/// <summary>
/// Classe responsável por simular sensores de um dispositivo WAVY,
/// registando dados em ficheiro CSV e publicando-os periodicamente via RabbitMQ.
/// </summary>
public class WavyRunner
{
    private readonly string wavyId;
    private readonly string csvPath;
    private readonly string aggregatorId;
    private readonly Mutex fileMutex = new();

    private bool stopRequested = false;

    /// <summary>
    /// Identificador único da instância WAVY.
    /// </summary>
    public string WavyId => wavyId;

    /// <summary>
    /// Construtor que inicializa os caminhos e configurações do WAVY.
    /// </summary>
    /// <param name="wavyId">Identificador do dispositivo WAVY.</param>
    /// <param name="folderPath">Pasta onde o CSV do WAVY está ou será armazenado.</param>
    /// <param name="aggregatorId">Identificador do agregador associado (não utilizado nesta versão).</param>
    public WavyRunner(string wavyId, string folderPath, string aggregatorId)
    {
        this.wavyId = wavyId;
        this.csvPath = Path.Combine(folderPath, $"{wavyId}.csv");
        this.aggregatorId = aggregatorId;
    }

    /// <summary>
    /// Inicia as simulações de sensores e a tarefa de envio de dados.
    /// </summary>
    public void Start()
    {
        // Cada sensor é simulado numa thread independente
        new Thread(TemperatureLoop).Start();
        new Thread(AccelerometerLoop).Start();
        new Thread(GyroscopeLoop).Start();
        new Thread(HydrophoneLoop).Start();

        // Envio dos dados é feito em paralelo através de uma Task
        Task.Run(SenderLoop);
    }

    /// <summary>
    /// Solicita a paragem de todas as tarefas e loops associados.
    /// </summary>
    public void Stop() => stopRequested = true;

    /// <summary>
    /// Simula medições de temperatura com variação diurna e ruído.
    /// </summary>
    private void TemperatureLoop()
    {
        Random rnd = new();
        double baseTemp = rnd.NextDouble() * 15 + 10; // Temperatura base entre 10 e 25 graus
        double amplitude = 5.0;
        DateTime startTime = DateTime.Now;

        while (!stopRequested)
        {
            double timeHours = (DateTime.Now - startTime).TotalHours;
            double diurnal = amplitude * Math.Sin((2 * Math.PI / 24) * timeHours); // Variação ao longo do dia
            double noise = rnd.NextDouble() * 0.5 - 0.25; // Pequeno ruído
            double temp = baseTemp + diurnal + noise;

            string line = $"{DateTime.Now:o},Temperature,{Math.Round(temp, 2).ToString(CultureInfo.InvariantCulture)}";
            WriteToFile(line);

            Thread.Sleep(10000); // Espera 10 segundos
        }
    }

    /// <summary>
    /// Simula leituras de acelerómetro em três eixos com possibilidade de picos.
    /// </summary>
    private void AccelerometerLoop()
    {
        Random rnd = new();
        double x = rnd.NextDouble() * 0.2 - 0.1;
        double y = 9.8 + rnd.NextDouble() * 0.2 - 0.1;
        double z = rnd.NextDouble() * 0.2 - 0.1;

        while (!stopRequested)
        {
            bool spike = rnd.NextDouble() < 0.05; // 5% de probabilidade de pico abrupto

            x += (spike ? rnd.NextDouble() * 2 - 1 : rnd.NextDouble() * 0.1 - 0.05);
            y += (spike ? rnd.NextDouble() * 2 - 1 : rnd.NextDouble() * 0.1 - 0.05);
            z += (spike ? rnd.NextDouble() * 2 - 1 : rnd.NextDouble() * 0.1 - 0.05);

            string value = $"\"X:{x:F2},Y:{y:F2},Z:{z:F2}\"";
            string line = $"{DateTime.Now:o},Accelerometer,{value}";

            WriteToFile(line);
            Thread.Sleep(10000);
        }
    }

    /// <summary>
    /// Simula dados de giroscópio com variações suaves nos três eixos.
    /// </summary>
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

    /// <summary>
    /// Simula sons captados por um hidrofone com tendência e ruído.
    /// </summary>
    private void HydrophoneLoop()
    {
        Random rnd = new();
        double val = rnd.NextDouble() * 20 + 110; // Valor entre 110 e 130 dB
        double trend = 0;

        while (!stopRequested)
        {
            trend += rnd.NextDouble() * 0.2 - 0.1;
            double noise = rnd.NextDouble() * 1.5 - 0.75;

            if (rnd.NextDouble() < 0.03) noise += rnd.NextDouble() * 10; // Pico raro

            val = Math.Clamp(val + trend + noise, 100, 140); // Limita o valor

            string line = $"{DateTime.Now:o},Hydrophone,{Math.Round(val, 1).ToString(CultureInfo.InvariantCulture)}";
            WriteToFile(line);
            Thread.Sleep(30000); // 30 segundos entre medições
        }
    }

    /// <summary>
    /// Lê os dados do ficheiro CSV e publica-os no RabbitMQ.
    /// </summary>
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
            finally
            {
                fileMutex.ReleaseMutex();
            }

            if (allLines.Length <= 1) continue; // Só existe o cabeçalho

            var header = allLines[0];
            var dataLines = allLines.Skip(1).ToList();
            var linesToKeep = new List<string>();

            foreach (var line in dataLines)
            {
                try
                {
                    var parts = SplitCsvLine(line);
                    if (parts.Length != 3)
                    {
                        linesToKeep.Add(line);
                        continue;
                    }

                    string timestamp = parts[0];
                    string sensor = parts[1];
                    string value = parts[2];

                    string message = $"{wavyId};{sensor};{timestamp};{value}";

                    await RabbitPublisher.PublishAsync(wavyId, sensor, message);
                }
                catch (Exception ex)
                {
                    // Mantém a linha para nova tentativa futura
                    linesToKeep.Add(line);
                    Console.WriteLine($"[{wavyId}] Erro ao publicar: {ex.Message}");
                }
            }

            fileMutex.WaitOne();
            try
            {
                File.WriteAllLines(csvPath, new[] { header }.Concat(linesToKeep));
            }
            finally
            {
                fileMutex.ReleaseMutex();
            }
        }

        RabbitPublisher.Close();
    }

    /// <summary>
    /// Escreve uma linha no ficheiro CSV de forma segura.
    /// </summary>
    /// <param name="line">Linha a ser adicionada ao ficheiro.</param>
    private void WriteToFile(string line)
    {
        fileMutex.WaitOne();
        try
        {
            File.AppendAllText(csvPath, line + "\n");
        }
        finally
        {
            fileMutex.ReleaseMutex();
        }
    }

    /// <summary>
    /// Divide uma linha CSV considerando campos com vírgulas dentro de aspas.
    /// </summary>
    /// <param name="line">Linha CSV a ser dividida.</param>
    /// <returns>Array com os campos da linha.</returns>
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
