namespace Wavies.Wavy
{
    public class ProjectExplanation
    {
        public static void Explicacao()
        {
            Console.Clear();
            Console.WriteLine("========================== EXPLICAÇÃO DO FUNCIONAMENTO DO PROJETO ==========================\n");

            Console.WriteLine("\nEste trabalho foi feito no ambito da Unidade Curricular de Sistemas Distribuidos sendo este o Trabalho Prático 1.\n");
            Console.WriteLine("Foi realizado por:\n");
            Console.WriteLine(" Rui Requeijo - al79138");
            Console.WriteLine(" Joao Mendes - al79229");
            Console.WriteLine(" Matilde Coelho - al79908\n\n");

            Console.WriteLine("======================================= CONTEXTO GERAL =======================================");
            Console.WriteLine("Este projeto simula um sistema distribuído composto por sensores virtuais (designados por WAVIES), agregadores intermediários (AGG_XX) e um servidor central que recolhe e consolida todos os dados.");
            Console.WriteLine("O principal objetivo é aplicar os conceitos abordados na unidade curricular de Sistemas Distribuídos, nomeadamente:");
            Console.WriteLine("- Comunicação por sockets TCP");
            Console.WriteLine("- Execução concorrente com múltiplas threads");
            Console.WriteLine("- Sincronização e controlo de acessos com mutex");
            Console.WriteLine("Para além disso, escolhemos ir mais além e decidimos utilizar tecnologias como docker, mysql para deste modo ter uma melhor infraestrutura e ao mesmo tempo mais realista");


            Console.WriteLine("\n\n================================= COMPONENTES DO SISTEMA =================================:");
            Console.WriteLine("1. WAVIES:");
            Console.WriteLine("- São sensores que simulam o comportamento de dispositivos reais.");
            Console.WriteLine("- Cada WAVY gera periodicamente dados de quatro tipos: Temperatura, Acelerómetro, Giroscópio e Hidrofones.");
            Console.WriteLine("- Os dados são registados localmente num ficheiro CSV.");
            Console.WriteLine("- A cada intervalo definido, os dados acumulados são enviados a um agregador atribuído.");
            Console.WriteLine("- A escrita no ficheiro e o envio de dados são protegidos com mecanismos de sincronização para evitar conflitos.\n");

            Console.WriteLine("2. AGREGADORES:");
            Console.WriteLine("- Recebem dados enviados pelos WAVIES por meio de sockets TCP.");
            Console.WriteLine("- Cada agregador possui uma base de dados MySQL própria para armazenar os dados recebidos.");
            Console.WriteLine("- Os dados recebidos são validados e inseridos na base de dados.");
            Console.WriteLine("- Existe uma verificação de identidade: apenas são aceites dados de WAVIES que foram previamente associados ao agregador.\n");

            Console.WriteLine("3. SERVIDOR CENTRAL:");
            Console.WriteLine("- Efetua a sincronização de dados com os agregadores.");
            Console.WriteLine("- Liga-se periodicamente às bases de dados dos agregadores para recolher apenas os dados novos (com base em timestamps).");
            Console.WriteLine("- Os dados recolhidos são armazenados numa base de dados central (server-db), incluindo a identificação do agregador de origem.");
            Console.WriteLine("- A sincronização é contínua e incremental.\n");

            Console.WriteLine("\n\n===================================== FUNCIONALIDADES =====================================");
            Console.WriteLine("1. Iniciar simulação com WAVIES existentes:");
            Console.WriteLine("   - Esta opção permite retomar uma simulação anterior, utilizando os WAVIES que já foram previamente criados e configurados.");
            Console.WriteLine("   - O sistema lê a configuração guardada (ficheiro wavy_config.csv) e ativa cada WAVY com os respetivos parâmetros e agregador associado.");
            Console.WriteLine("   - Útil para continuar a simulação sem precisar de reconfigurar os sensores.\n");

            Console.WriteLine("2. Adicionar novos WAVIES (Escolha do Agregador de forma MANUAL):");
            Console.WriteLine("   - Permite criar novos WAVIES e escolher manualmente o agregador ao qual cada sensor será associado (AGG_01, AGG_02 ou AGG_03).");
            Console.WriteLine("   - Cada WAVY criado será registado na configuração e começará imediatamente a simular e enviar dados.\n");

            Console.WriteLine("3. Gerar WAVIES aleatórios (Escolha do Agregador de forma AUTOMÁTICA):");
            Console.WriteLine("   - Cria automaticamente um número especificado de WAVIES, associando-os aleatoriamente a um dos agregadores disponíveis.");
            Console.WriteLine("   - Ideal para testes rápidos e simulações em larga escala com configuração mínima por parte do utilizador.\n");

            Console.WriteLine("4. Eliminar WAVIES existentes:");
            Console.WriteLine("   - Apaga todos os WAVIES configurados, incluindo os seus ficheiros CSV e o ficheiro de configuração.");
            Console.WriteLine("   - Termina as simulações ativas e limpa também os registos de autorização dos agregadores.");
            Console.WriteLine("   - Útil para recomeçar uma simulação do zero.\n");

            Console.WriteLine("5. Eliminar um WAVY específico:");
            Console.WriteLine("   - Permite selecionar e apagar apenas um dos WAVIES existentes.");
            Console.WriteLine("   - Remove o ficheiro CSV e a respetiva linha do ficheiro de configuração e do ficheiro de autorização do agregador.");
            Console.WriteLine("   - A simulação correspondente é também terminada.\n");

            Console.WriteLine("6. Explicação do funcionamento do Trabalho:");
            Console.WriteLine("   - Apresenta um resumo completo da arquitetura do sistema, os seus componentes e objetivos do projeto.");
            Console.WriteLine("   - Essencial para compreender como os módulos se interligam e comunicam entre si.\n");

            Console.WriteLine("0. Sair:");
            Console.WriteLine("   - Termina o programa.\n\n");

            Console.WriteLine("\n\n======================================= DOCKER =======================================");
            Console.WriteLine("- Os componentes (agregadores, bases de dados e servidor) são executados em containers Docker.");
            Console.WriteLine("- O ficheiro `docker-compose.yml` define toda a infraestrutura do sistema.");
            Console.WriteLine("- As variáveis de ambiente são utilizadas para configurar dinamicamente cada container.");
            Console.WriteLine("- O sistema é escalável e modular, permitindo a adição de mais WAVIES ou agregadores com pouca alteração no código.\n");

            Console.WriteLine("\n\n============================= OBJETIVOS ACADEMICOS ATINGIDOS =============================");
            Console.WriteLine("- Aplicação prática de conceitos fundamentais de sistemas distribuídos.");
            Console.WriteLine("- Criação de um ecossistema distribuído completo e funcional.");
            Console.WriteLine("- Compreensão e utilização de SOckets, Threads (Multithreads) e Mutex.\n");
            Console.WriteLine("- Experiência com integração de várias tecnologias: C#, TCP/IP, MySQL, Docker.");

            Console.WriteLine("Este menu permite gerir, testar e acompanhar o comportamento do sistema de forma simples e interativa.");
        }
    }
}
