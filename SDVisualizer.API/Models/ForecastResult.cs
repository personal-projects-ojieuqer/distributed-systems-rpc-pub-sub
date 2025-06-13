namespace OceanMonitor.API.Models
{
    /// <summary>
    /// Representa o resultado de uma previsão gerada para um dado sensor e instante base.
    /// </summary>
    public class ForecastResult
    {
        /// <summary>
        /// Instante base da previsão (timestamp UTC).
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Número de minutos após o timestamp base para o qual a previsão foi gerada.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Valor previsto pelo Modelo A (média ponderada ou outro algoritmo definido).
        /// </summary>
        public double ModeloA { get; set; }

        /// <summary>
        /// Valor previsto pelo Modelo B (ex: suavização exponencial).
        /// </summary>
        public double ModeloB { get; set; }

        /// <summary>
        /// Identificador do modelo considerado mais fiável para esta previsão.
        /// </summary>
        public string Melhor { get; set; }

        /// <summary>
        /// Classificação qualitativa da tendência ou comportamento previsto (ex: "subida rápida").
        /// </summary>
        public string Classificacao { get; set; }

        /// <summary>
        /// Justificação textual para a previsão e classificação atribuída.
        /// </summary>
        public string Explicacao { get; set; }

        /// <summary>
        /// Grau de confiança (entre 0.0 e 1.0) atribuído à previsão gerada.
        /// </summary>
        public double Confianca { get; set; }
    }
}
