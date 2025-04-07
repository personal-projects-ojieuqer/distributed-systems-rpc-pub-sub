namespace wavies.Wavy
{
    class Program
    {
        //static async Task Main(string[] args)
        //{
        //    string folder = WavyGenerator.GetWaviesFolderPath();
        //    Directory.CreateDirectory(folder);

        //    Console.WriteLine("Quantos WAVIES queres gerar?");
        //    if (!int.TryParse(Console.ReadLine(), out int count) || count <= 0)
        //    {
        //        Console.WriteLine("Número inválido."); 
        //    }

        //    WavyGenerator.GenerateWavies(count);

        //    var cts = new CancellationTokenSource();
        //    Console.CancelKeyPress += (s, e) =>
        //    {
        //        Console.WriteLine("\nCancelamento pedido! A terminar geradores...");
        //        e.Cancel = true;
        //        cts.Cancel();
        //    };

        //    var tasks = new List<Task>();

        //    for (int i = 1; i <= count; i++)
        //    {
        //        string wavyId = $"WAVY_{i:D3}";
        //        Console.WriteLine($"A iniciar tarefa para {wavyId}...");
        //        tasks.Add(WavyGenerator.SimulateMultiSensorData(wavyId, folder, cts.Token));
        //    }

        //    try
        //    {
        //        await Task.WhenAll(tasks);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"💥 Erro geral: {ex.Message}");
        //    }

        //    Console.WriteLine("Todos os WAVIES terminaram.");

        //    Console.WriteLine("Pressiona Enter para sair...");
        //    Console.ReadLine();
        //}


        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("===== MENU WAVIES =====");
                Console.WriteLine("1. Adicionar novos WAVIES");
                Console.WriteLine("2. Eliminar WAVIES existentes");
                Console.WriteLine("3. Explicação do funcionamento");
                Console.WriteLine("4. Sair");
                Console.Write("Escolhe uma opção: ");

                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        WavyGenerator.AdicionarWavies();
                        break;

                    case "2":
                        WavyGenerator.EliminarWavies();
                        break;

                    case "3":
                        Explicacao();
                        break;

                    case "4":
                        Console.WriteLine("A sair...");
                        return;

                    default:
                        Console.WriteLine("Opção inválida.");
                        break;
                }

                Console.WriteLine("\nPressiona ENTER para voltar ao menu...");
                Console.ReadLine();
            }
        }

        static void Explicacao()
        {
            Console.Clear();
            Console.WriteLine("===== EXPLICAÇÃO DO FUNCIONAMENTO =====\n");
            Console.WriteLine("Este trabalho simula um sistema distribuído com sensores (WAVIES) que geram dados.");
            Console.WriteLine("Cada WAVY gera um ficheiro CSV com dados de sensores: Temperatura, Acelerómetro, Giroscópio e Hidrofones.");
            Console.WriteLine("Os WAVIES enviam estes dados para um Agregador (AGG_01, AGG_02, AGG_03), que os armazena numa base de dados MySQL.");
            Console.WriteLine("O objetivo é aplicar conceitos de sistemas distribuídos: comunicação por sockets, paralelismo e persistência de dados.");
            Console.WriteLine("A opção 1 permite-te adicionar mais WAVIES ativos.");
            Console.WriteLine("A opção 2 remove WAVIES existentes (e os seus ficheiros CSV).");
        }
    }
}
