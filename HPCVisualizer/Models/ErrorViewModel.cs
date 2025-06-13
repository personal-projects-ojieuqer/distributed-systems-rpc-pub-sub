namespace HPCVisualizer.Models
{
    /// <summary>
    /// Modelo utilizado para representar informações de erro na interface da aplicação,
    /// incluindo o identificador da requisição onde ocorreu a falha.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Identificador único da requisição que originou o erro.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Indica se o identificador da requisição está presente e deve ser mostrado.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
