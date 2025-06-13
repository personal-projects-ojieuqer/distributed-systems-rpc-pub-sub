namespace SDVisualizer.API.Models
{
    /// <summary>
    /// Representa uma previsão individual para um sensor num dado instante e offset.
    /// </summary>
    public class ForecastData
    {
        /// <summary>
        /// Timestamp da previsão no formato ISO 8601 (normalmente UTC).
        /// </summary>
        public string Timestamp { get; set; }

        /// <summary>
        /// Número de minutos a partir do timestamp base a que se refere a previsão.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Valor previsto pelo Modelo A (ex: média ponderada).
        /// </summary>
        public double ModeloA { get; set; }

        /// <summary>
        /// Valor previsto pelo Modelo B (ex: suavização exponencial).
        /// </summary>
        public double ModeloB { get; set; }

        /// <summary>
        /// Identificação do modelo considerado mais fiável entre os dois.
        /// </summary>
        public string Melhor { get; set; }

        /// <summary>
        /// Classificação qualitativa da previsão (ex: “subida suave”, “queda rápida”).
        /// </summary>
        public string Classificacao { get; set; }

        /// <summary>
        /// Justificação textual da previsão e classificação associada.
        /// </summary>
        public string Explicacao { get; set; }

        /// <summary>
        /// Grau de confiança na previsão (0.0 a 1.0).
        /// </summary>
        public double Confianca { get; set; }
    }

    /// <summary>
    /// Conjunto de dados reais e previsões associados a um dispositivo Wavy específico.
    /// </summary>
    public class WavySensorData
    {
        /// <summary>
        /// Identificador do dispositivo Wavy.
        /// </summary>
        public string WavyId { get; set; }

        /// <summary>
        /// Lista de valores reais recolhidos do sensor ao longo do tempo.
        /// </summary>
        public List<double> DadosReais { get; set; } = new();

        /// <summary>
        /// Lista de previsões associadas ao sensor deste Wavy.
        /// </summary>
        public List<ForecastData> Previsoes { get; set; } = new();
    }
}
