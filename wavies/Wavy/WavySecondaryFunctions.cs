namespace Wavies.Wavy
{
    public class WavySecondaryFunctions
    {
        public static void RemoverWavyDeAutorizacao(string wavyId, string aggregatorId)
        {
            try
            {
                string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!
                    .Parent!
                    .Parent!
                    .Parent!
                    .Parent!
                    .FullName;
                string authFolder = Path.Combine(projectRoot,
                    "agregators",
                    "autorizacoes");
                string authFile = Path.Combine(authFolder,
                    $"{aggregatorId}.txt");

                if (!File.Exists(authFile)) return;

                var linhas = File.ReadAllLines(authFile)
                    .ToList();
                if (linhas.Remove(wavyId))
                {
                    File.WriteAllLines(authFile, linhas);
                    Console.WriteLine($"{wavyId} removido de {aggregatorId}.txt");
                }
                else
                {
                    Console.WriteLine($"{wavyId} não estava presente em {aggregatorId}.txt");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar autorizações: {ex.Message}");
            }
        }
    }
}
