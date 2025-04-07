namespace wavies.Wavy
{
    using System.Text;

    public static class WavyGenerator
    {
        public static string GetWaviesFolderPath()
        {
            string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
            return Path.Combine(projectRoot, "Wavy", "Data", "Wavies");
        }

        public static void GenerateWavies(int numberOfWavies)
        {

            string folderPath = GetWaviesFolderPath();
            Directory.CreateDirectory(folderPath);

            var configBuilder = new StringBuilder();
            configBuilder.AppendLine("WAVY_ID;status;[data_types];last_sync;aggregator_id");

            Random rand = new Random();

            for (int i = 1; i <= numberOfWavies; i++)
            {
                string wavyId = $"WAVY_{i:D3}";
                string status = "operação";
                string dataTypes = "Accelerometer,Gyroscope,Hydrophone,Temperature";
                string lastSync = DateTime.UtcNow.ToString("o");

                string aggregatorId = $"AGG_{rand.Next(1, 4):D2}"; // AGG_01, AGG_02 ou AGG_03

                configBuilder.AppendLine($"{wavyId};{status};[{dataTypes}];{lastSync};{aggregatorId}");

                string csvPath = Path.Combine(folderPath, $"{wavyId}.csv");
                File.WriteAllText(csvPath, "Timestamp,SensorType,Value\n");

                Console.WriteLine($"WAVY {wavyId} criado em {csvPath}");
            }

            string configPath = Path.Combine(folderPath, "wavy_config.csv");
            File.WriteAllText(configPath, configBuilder.ToString());
            Console.WriteLine($"Configuração criada em {configPath}");
        }


        static List<WavyRunner> ativos = new();
        public static void AdicionarWavies()
        {
            Console.Write("Quantos WAVIES queres adicionar? ");
            if (!int.TryParse(Console.ReadLine(), out int count) || count <= 0)
            {
                Console.WriteLine("Número inválido.");
                return;
            }

            string folder = GetWaviesFolderPath();
            GenerateWavies(count);

            for (int i = 1; i <= count; i++)
            {
                string wavyId = $"WAVY_{i:D3}";
                string configPath = Path.Combine(folder, "wavy_config.csv");
                string aggregatorId = WavyManager.GetAggregatorFromConfig(wavyId, configPath);

                var runner = new WavyRunner(wavyId, folder, aggregatorId);
                runner.Start();
                ativos.Add(runner);
            }

            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Cancelamento recebido, a terminar WAVIES...");
                foreach (var r in ativos) r.Stop();
                e.Cancel = true;
            };
        }




        public static async Task SimulateMultiSensorData(string wavyId, string folderPath, CancellationToken cancellationToken)
        {
            string csvPath = Path.Combine(folderPath, $"{wavyId}.csv");
            string configPath = Path.Combine(folderPath, "wavy_config.csv");

            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"❗ WAVY {wavyId} não existe.");
                return;
            }

            string aggregatorId;
            try
            {
                aggregatorId = WavyManager.GetAggregatorFromConfig(wavyId, configPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro a obter agregador para {wavyId}: {ex.Message}");
                return;
            }

            Random random = new();
            double temp = 15.0, hydro = 120.0;
            double accX = 0, accY = 9.8, accZ = 0;
            double gyroX = 0, gyroY = 0, gyroZ = 0;

            var writeLock = new SemaphoreSlim(1, 1);
            var tasks = new List<Task>();

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        temp += random.NextDouble() * 0.3 - 0.15;
                        string line = $"{DateTime.UtcNow:o},Temperature,{Math.Round(temp, 2)}\n";
                        await writeLock.WaitAsync(cancellationToken);
                        try { await File.AppendAllTextAsync(csvPath, line); }
                        finally { writeLock.Release(); }
                        await Task.Delay(5000, cancellationToken);
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Temperature [{wavyId}]: {ex.Message}"); }
            }));

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        accX += random.NextDouble() * 0.1 - 0.05;
                        accY += random.NextDouble() * 0.1 - 0.05;
                        accZ += random.NextDouble() * 0.1 - 0.05;
                        string val = $"\"X:{Math.Round(accX, 2)},Y:{Math.Round(accY, 2)},Z:{Math.Round(accZ, 2)}\"";
                        string line = $"{DateTime.UtcNow:o},Accelerometer,{val}\n";
                        await writeLock.WaitAsync(cancellationToken);
                        try { await File.AppendAllTextAsync(csvPath, line); }
                        finally { writeLock.Release(); }
                        await Task.Delay(1000, cancellationToken);
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Accelerometer [{wavyId}]: {ex.Message}"); }
            }));

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        gyroX += random.NextDouble() * 0.1 - 0.05;
                        gyroY += random.NextDouble() * 0.1 - 0.05;
                        gyroZ += random.NextDouble() * 0.1 - 0.05;
                        string val = $"\"X:{Math.Round(gyroX, 2)},Y:{Math.Round(gyroY, 2)},Z:{Math.Round(gyroZ, 2)}\"";
                        string line = $"{DateTime.UtcNow:o},Gyroscope,{val}\n";
                        await writeLock.WaitAsync(cancellationToken);
                        try { await File.AppendAllTextAsync(csvPath, line); }
                        finally { writeLock.Release(); }
                        await Task.Delay(1000, cancellationToken);
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Gyroscope [{wavyId}]: {ex.Message}"); }
            }));

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        hydro += random.NextDouble() * 1.5 - 0.75;
                        string line = $"{DateTime.UtcNow:o},Hydrophone,{Math.Round(hydro, 1)}\n";
                        await writeLock.WaitAsync(cancellationToken);
                        try { await File.AppendAllTextAsync(csvPath, line); }
                        finally { writeLock.Release(); }
                        await Task.Delay(10000, cancellationToken);
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Hydrophone [{wavyId}]: {ex.Message}"); }
            }));

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(7000, cancellationToken);
                        await writeLock.WaitAsync(cancellationToken);
                        try
                        {
                            var success = await WavyComunication.SendToAggregatorAsync(wavyId, csvPath, aggregatorId);
                            if (success) CsvWriter.Clear(csvPath);
                        }
                        finally { writeLock.Release(); }
                    }
                }
                catch (Exception ex) { Console.WriteLine($"SendToAggregator [{wavyId}]: {ex.Message}"); }
            }));

            await Task.WhenAll(tasks);
        }

        public static void EliminarWavies()
        {
            string folder = WavyGenerator.GetWaviesFolderPath();
            if (!Directory.Exists(folder))
            {
                Console.WriteLine("Nenhum WAVY existente encontrado.");
                return;
            }

            var files = Directory.GetFiles(folder, "WAVY_*.csv");
            if (!files.Any())
            {
                Console.WriteLine("Nenhum ficheiro WAVY para apagar.");
                return;
            }

            foreach (var file in files)
            {
                File.Delete(file);
                Console.WriteLine($"Apagado: {Path.GetFileName(file)}");
            }

            string configPath = Path.Combine(folder, "wavy_config.csv");
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
                Console.WriteLine("Ficheiro de configuração apagado.");
            }
        }

    }
}

