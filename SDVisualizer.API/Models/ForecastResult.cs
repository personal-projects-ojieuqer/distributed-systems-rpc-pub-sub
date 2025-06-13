namespace OceanMonitor.API.Models
{
    public class ForecastResult
    {
        public DateTime Timestamp { get; set; }
        public int Offset { get; set; }
        public double ModeloA { get; set; }
        public double ModeloB { get; set; }
        public string Melhor { get; set; }
        public string Classificacao { get; set; }
        public string Explicacao { get; set; }
        public double Confianca { get; set; }
    }
}
