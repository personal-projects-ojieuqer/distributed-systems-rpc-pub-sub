namespace HPCVisualizer.Models
{
    /// <summary>
    /// Modelo utilizado para representar informa��es de erro na interface da aplica��o,
    /// incluindo o identificador da requisi��o onde ocorreu a falha.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Identificador �nico da requisi��o que originou o erro.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Indica se o identificador da requisi��o est� presente e deve ser mostrado.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
