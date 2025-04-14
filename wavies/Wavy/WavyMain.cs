namespace wavies.Wavy
{
    using Wavies.Wavy;

    class Program
    {
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
                        WavyGenerator.IniciarWaviesExistentes();
                        break;
                    case "2":
                        WavyGenerator.AdicionarWavies();
                        break;

                    case "3":
                        Console.Write("Quantos WAVIES aleatórios queres gerar? ");
                        if (int.TryParse(Console.ReadLine(), out int aleatorios) && aleatorios > 0)
                            WavyGenerator.AdicionarWaviesAleatorio(aleatorios);
                        else
                            Console.WriteLine("Número inválido.");
                        break;

                    case "4":
                        WavyGenerator.EliminarWavies();
                        break;

                    case "5":
                        WavyGenerator.EliminarWavyEspecifico();
                        break;

                    case "6":
                        ProjectExplanation.Explicacao();
                        break;

                    case "0":
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
    }
}
