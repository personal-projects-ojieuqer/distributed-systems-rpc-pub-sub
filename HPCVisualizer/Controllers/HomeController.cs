using HPCVisualizerFE.Models;
using Microsoft.AspNetCore.Mvc;

namespace HPCVisualizerFE.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _http;

        public HomeController(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("backend");
        }

        public async Task<IActionResult> Index(string? sensor)
        {
            var model = new SensorViewModel
            {
                SensorSelecionado = sensor,
                SensoresDisponiveis = new List<string> { "temperature", "gyroscope", "accelerometer", "hydrophone" },
                DadosPorWavy = new List<WavySensorData>()
            };

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
                    // podes adicionar uma mensagem de erro ao model se quiseres
                }
            }

            return View(model);
        }


        public async Task<IActionResult> VisualizacaoPrevisao(string? sensor)
        {
            var model = new SensorViewModel
            {
                SensorSelecionado = sensor,
                SensoresDisponiveis = new List<string> { "temperature", "gyroscope", "accelerometer", "hydrophone" },
                DadosPorWavy = new List<WavySensorData>()
            };

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
                }
            }

            return View("VisualizacaoPrevisao", model);

        }

    }
}
