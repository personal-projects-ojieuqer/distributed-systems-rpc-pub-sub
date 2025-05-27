namespace wavies.Wavy
{
    public static class WavyManager
    {
        public static string GetWaviesFolderPath()
        {
            string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
            return Path.Combine(projectRoot, "Wavy", "Data", "Wavies");
        }

        public static void AdicionarWaviesAleatorio(int numberOfWavies)
        {
            string folderPath = GetWaviesFolderPath();
            Directory.CreateDirectory(folderPath);

            string configPath = Path.Combine(folderPath, "wavy_config.csv");
            var configLines = new List<string>();

            if (File.Exists(configPath))
                configLines.AddRange(File.ReadAllLines(configPath));
            else
                configLines.Add("WAVY_ID;status;[data_types];last_sync");

            int nextId = configLines
                .Skip(1)
                .Select(line => int.TryParse(line.Split(';')[0].Replace("WAVY_", ""), out var id) ? id : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;


            for (int i = 0; i < numberOfWavies; i++)
            {
                string wavyId = $"WAVY_{nextId:D3}";
                nextId++;

                string status = "operação";
                string dataTypes = "Accelerometer,Gyroscope,Hydrophone,Temperature";
                string lastSync = DateTime.Now.ToString("o");

                string configLine = $"{wavyId};{status};[{dataTypes}];{lastSync}";
                configLines.Add(configLine);

                string csvPath = Path.Combine(folderPath, $"{wavyId}.csv");
                File.WriteAllText(csvPath, "Timestamp,SensorType,Value\n");

                Console.WriteLine($"WAVY {wavyId} criado em {csvPath}");
            }

            File.WriteAllLines(configPath, configLines);
            Console.WriteLine($"Configuração atualizada em {configPath}");
        }

        static List<WavyRunner> ativos = new();

        public static void IniciarWaviesExistentes()
        {
            string folder = GetWaviesFolderPath();
            string configPath = Path.Combine(folder, "wavy_config.csv");

            if (!File.Exists(configPath))
            {
                Console.WriteLine("Nenhum WAVY configurado encontrado.");
                return;
            }

            var lines = File.ReadAllLines(configPath).Skip(1);
            foreach (var line in lines)
            {
                var parts = line.Split(';');
                if (parts.Length != 4) continue;

                string wavyId = parts[0];
                var runner = new WavyRunner(wavyId, folder, "UNUSED");
                runner.Start();
                ativos.Add(runner);

                Console.WriteLine($"{wavyId} iniciado.");
            }

            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Cancelamento recebido, a terminar WAVIES...");
                foreach (var r in ativos) r.Stop();
                e.Cancel = true;
            };
        }

        public static void AdicionarWavies()
        {
            Console.Write("Quantos WAVIES queres adicionar? ");
            if (!int.TryParse(Console.ReadLine(), out int count) || count <= 0)
            {
                Console.WriteLine("Número inválido.");
                return;
            }

            string folderPath = GetWaviesFolderPath();
            Directory.CreateDirectory(folderPath);
            string configPath = Path.Combine(folderPath, "wavy_config.csv");

            var configLines = new List<string>();
            if (File.Exists(configPath))
                configLines.AddRange(File.ReadAllLines(configPath));
            else
                configLines.Add("WAVY_ID;status;[data_types];last_sync");

            int nextId = configLines
                .Skip(1)
                .Select(line => int.Parse(line.Split(';')[0].Replace("WAVY_", "")))
                .DefaultIfEmpty(0)
                .Max() + 1;

            for (int i = 0; i < count; i++)
            {
                string wavyId = $"WAVY_{nextId:D3}";
                nextId++;

                string status = "operação";
                string dataTypes = "Accelerometer,Gyroscope,Hydrophone,Temperature";
                string lastSync = DateTime.UtcNow.ToString("o");

                string configLine = $"{wavyId};{status};[{dataTypes}];{lastSync}";
                configLines.Add(configLine);

                string csvPath = Path.Combine(folderPath, $"{wavyId}.csv");
                File.WriteAllText(csvPath, "Timestamp,SensorType,Value\n");

                Console.WriteLine($"{wavyId} configurado.");
            }

            File.WriteAllLines(configPath, configLines);
        }

        public static void EliminarWavies()
        {
            string folder = GetWaviesFolderPath();
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

            foreach (var runner in ativos) runner.Stop();
            ativos.Clear();

            Console.WriteLine("Todos os WAVIES foram terminados.");

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

        public static void EliminarWavyEspecifico()
        {
            string folder = GetWaviesFolderPath();
            string configPath = Path.Combine(folder, "wavy_config.csv");

            if (!File.Exists(configPath))
            {
                Console.WriteLine("Ficheiro de configuração não encontrado.");
                return;
            }

            var lines = File.ReadAllLines(configPath).ToList();
            if (lines.Count <= 1)
            {
                Console.WriteLine("Nenhum WAVY existente para remover.");
                return;
            }

            Console.WriteLine("WAVIES disponíveis:");
            for (int i = 1; i < lines.Count; i++)
                Console.WriteLine($"- {lines[i].Split(';')[0]}");

            Console.Write("\nIndica o ID do WAVY a eliminar (ex: WAVY_002): ");
            string target = Console.ReadLine()?.Trim();

            var found = lines.FindIndex(l => l.StartsWith(target + ";"));
            if (found == -1)
            {
                Console.WriteLine($"WAVY {target} não encontrado.");
                return;
            }

            lines.RemoveAt(found);
            File.WriteAllLines(configPath, lines);

            string csvPath = Path.Combine(folder, $"{target}.csv");
            if (File.Exists(csvPath))
            {
                File.Delete(csvPath);
                Console.WriteLine($"WAVY {target} removido com sucesso.");
            }

            var runner = ativos.FirstOrDefault(r => r.WavyId == target);
            if (runner != null)
            {
                runner.Stop();
                ativos.Remove(runner);
                Console.WriteLine($"Simulação de {target} terminada.");
            }
        }
    }
}
