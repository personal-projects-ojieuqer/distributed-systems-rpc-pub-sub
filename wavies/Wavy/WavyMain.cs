namespace wavies.Wavy
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string folder = WavyGenerator.GetWaviesFolderPath();
            Directory.CreateDirectory(folder);

            Console.WriteLine("Quantos WAVIES queres gerar?");
            if (!int.TryParse(Console.ReadLine(), out int count) || count <= 0)
            {
                Console.WriteLine("Número inválido.");
                return;
            }

            WavyGenerator.GenerateWavies(count);

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("\nCancelamento pedido! A terminar geradores...");
                e.Cancel = true;
                cts.Cancel();
            };

            var tasks = new List<Task>();

            for (int i = 1; i <= count; i++)
            {
                string wavyId = $"WAVY_{i:D3}";
                Console.WriteLine($"A iniciar tarefa para {wavyId}...");
                tasks.Add(WavyGenerator.SimulateMultiSensorData(wavyId, folder, cts.Token));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Erro geral: {ex.Message}");
            }

            Console.WriteLine("Todos os WAVIES terminaram.");

            Console.WriteLine("Pressiona Enter para sair...");
            Console.ReadLine();
        }
    }
}
