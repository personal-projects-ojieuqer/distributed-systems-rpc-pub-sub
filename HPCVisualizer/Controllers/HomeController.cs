using HPCVisualizerFE.Models;
using Microsoft.AspNetCore.Mvc;

namespace HPCVisualizerFE.Controllers
{
    /// <summary>
    /// Controlador respons�vel por apresentar dados reais e previs�es dos sensores,
    /// obtidos a partir do backend via HTTP.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly HttpClient _http;

        /// <summary>
        /// Construtor que recebe uma inst�ncia de <see cref="IHttpClientFactory"/> para comunica��es com o backend.
        /// </summary>
        public HomeController(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("backend"); // Nomeado no Program.cs ou Startup.cs
        }

        /// <summary>
        /// P�gina principal que mostra dados reais e previs�es dos sensores por WAVY.
        /// </summary>
        /// <param name="sensor">Nome do sensor selecionado (opcional).</param>
        /// <returns>Vista com o modelo preenchido com os dados.</returns>
        public async Task<IActionResult> Index(string? sensor)
        {
            var model = new SensorViewModel
            {
                SensorSelecionado = sensor,
                SensoresDisponiveis = new List<string> { "temperature", "gyroscope", "accelerometer", "hydrophone" },
                DadosPorWavy = new List<WavySensorData>()
            };

            // Se foi fornecido um sensor, tenta obter os dados do backend
            if (!string.IsNullOrEmpty(sensor))
            {
                try
                {
                    var dados = await _http.GetFromJsonAsync<List<WavySensorData>>($"data/{sensor}/wavies");

                    if (dados != null)
                        model.DadosPorWavy = dados;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERRO] Falha ao obter dados do backend: {ex.Message}");
                    // Podes adicionar uma mensagem de erro ao model, se desejado
                }
            }

            // Renderiza a vista com os dados recebidos
            return View("VisualizarPrevisaoEReais", model);
        }

        /// <summary>
        /// P�gina alternativa para visualiza��o de previs�es de sensores.
        /// </summary>
        /// <param name="sensor">Nome do sensor selecionado (opcional).</param>
        /// <returns>Vista espec�fica de previs�es com dados por WAVY.</returns>
        public async Task<IActionResult> VisualizacaoPrevisao(string? sensor)
        {
            var model = new SensorViewModel
            {
                SensorSelecionado = sensor,
                SensoresDisponiveis = new List<string> { "temperature", "gyroscope", "accelerometer", "hydrophone" },
                DadosPorWavy = new List<WavySensorData>()
            };

            // Tenta obter os dados apenas se um sensor foi especificado
            if (!string.IsNullOrEmpty(sensor))
            {
                try
                {
                    var dados = await _http.GetFromJsonAsync<List<WavySensorData>>($"data/{sensor}/wavies");

                    if (dados != null)
                        model.DadosPorWavy = dados;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERRO] Falha ao obter dados do backend: {ex.Message}");
                    // Aqui tamb�m poderias propagar um erro ou exibir aviso na UI
                }
            }

            return View("VisualizacaoPrevisao", model);
        }
    }
}
