//using Grpc.Core;
//using HpcService;

//public class HpcForecastService : HpcAnalysisService.HpcAnalysisServiceBase
//{
//    public override Task<HpcForecastResponse> PreverDados(HpcForecastRequest request, ServerCallContext context)
//    {
//        var historico = request.Historico.ToList();
//        var previsoes = new List<double>();

//        if (historico.Count < 2)
//        {
//            return Task.FromResult(new HpcForecastResponse
//            {
//                Previsoes = { historico.FirstOrDefault() },
//                Classificacao = "dados insuficientes",
//                Confianca = 0.0
//            });
//        }

//        // Calcular média de variações
//        var deltas = new List<double>();
//        for (int i = 1; i < historico.Count; i++)
//            deltas.Add(historico[i] - historico[i - 1]);

//        double deltaMedio = deltas.Average();
//        double atual = historico.Last();

//        // Gerar 5 previsões futuras
//        for (int i = 0; i < 5; i++)
//        {
//            atual += deltaMedio;
//            previsoes.Add(Math.Round(atual, 2));
//        }

//        string classificacao = deltaMedio switch
//        {
//            > 0.5 => "subida rápida",
//            > 0.1 => "subida suave",
//            < -0.5 => "queda rápida",
//            < -0.1 => "queda suave",
//            _ => "estável"
//        };

//        return Task.FromResult(new HpcForecastResponse
//        {
//            Previsoes = { previsoes },
//            Classificacao = classificacao,
//            Confianca = 0.85
//        });
//    }

//}


// HpcForecastService.cs com suporte a 4 sensores (inclui vetores)
using Grpc.Core;
using HpcService;

namespace ServicoHPC
{
    public class HpcForecastService : HpcAnalysisService.HpcAnalysisServiceBase
    {
        public override Task<HpcForecastResponse> PreverDados(HpcForecastRequest request, ServerCallContext context)
        {
            string sensor = request.Sensor;
            var historico = request.Historico.ToList();
            var previsoesA = new List<double>();
            var previsoesB = new List<double>();

            if (historico.Count < 2)
            {
                return Task.FromResult(new HpcForecastResponse
                {
                    PrevisoesModeloA = { historico.FirstOrDefault() },
                    PrevisoesModeloB = { historico.FirstOrDefault() },
                    ModeloMaisConfiavel = "indefinido",
                    Classificacao = "dados insuficientes",
                    Explicacao = "Histórico insuficiente para gerar previsão",
                    Confianca = 0.0,
                    PadraoDetectado = "desconhecido"
                });
            }

            // Cálculo dos deltas
            var deltas = new List<double>();
            for (int i = 1; i < historico.Count; i++)
                deltas.Add(historico[i] - historico[i - 1]);

            double deltaMedio = deltas.Average();

            // Modelo A: delta médio
            double atualA = historico.Last();
            for (int i = 0; i < 5; i++)
            {
                atualA += deltaMedio;
                previsoesA.Add(Math.Round(atualA, 2));
            }

            // Modelo B: regressão linear
            double somaX = 0, somaY = 0, somaXY = 0, somaXX = 0;
            for (int i = 0; i < historico.Count; i++)
            {
                somaX += i;
                somaY += historico[i];
                somaXY += i * historico[i];
                somaXX += i * i;
            }
            double n = historico.Count;
            double slope = (n * somaXY - somaX * somaY) / (n * somaXX - somaX * somaX);
            double intercept = (somaY - slope * somaX) / n;

            for (int i = historico.Count; i < historico.Count + 5; i++)
            {
                double y = slope * i + intercept;
                previsoesB.Add(Math.Round(y, 2));
            }

            // Comparar confiança entre modelos
            double confiancaA = Math.Abs(deltaMedio) < 0.5 ? 0.9 : 0.75;
            double confiancaB = Math.Abs(slope) < 0.5 ? 0.8 : 0.7;
            string modeloMaisConfiavel = confiancaA >= confiancaB ? "ModeloA" : "ModeloB";

            // Classificação adaptada ao tipo de sensor
            string classificacao = sensor switch
            {
                "Temperature" or "Hydrophone" => deltaMedio switch
                {
                    > 0.5 => "subida rápida",
                    > 0.1 => "subida suave",
                    < -0.5 => "queda rápida",
                    < -0.1 => "queda suave",
                    _ => "estável"
                },
                "Accelerometer" or "Gyroscope" => deltaMedio switch
                {
                    > 0.5 => "vibração crescente",
                    < -0.5 => "vibração a diminuir",
                    _ => "vibração estável"
                },
                _ => "indefinido"
            };

            string padraoDetectado = DetectarPadrao(historico);
            string explicacao = $"Sensor: {sensor}. Média de variação: {deltaMedio:F2}. Classificado como '{classificacao}' com base nos últimos {historico.Count} pontos.";

            return Task.FromResult(new HpcForecastResponse
            {
                PrevisoesModeloA = { previsoesA },
                PrevisoesModeloB = { previsoesB },
                ModeloMaisConfiavel = modeloMaisConfiavel,
                Classificacao = classificacao,
                Explicacao = explicacao,
                Confianca = Math.Max(confiancaA, confiancaB),
                PadraoDetectado = padraoDetectado
            });
        }

        private string DetectarPadrao(List<double> historico)
        {
            if (historico.Count < 3) return "desconhecido";
            var deltas = new List<double>();
            for (int i = 1; i < historico.Count; i++)
                deltas.Add(historico[i] - historico[i - 1]);

            bool crescente = deltas.All(d => d > 0);
            bool decrescente = deltas.All(d => d < 0);
            bool oscilante = deltas.Any(d => d > 0) && deltas.Any(d => d < 0);

            return crescente ? "crescente" :
                   decrescente ? "decrescente" :
                   oscilante ? "oscilante" : "estável";
        }
    }
}
