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

            if (historico.Count < 5)
            {
                return Task.FromResult(new HpcForecastResponse
                {
                    PrevisoesModeloA = { historico.LastOrDefault() },
                    PrevisoesModeloB = { historico.LastOrDefault() },
                    ModeloMaisConfiavel = "indefinido",
                    Classificacao = "dados insuficientes",
                    Explicacao = "Histórico insuficiente para gerar previsão",
                    Confianca = 0.0,
                    PadraoDetectado = "desconhecido"
                });
            }

            // Modelo A: média móvel ponderada
            var pesos = Enumerable.Range(1, historico.Count).Select(i => (double)i).ToList();
            double somaPesos = pesos.Sum();
            double valorBaseA = historico.Zip(pesos, (v, p) => v * p).Sum() / somaPesos;
            double deltaA = (historico.Last() - historico.First()) / historico.Count;

            for (int i = 0; i < 5; i++)
                previsoesA.Add(Math.Round(valorBaseA + deltaA * (i + 1), 2));

            // Modelo B: suavização exponencial
            double alpha = 0.5;
            double smoothed = historico[0];
            foreach (var val in historico.Skip(1))
                smoothed = alpha * val + (1 - alpha) * smoothed;

            for (int i = 0; i < 5; i++)
                previsoesB.Add(Math.Round(smoothed, 2));

            // Confiança baseada na variabilidade
            double desvio = DesvioPadrao(historico);
            double confiancaA = desvio < 0.5 ? 0.9 : 0.6;
            double confiancaB = desvio < 0.8 ? 0.85 : 0.7;
            string modeloMaisConfiavel = confiancaA >= confiancaB ? "ModeloA" : "ModeloB";

            // Classificação
            double deltaMedio = historico.Last() - historico.First();
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
            string explicacao = $"Sensor: {sensor}. Desvio padrão: {desvio:F2}. Classificado como '{classificacao}' com base nos últimos {historico.Count} pontos.";

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

        private static double DesvioPadrao(List<double> dados)
        {
            double media = dados.Average();
            double soma = dados.Sum(v => Math.Pow(v - media, 2));
            return Math.Sqrt(soma / dados.Count);
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
