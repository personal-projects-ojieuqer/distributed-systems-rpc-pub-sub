namespace wavies.Wavy
{
    using Wavies.Wavy;

    public static class WavyGenerator
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

            // Se já existir config, preserva cabeçalho e linhas
            if (File.Exists(configPath))
                configLines.AddRange(File.ReadAllLines(configPath));
            else
                configLines.Add("WAVY_ID;status;[data_types];last_sync;aggregator_id");

            // Determinar o próximo número disponível para o WAVY
            int nextId = configLines
                .Skip(1)
                .Select(line => int.Parse(line.Split(';')[0].Replace("WAVY_", "")))
                .DefaultIfEmpty(0)
                .Max() + 1;

            Random rand = new Random();

            for (int i = 0; i < numberOfWavies; i++)
            {
                string wavyId = $"WAVY_{nextId:D3}";
                nextId++;

                string status = "operação";
                string dataTypes = "Accelerometer,Gyroscope,Hydrophone,Temperature";
                string lastSync = DateTime.UtcNow.ToString("o");
                string aggregatorId = $"AGG_{rand.Next(1, 4):D2}";

                string configLine = $"{wavyId};{status};[{dataTypes}];{lastSync};{aggregatorId}";
                configLines.Add(configLine);

                string csvPath = Path.Combine(folderPath, $"{wavyId}.csv");
                File.WriteAllText(csvPath, "Timestamp,SensorType,Value\n");
                Console.WriteLine($"WAVY {wavyId} criado em {csvPath} e associado a {aggregatorId}");

                try
                {
                    string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.Parent!.FullName;
                    string authFolder = Path.Combine(projectRoot, "agregators", "autorizacoes");
                    Directory.CreateDirectory(authFolder);

                    string authFile = Path.Combine(authFolder, $"{aggregatorId}.txt");
                    File.AppendAllLines(authFile, new[] { wavyId });
                    Console.WriteLine($"🔐 {wavyId} autorizado no ficheiro {aggregatorId}.txt");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao escrever no ficheiro de autorizações: {ex.Message}");
                }
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

            var lines = File.ReadAllLines(configPath).Skip(1); // ignora cabeçalho
            foreach (var line in lines)
            {
                var parts = line.Split(';');
                if (parts.Length != 5) continue;

                string wavyId = parts[0];
                string aggregatorId = parts[4];

                var runner = new WavyRunner(wavyId, folder, aggregatorId);
                runner.Start();
                ativos.Add(runner);
                Console.WriteLine($"{wavyId} iniciado (agregador: {aggregatorId})");
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

            // Lê config existente (ou cria cabeçalho)
            var configLines = new List<string>();
            if (File.Exists(configPath))
                configLines.AddRange(File.ReadAllLines(configPath));
            else
                configLines.Add("WAVY_ID;status;[data_types];last_sync;aggregator_id");

            // Descobre o maior número já utilizado
            int nextId = configLines
                .Skip(1)
                .Select(line => int.Parse(line.Split(';')[0].Replace("WAVY_", "")))
                .DefaultIfEmpty(0)
                .Max() + 1;

            var novosWavies = new List<(string wavyId, string aggregatorId)>();

            for (int i = 0; i < count; i++)
            {
                string wavyId = $"WAVY_{nextId:D3}";
                nextId++;

                Console.WriteLine($"\nConfiguração para {wavyId}");
                Console.Write("Escolhe o agregador (AGG_01 / AGG_02 / AGG_03): ");
                string aggregatorId = Console.ReadLine()?.Trim().ToUpper() ?? "AGG_01";

                if (!new[] { "AGG_01", "AGG_02", "AGG_03" }.Contains(aggregatorId))
                {
                    Console.WriteLine("Agregador inválido. A utilizar AGG_01 por defeito.");
                    aggregatorId = "AGG_01";
                }

                string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.Parent!.FullName;
                string authFolder = Path.Combine(projectRoot, "agregators", "autorizacoes");
                Directory.CreateDirectory(authFolder);

                string authFile = Path.Combine(authFolder, $"{aggregatorId}.txt");
                File.AppendAllLines(authFile, new[] { wavyId });
                Console.WriteLine($"{wavyId} autorizado no ficheiro {aggregatorId}.txt");

                string status = "operação";
                string dataTypes = "Accelerometer,Gyroscope,Hydrophone,Temperature";
                string lastSync = DateTime.UtcNow.ToString("o");

                string configLine = $"{wavyId};{status};[{dataTypes}];{lastSync};{aggregatorId}";
                configLines.Add(configLine);

                string csvPath = Path.Combine(folderPath, $"{wavyId}.csv");
                File.WriteAllText(csvPath, "Timestamp,SensorType,Value\n");

                Console.WriteLine($"{wavyId} configurado e associado a {aggregatorId}");
                novosWavies.Add((wavyId, aggregatorId));
            }

            // Salvar todos no final
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

            foreach (var runner in ativos)
            {
                runner.Stop();
            }
            ativos.Clear();
            Console.WriteLine("Todos os WAVIES foram terminados.");

            foreach (var file in files)
            {
                File.Delete(file);
                Console.WriteLine($"Apagado: {Path.GetFileName(file)}");
            }

            string configPath = Path.Combine(folder, "wavy_config.csv");
            List<string> linhasAntigas = new();

            if (File.Exists(configPath))
            {
                linhasAntigas = File.ReadAllLines(configPath).Skip(1).ToList(); // ignora header
                File.Delete(configPath);
                Console.WriteLine("Ficheiro de configuração apagado.");
            }

            // Apagar todos os wavy IDs dos ficheiros de autorização
            string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.Parent!.FullName;
            string authFolder = Path.Combine(projectRoot, "agregators", "autorizacoes");

            if (Directory.Exists(authFolder))
            {
                foreach (var file in Directory.GetFiles(authFolder, "AGG_*.txt"))
                {
                    File.WriteAllText(file, "");
                    Console.WriteLine($"Ficheiro de autorização limpo: {Path.GetFileName(file)}");
                }
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
            {
                Console.WriteLine($"- {lines[i].Split(';')[0]}");
            }

            Console.Write("\nIndica o ID do WAVY a eliminar (ex: WAVY_002): ");
            string target = Console.ReadLine()?.Trim();

            var found = lines.FindIndex(l => l.StartsWith(target + ";"));
            if (found == -1)
            {
                Console.WriteLine($"WAVY {target} não encontrado.");
                return;
            }

            var parts = lines[found].Split(';');
            string aggregatorId = parts[4];

            lines.RemoveAt(found);
            File.WriteAllLines(configPath, lines);

            string csvPath = Path.Combine(folder, $"{target}.csv");
            if (File.Exists(csvPath))
            {
                File.Delete(csvPath);
                Console.WriteLine($"WAVY {target} removido com sucesso.");
            }
            else
            {
                Console.WriteLine($"Ficheiro CSV de {target} não encontrado (pode já ter sido apagado).");
            }

            var runner = ativos.FirstOrDefault(r => r.WavyId == target);
            if (runner != null)
            {
                runner.Stop();
                ativos.Remove(runner);
                Console.WriteLine($"Simulação de {target} terminada.");
            }
            else
            {
                Console.WriteLine($"{target} não está ativo ou já tinha sido removido.");
            }

            WavySecondaryFunctions.RemoverWavyDeAutorizacao(target, aggregatorId);
        }


    }
}

