namespace wavies.Wavy
{
    public static class WavyManager
    {
        public static string GetAggregatorFromConfig(string wavyId, string configPath)
        {
            Console.WriteLine($"A procurar agregador para {wavyId} em {configPath}");

            var lines = File.ReadAllLines(configPath).Skip(1); // Ignora o header
            foreach (var line in lines)
            {
                Console.WriteLine($"[CONFIG LINE] {line}");
                var parts = line.Split(';');
                if (parts.Length == 5 && parts[0] == wavyId)
                {
                    return parts[4];
                }
            }

            throw new Exception($"Agregador não encontrado para {wavyId} no config.");
        }

    }
}
