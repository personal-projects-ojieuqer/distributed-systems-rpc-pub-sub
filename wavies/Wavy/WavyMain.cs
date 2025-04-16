namespace wavies.Wavy
{
    using Wavies.Wavy;

    /// <summary>
    /// Classe principal responsável por apresentar um menu interativo
    /// que permite ao utilizador gerir os dispositivos WAVIES.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Método principal da aplicação.
        /// Apresenta um menu com várias opções para gerir os WAVIES:
        /// iniciar simulações, adicionar, remover e obter explicações sobre o funcionamento.
        /// </summary>
        /// <param name="args">Argumentos de linha de comandos (não utilizados).</param>
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("===== MENU WAVIES =====");
                Console.WriteLine("1. Iniciar simulação com WAVIES existentes");
                Console.WriteLine("2. Adicionar novos WAVIES (Escolha do Agregator de forma MANUAL)");
                Console.WriteLine("3. Gerar WAVIES aleatórios (Escolha do Agregator de forma AUTOMÁTICA)");
                Console.WriteLine("4. Eliminar WAVIES existentes");
                Console.WriteLine("5. Eliminar um WAVY específico");
                Console.WriteLine("6. Explicação do funcionamento do Trabalho");
                Console.WriteLine("0. Sair");
                Console.Write("Escolhe uma opção: ");

                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        // Inicia todos os WAVIES configurados
                        WavyManager.IniciarWaviesExistentes();
                        break;

                    case "2":
                        // Permite adicionar WAVIES manualmente com escolha do agregador
                        WavyManager.AdicionarWavies();
                        break;

                    case "3":
                        // Gera automaticamente WAVIES e associa a agregadores aleatórios
                        Console.Write("Quantos WAVIES aleatórios queres gerar? ");
                        if (int.TryParse(Console.ReadLine(), out int aleatorios) && aleatorios > 0)
                            WavyManager.AdicionarWaviesAleatorio(aleatorios);
                        else
                            Console.WriteLine("Número inválido.");
                        break;

                    case "4":
                        // Elimina todos os WAVIES existentes
                        WavyManager.EliminarWavies();
                        break;

                    case "5":
                        // Elimina apenas um WAVY escolhido pelo utilizador
                        WavyManager.EliminarWavyEspecifico();
                        break;

                    case "6":
                        // Mostra explicação do funcionamento do projeto
                        ProjectExplanation.Explicacao();
                        break;

                    case "0":
                        // Termina o programa
                        Console.WriteLine("A sair...");
                        return;

                    default:
                        // Tratamento de opções inválidas
                        Console.WriteLine("Opção inválida.");
                        break;
                }

                Console.WriteLine("\nPressiona ENTER para voltar ao menu...");
                Console.ReadLine();
            }
        }
    }
}
