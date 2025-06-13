namespace HPCVisualizerFE.Models
{
    /// <summary>
    /// Representa uma previsão individual de um sensor,
    /// incluindo valores previstos por dois modelos, classificações e confiança.
    /// </summary>
    public class ForecastData
    {
        /// <summary>
        /// Timestamp da previsão no formato ISO 8601.
        /// </summary>
        public string Timestamp { get; set; }

        /// <summary>
        /// Deslocamento temporal em relação ao último dado real (ex: +1, +2).
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Valor previsto pelo Modelo A.
        /// </summary>
        public double ModeloA { get; set; }

        /// <summary>
        /// Valor previsto pelo Modelo B.
        /// </summary>
        public double ModeloB { get; set; }

        /// <summary>
        /// Identificador do modelo considerado mais fiável para esta previsão.
        /// </summary>
        public string Melhor { get; set; }

        /// <summary>
        /// Classificação qualitativa da previsão (ex: subida rápida, vibração estável).
        /// </summary>
        public string Classificacao { get; set; }

        /// <summary>
        /// Explicação textual gerada pelo sistema de análise.
        /// </summary>
        public string Explicacao { get; set; }

        /// <summary>
        /// Grau de confiança na previsão (0.0 a 1.0).
        /// </summary>
        public double Confianca { get; set; }
    }

    /// <summary>
    /// ViewModel para representar os sensores e dispositivos disponíveis
    /// juntamente com previsões associadas a um WAVY específico.
    /// </summary>
    public class WavyViewModel
    {
        /// <summary>
        /// Lista de identificadores de dispositivos WAVY disponíveis.
        /// </summary>
        public List<string> Wavies { get; set; }

        /// <summary>
        /// Lista de sensores disponíveis para análise.
        /// </summary>
        public List<string> Sensores { get; set; }

        /// <summary>
        /// Identificador do WAVY selecionado atualmente.
        /// </summary>
        public string? SelectedWavy { get; set; }

        /// <summary>
        /// Nome do sensor selecionado atualmente.
        /// </summary>
        public string? SelectedSensor { get; set; }

        /// <summary>
        /// Lista de previsões a apresentar para o WAVY/sensor selecionados.
        /// </summary>
        public List<ForecastData> Forecasts { get; set; }
    }

    /// <summary>
    /// Representa os dados agregados de um dispositivo WAVY,
    /// incluindo valores reais lidos e previsões calculadas.
    /// </summary>
    public class WavySensorData
    {
        /// <summary>
        /// Identificador do dispositivo WAVY.
        /// </summary>
        public string WavyId { get; set; }

        /// <summary>
        /// Lista de valores reais registados pelo sensor.
        /// </summary>
        public List<double> DadosReais { get; set; } = new();

        /// <summary>
        /// Lista de previsões geradas com base nos dados reais.
        /// </summary>
        public List<ForecastData> Previsoes { get; set; } = new();
    }

    /// <summary>
    /// ViewModel utilizado para apresentar sensores disponíveis e os respetivos dados por dispositivo.
    /// </summary>
    public class SensorViewModel
    {
        /// <summary>
        /// Lista dos tipos de sensores disponíveis para seleção.
        /// </summary>
        public List<string> SensoresDisponiveis { get; set; } = new();

        /// <summary>
        /// Sensor atualmente selecionado para visualização.
        /// </summary>
        public string? SensorSelecionado { get; set; }

        /// <summary>
        /// Lista dos dados por cada dispositivo WAVY correspondente ao sensor selecionado.
        /// </summary>
        public List<WavySensorData> DadosPorWavy { get; set; } = new();
    }
}
