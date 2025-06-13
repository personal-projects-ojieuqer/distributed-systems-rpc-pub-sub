namespace SDVisualizer.API.Models
{
    public class ForecastData
    {
        public string Timestamp { get; set; }
        public int Offset { get; set; }
        public double ModeloA { get; set; }
        public double ModeloB { get; set; }
        public string Melhor { get; set; }
        public string Classificacao { get; set; }
        public string Explicacao { get; set; }
        public double Confianca { get; set; }
    }

    public class WavySensorData
    {
        public string WavyId { get; set; }
        public List<double> DadosReais { get; set; } = new();
        public List<ForecastData> Previsoes { get; set; } = new();
    }
}
