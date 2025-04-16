namespace wavies.Wavy
{
    using Wavies.Wavy;

    /// <summary>
    /// Classe responsável pela gestão de dispositivos Wavy:
    /// criação, configuração, inicialização, eliminação e associação a agregadores.
    /// </summary>
    public static class WavyManager
    {
        /// <summary>
        /// Devolve o caminho da pasta onde os ficheiros dos WAVIES estão localizados.
        /// </summary>
        /// <returns>Caminho completo da pasta dos WAVIES.</returns>
        public static string GetWaviesFolderPath()
        {
            // Obtém o caminho absoluto para a pasta "Wavy/Data/Wavies" com base na localização do projeto
            string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
            return Path.Combine(projectRoot, "Wavy", "Data", "Wavies");
        }

        /// <summary>
        /// Adiciona um número específico de WAVIES de forma automática e aleatória.
        /// Cada WAVY gerado é associado a um agregador aleatório.
        /// </summary>
        /// <param name="numberOfWavies">Número de WAVIES a gerar.</param>
        public static void AdicionarWaviesAleatorio(int numberOfWavies)
        {
            string folderPath = GetWaviesFolderPath();
            Directory.CreateDirectory(folderPath); // Garante que a pasta existe

            string configPath = Path.Combine(folderPath, "wavy_config.csv");
            var configLines = new List<string>();

            // Lê o ficheiro de configuração ou cria o cabeçalho se não existir
            if (File.Exists(configPath))
                configLines.AddRange(File.ReadAllLines(configPath));
            else
                configLines.Add("WAVY_ID;status;[data_types];last_sync;aggregator_id");

            // Determina o próximo ID disponível
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

                // Dados base do WAVY
                string status = "operação";
                string dataTypes = "Accelerometer,Gyroscope,Hydrophone,Temperature";
                string lastSync = DateTime.UtcNow.ToString("o");
                string aggregatorId = $"AGG_{rand.Next(1, 4):D2}"; // Escolhe aleatoriamente um agregador

                string configLine = $"{wavyId};{status};[{dataTypes}];{lastSync};{aggregatorId}";
                configLines.Add(configLine);

                // Criação do ficheiro CSV inicial
                string csvPath = Path.Combine(folderPath, $"{wavyId}.csv");
                File.WriteAllText(csvPath, "Timestamp,SensorType,Value\n");

                Console.WriteLine($"WAVY {wavyId} criado em {csvPath} e associado a {aggregatorId}");

                try
                {
                    // Autoriza o WAVY no respetivo agregador
                    string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.Parent!.FullName;
                    string authFolder = Path.Combine(projectRoot, "agregators", "autorizacoes");
                    Directory.CreateDirectory(authFolder);

                    string authFile = Path.Combine(authFolder, $"{aggregatorId}.txt");
                    File.AppendAllLines(authFile, new[] { wavyId });

                    Console.WriteLine($"{wavyId} autorizado no ficheiro {aggregatorId}.txt");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao escrever no ficheiro de autorizações: {ex.Message}");
                }
            }

            // Guarda todas as alterações no ficheiro de configuração
            File.WriteAllLines(configPath, configLines);
            Console.WriteLine($"Configuração atualizada em {configPath}");
        }

        // Lista dos WAVIES em execução
        static List<WavyRunner> ativos = new();

        /// <summary>
        /// Inicia todos os WAVIES definidos no ficheiro de configuração.
        /// Para cada WAVY, cria uma instância de WavyRunner e inicia a simulação.
        /// </summary>
        public static void IniciarWaviesExistentes()
        {
            string folder = GetWaviesFolderPath();
            string configPath = Path.Combine(folder, "wavy_config.csv");

            if (!File.Exists(configPath))
            {
                Console.WriteLine("Nenhum WAVY configurado encontrado.");
                return;
            }

            // Lê a configuração e inicia os WAVIES
            var lines = File.ReadAllLines(configPath).Skip(1);
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

            // Captura Ctrl+C e termina os WAVIES de forma segura
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Cancelamento recebido, a terminar WAVIES...");
                foreach (var r in ativos) r.Stop();
                e.Cancel = true;
            };
        }

        /// <summary>
        /// Permite adicionar WAVIES de forma interativa, pedindo ao utilizador o número de dispositivos
        /// e o agregador a que cada um deve estar associado.
        /// </summary>
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
                configLines.Add("WAVY_ID;status;[data_types];last_sync;aggregator_id");

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

                // Autoriza o WAVY
                string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.Parent!.FullName;
                string authFolder = Path.Combine(projectRoot, "agregators", "autorizacoes");
                Directory.CreateDirectory(authFolder);

                string authFile = Path.Combine(authFolder, $"{aggregatorId}.txt");
                File.AppendAllLines(authFile, new[] { wavyId });

                Console.WriteLine($"{wavyId} autorizado no ficheiro {aggregatorId}.txt");

                // Dados do WAVY
                string status = "operação";
                string dataTypes = "Accelerometer,Gyroscope,Hydrophone,Temperature";
                string lastSync = DateTime.UtcNow.ToString("o");

                string configLine = $"{wavyId};{status};[{dataTypes}];{lastSync};{aggregatorId}";
                configLines.Add(configLine);

                // Ficheiro de dados
                string csvPath = Path.Combine(folderPath, $"{wavyId}.csv");
                File.WriteAllText(csvPath, "Timestamp,SensorType,Value\n");

                Console.WriteLine($"{wavyId} configurado e associado a {aggregatorId}");
                novosWavies.Add((wavyId, aggregatorId));
            }

            // Guarda nova configuração
            File.WriteAllLines(configPath, configLines);
        }

        /// <summary>
        /// Elimina todos os WAVIES existentes, os ficheiros CSV, a configuração e limpa os ficheiros de autorização.
        /// </summary>
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

            // Para todos os WAVIES em execução
            foreach (var runner in ativos) runner.Stop();
            ativos.Clear();

            Console.WriteLine("Todos os WAVIES foram terminados.");

            // Apaga ficheiros CSV
            foreach (var file in files)
            {
                File.Delete(file);
                Console.WriteLine($"Apagado: {Path.GetFileName(file)}");
            }

            // Apaga o ficheiro de configuração
            string configPath = Path.Combine(folder, "wavy_config.csv");
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
                Console.WriteLine("Ficheiro de configuração apagado.");
            }

            // Limpa autorizações
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

        /// <summary>
        /// Permite eliminar manualmente um WAVY específico, removendo-o da configuração, do CSV e da lista de autorização.
        /// </summary>
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

            // Recupera o ID do agregador
            var parts = lines[found].Split(';');
            string aggregatorId = parts[4];

            // Remove da configuração
            lines.RemoveAt(found);
            File.WriteAllLines(configPath, lines);

            // Apaga o ficheiro CSV
            string csvPath = Path.Combine(folder, $"{target}.csv");
            if (File.Exists(csvPath))
            {
                File.Delete(csvPath);
                Console.WriteLine($"WAVY {target} removido com sucesso.");
            }

            // Termina simulação se estiver ativa
            var runner = ativos.FirstOrDefault(r => r.WavyId == target);
            if (runner != null)
            {
                runner.Stop();
                ativos.Remove(runner);
                Console.WriteLine($"Simulação de {target} terminada.");
            }

            // Remove do ficheiro de autorizações
            WavySecondaryFunctions.RemoverWavyDeAutorizacao(target, aggregatorId);
        }
    }
}
