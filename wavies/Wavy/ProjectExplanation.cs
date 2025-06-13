namespace Wavies.Wavy
{
    /// <summary>
    /// Fornece uma explicação detalhada e interativa do funcionamento do projeto de Sistemas Distribuídos (TP2).
    /// A explicação inclui a arquitetura do sistema, os objetivos, as tecnologias utilizadas e as funcionalidades disponíveis.
    /// </summary>
    public class ProjectExplanation
    {
        /// <summary>
        /// Apresenta no ecrã uma descrição completa da arquitetura do projeto,
        /// cobrindo todos os seus componentes (WAVIES, Agregadores, Servidor, Serviços gRPC, API e Interface).
        /// Inclui ainda a enumeração das funcionalidades disponíveis no menu principal e os objetivos académicos atingidos.
        /// </summary>
        public static void Explicacao()
        {
            Console.Clear();

            Console.WriteLine("====================== EXPLICAÇÃO DO FUNCIONAMENTO DO PROJETO ======================\n");

            // Introdução e autoria
            Console.WriteLine("Este trabalho foi desenvolvido no âmbito da unidade curricular de Sistemas Distribuídos,");
            Console.WriteLine("representando a segunda fase prática (TP2) da evolução do sistema iniciado no TP1.\n");

            Console.WriteLine("Trabalho realizado por:");
            Console.WriteLine(" - Rui Requeijo (al79138)");
            Console.WriteLine(" - João Mendes (al79229)");
            Console.WriteLine(" - Matilde Coelho (al79908)\n");

            // Arquitetura e componentes principais
            Console.WriteLine("============================= OBJETIVOS E ARQUITETURA ==============================");
            Console.WriteLine("Este projeto simula um sistema distribuído de monitorização oceânica, composto por:");
            Console.WriteLine("- WAVIES: sensores virtuais que geram dados simulados (temperatura, giroscópio, etc);");
            Console.WriteLine("- Agregadores: responsáveis por recolher, processar e armazenar localmente os dados;");
            Console.WriteLine("- Servidor Central: recebe os dados validados, armazena-os e consulta previsões;");
            Console.WriteLine("- Serviços gRPC: dois serviços remotos (Pré-processamento e HPC) para validação e previsão;");
            Console.WriteLine("- API REST e Frontend Web: interface de visualização gráfica das previsões e tendências.\n");

            Console.WriteLine("A comunicação entre os módulos segue diferentes abordagens:");
            Console.WriteLine("- Pub/Sub via RabbitMQ (WAVIES → Agregadores);");
            Console.WriteLine("- gRPC (Agregadores → Pré-processamento, Servidor → HPC);");
            Console.WriteLine("- TCP com encriptação AES+RSA (Agregadores → Servidor);");
            Console.WriteLine("- REST (Frontend → API).\n");

            // Funcionalidades do menu interativo
            Console.WriteLine("============================= FUNCIONALIDADES DISPONÍVEIS =============================");
            Console.WriteLine("1. Iniciar simulação com WAVIES existentes");
            Console.WriteLine("   - Ativa os WAVIES definidos no ficheiro de configuração e reinicia a simulação.");

            Console.WriteLine("2. Adicionar novos WAVIES (manual)");
            Console.WriteLine("   - Permite criar WAVIES e escolher manualmente o agregador associado.");

            Console.WriteLine("3. Gerar WAVIES aleatórios (automático)");
            Console.WriteLine("   - Cria WAVIES de forma automática e associa-os aleatoriamente a agregadores.");

            Console.WriteLine("4. Eliminar todos os WAVIES existentes");
            Console.WriteLine("   - Limpa todos os ficheiros de configuração, CSVs e termina as simulações.");

            Console.WriteLine("5. Eliminar um WAVY específico");
            Console.WriteLine("   - Apaga um único WAVY e remove os dados associados.");

            Console.WriteLine("6. Explicação do funcionamento");
            Console.WriteLine("   - Apresenta esta explicação com todos os detalhes da arquitetura e objetivos.");

            Console.WriteLine("0. Sair");
            Console.WriteLine("   - Encerra o programa.\n");

            // Tecnologias utilizadas
            Console.WriteLine("============================= INFRAESTRUTURA E TECNOLOGIAS =============================");
            Console.WriteLine("- Toda a arquitetura corre em containers Docker (bases de dados, serviços, APIs, etc);");
            Console.WriteLine("- As bases de dados usam MySQL, separadas por componente;");
            Console.WriteLine("- A configuração é feita via variáveis de ambiente no docker-compose;");
            Console.WriteLine("- A segurança da transmissão entre agregador e servidor é garantida com AES-128 + RSA;");
            Console.WriteLine("- O pré-processamento valida os dados recebidos com base em regras por sensor;");
            Console.WriteLine("- O módulo HPC prevê os valores futuros com dois modelos e fornece explicação detalhada;");
            Console.WriteLine("- A interface gráfica é construída com ASP.NET MVC e Chart.js.\n");

            // Resultados académicos
            Console.WriteLine("============================= OBJETIVOS ACADÉMICOS ATINGIDOS =============================");
            Console.WriteLine("- Aplicação de conceitos de comunicação distribuída (TCP, gRPC, REST, Pub/Sub);");
            Console.WriteLine("- Criação de um sistema concorrente com múltiplas threads, tasks e semáforos;");
            Console.WriteLine("- Integração de Docker para simulação de uma arquitetura real de serviços;");
            Console.WriteLine("- Implementação de segurança (tokens, encriptação AES/RSA, autenticação por handshake);");
            Console.WriteLine("- Visualização de dados previsionais com base em análises estatísticas reais.\n");

            Console.WriteLine("Este menu permite testar e controlar o sistema de forma interativa, facilitando a compreensão dos");
            Console.WriteLine("conceitos aplicados e a demonstração das funcionalidades implementadas.");
        }
    }
}
