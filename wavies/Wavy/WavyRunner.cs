using System.Net.Sockets;
using System.Text;

/// <summary>
/// Classe responsável por simular o comportamento de um dispositivo Wavy,
/// gerando dados de sensores, guardando-os num ficheiro CSV e enviando-os periodicamente para um agregador.
/// </summary>
public class WavyRunner
{
    private readonly string wavyId;
    private readonly string csvPath;
    private readonly string aggregatorId;
    private readonly Mutex fileMutex = new();

    private bool stopRequested = false;

    /// <summary>
    /// Identificador único do dispositivo Wavy.
    /// </summary>
    public string WavyId { get; }

    /// <summary>
    /// Construtor do WavyRunner.
    /// </summary>
    /// <param name="wavyId">ID do Wavy.</param>
    /// <param name="folderPath">Caminho da pasta onde o ficheiro CSV será guardado.</param>
    /// <param name="aggregatorId">ID do agregador ao qual os dados serão enviados.</param>
    public WavyRunner(string wavyId, string folderPath, string aggregatorId)
    {
        this.wavyId = wavyId;
        this.csvPath = Path.Combine(folderPath, $"{wavyId}.csv");
        this.aggregatorId = aggregatorId;
    }

    /// <summary>
    /// Inicia as threads responsáveis por gerar e enviar os dados dos sensores.
    /// </summary>
    public void Start()
    {
        new Thread(TemperatureLoop).Start();
        new Thread(AccelerometerLoop).Start();
        new Thread(GyroscopeLoop).Start();
        new Thread(HydrophoneLoop).Start();
        new Thread(SenderLoop).Start();
    }

    /// <summary>
    /// Sinaliza para que todas as threads do Wavy parem de executar.
    /// </summary>
    public void Stop() => stopRequested = true;

    /// <summary>
    /// Gera valores simulados de temperatura e escreve-os no ficheiro CSV.
    /// </summary>
    private void TemperatureLoop()
    {
        double temp = 15.0;
        Random rnd = new();

        while (!stopRequested)
        {
            temp += rnd.NextDouble() * 0.3 - 0.15;
            string line = $"{DateTime.Now:o},Temperature,{Math.Round(temp, 2)}";

            WriteToFile(line);
            Thread.Sleep(20000); // 20 segundos
        }
    }

    /// <summary>
    /// Gera valores simulados de aceleração (acelerómetro) e escreve-os no ficheiro CSV.
    /// </summary>
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
            string line = $"{DateTime.Now:o},Accelerometer,{value}";

            WriteToFile(line);
            Thread.Sleep(10000); // 10 segundos
        }
    }

    /// <summary>
    /// Gera valores simulados de rotação (giroscópio) e escreve-os no ficheiro CSV.
    /// </summary>
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
            string line = $"{DateTime.Now:o},Gyroscope,{value}";

            WriteToFile(line);
            Thread.Sleep(10000); // 10 segundos
        }
    }

    /// <summary>
    /// Gera valores simulados de pressão sonora (hidrofone) e escreve-os no ficheiro CSV.
    /// </summary>
    private void HydrophoneLoop()
    {
        double val = 120;
        Random rnd = new();

        while (!stopRequested)
        {
            val += rnd.NextDouble() * 1.5 - 0.75;
            string line = $"{DateTime.Now:o},Hydrophone,{Math.Round(val, 1)}";

            WriteToFile(line);
            Thread.Sleep(30000); // 30 segundos
        }
    }

    /// <summary>
    /// Lê os dados do ficheiro CSV e envia-os para o agregador via TCP de forma periódica.
    /// Após o envio, o ficheiro CSV é limpo e reiniciado com o cabeçalho.
    /// </summary>
    private void SenderLoop()
    {
        while (!stopRequested)
        {
            Thread.Sleep(9000); // Aguarda 9 segundos antes de cada envio

            string[] lines;
            fileMutex.WaitOne();
            try
            {
                if (!File.Exists(csvPath))
                {
                    Console.WriteLine($"{wavyId}: CSV não encontrado. A aguardar novo ciclo.");
                    Thread.Sleep(3000);
                    continue;
                }

                lines = File.ReadAllLines(csvPath).Skip(1).ToArray(); // Ignora o Header
                File.WriteAllText(csvPath, "Timestamp,SensorType,Value\n"); // reinicia o ficheiro
            }
            finally { fileMutex.ReleaseMutex(); }

            foreach (var line in lines)
            {
                try
                {
                    using TcpClient client = new();
                    client.Connect("127.0.0.1", GetPort(aggregatorId));
                    using var stream = client.GetStream();
                    using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                    writer.WriteLine($"START:{wavyId}");

                    foreach (var dataLine in lines)
                    {
                        string message = $"{wavyId}:{dataLine}";
                        writer.WriteLine(message);
                    }

                    writer.WriteLine($"END:{wavyId}");

                    Console.WriteLine($"[{wavyId}] Envio completo para {aggregatorId}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{wavyId}] Erro ao enviar para {aggregatorId}: {ex.Message}");
                }
            }

            Console.WriteLine($"\n\n[{wavyId}] Envio completo de bloco com {lines.Length} linhas para {aggregatorId}.");
        }
    }

    /// <summary>
    /// Escreve uma linha no ficheiro CSV de forma segura (thread-safe).
    /// </summary>
    /// <param name="line">Linha de texto a escrever.</param>
    private void WriteToFile(string line)
    {
        fileMutex.WaitOne();
        try { File.AppendAllText(csvPath, line + "\n"); }
        finally { fileMutex.ReleaseMutex(); }
    }

    /// <summary>
    /// Retorna a porta TCP associada a um determinado agregador.
    /// </summary>
    /// <param name="id">ID do agregador.</param>
    /// <returns>Porta numérica correspondente ao agregador.</returns>
    /// <exception cref="Exception">Lançada se o ID do agregador for desconhecido.</exception>
    private int GetPort(string id) => id switch
    {
        "AGG_01" => 5001,
        "AGG_02" => 5002,
        "AGG_03" => 5003,
        _ => throw new Exception("Porta inválida")
    };
}
