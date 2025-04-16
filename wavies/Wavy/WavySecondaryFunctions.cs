namespace Wavies.Wavy
{
    /// <summary>
    /// Classe que contém funções auxiliares relacionadas com a gestão dos dispositivos Wavy,
    /// nomeadamente a manipulação da lista de autorizações.
    /// </summary>
    public class WavySecondaryFunctions
    {
        /// <summary>
        /// Remove o identificador de um dispositivo Wavy da lista de autorizações do agregador especificado.
        /// A lista de autorizações encontra-se num ficheiro de texto dentro da pasta "autorizacoes".
        /// </summary>
        /// <param name="wavyId">Identificador do dispositivo Wavy a ser removido.</param>
        /// <param name="aggregatorId">Identificador do agregador ao qual o Wavy estava autorizado.</param>
        public static void RemoverWavyDeAutorizacao(string wavyId, string aggregatorId)
        {
            try
            {
                // Obtém o caminho absoluto até à pasta do projeto
                string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.Parent!.FullName;

                // Caminho para a pasta e ficheiro de autorizações do agregador
                string authFolder = Path.Combine(projectRoot, "agregators", "autorizacoes");
                string authFile = Path.Combine(authFolder, $"{aggregatorId}.txt");

                // Se o ficheiro de autorizações não existir, não há nada a fazer
                if (!File.Exists(authFile)) return;

                var linhas = File.ReadAllLines(authFile).ToList();

                // Tenta remover o Wavy da lista e atualiza o ficheiro
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
