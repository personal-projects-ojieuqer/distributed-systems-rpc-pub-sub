using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using OceanMonitor.API.Models;
using SDVisualizer.API.Models;

namespace OceanMonitor.API.Controllers;

/// <summary>
/// Controlador responsável pela exposição dos dados de sensores (projetados e reais) associados a dispositivos Wavy.
/// </summary>
[ApiController]
[Route("api/data")] // Rota personalizada para alinhamento com o frontend
public class WaviesController : ControllerBase
{
    private readonly IConfiguration _config;

    /// <summary>
    /// Construtor que recebe a configuração da aplicação (para obter connection strings).
    /// </summary>
    public WaviesController(IConfiguration config) => _config = config;

    /// <summary>
    /// Obtém todos os Wavy IDs distintos que têm previsões associadas, independentemente do sensor.
    /// </summary>
    [HttpGet("wavies")]
    public async Task<IActionResult> GetWavies()
    {
        var lista = new List<string>();

        using var conn = new MySqlConnection(_config.GetConnectionString("CentralDB"));
        await conn.OpenAsync();

        string sql = "SELECT DISTINCT wavy_id FROM projection_temperature " +
                     "UNION SELECT wavy_id FROM projection_gyroscope " +
                     "UNION SELECT wavy_id FROM projection_hydrophone " +
                     "UNION SELECT wavy_id FROM projection_accelerometer";

        using var cmd = new MySqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            lista.Add(reader.GetString(0));

        return Ok(lista);
    }

    /// <summary>
    /// Lista todos os sensores com dados disponíveis para um dado dispositivo Wavy.
    /// </summary>
    /// <param name="wavyId">Identificador do dispositivo Wavy.</param>
    [HttpGet("{wavyId}/sensors")]
    public async Task<IActionResult> GetSensors(string wavyId)
    {
        var lista = new List<string>();
        string[] sensores = { "temperature", "hydrophone", "accelerometer", "gyroscope" };

        using var conn = new MySqlConnection(_config.GetConnectionString("CentralDB"));
        await conn.OpenAsync();

        foreach (var sensor in sensores)
        {
            var cmd = new MySqlCommand($"SELECT 1 FROM projection_{sensor} WHERE wavy_id=@w LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@w", wavyId);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null) lista.Add(sensor);
        }

        return Ok(lista);
    }

    /// <summary>
    /// Obtém as últimas previsões geradas para um determinado sensor e Wavy.
    /// </summary>
    /// <param name="wavyId">Identificador do Wavy.</param>
    /// <param name="sensor">Tipo de sensor (temperature, hydrophone, etc).</param>
    [HttpGet("{wavyId}/data")]
    public async Task<IActionResult> GetForecasts(string wavyId, [FromQuery] string sensor)
    {
        var lista = new List<ForecastResult>();

        using var conn = new MySqlConnection(_config.GetConnectionString("CentralDB"));
        await conn.OpenAsync();

        var cmd = new MySqlCommand($@"
            SELECT base_timestamp, minuto_offset, valor_previsto_modeloA,
                   valor_previsto_modeloB, modelo_mais_confiavel,
                   classificacao, explicacao, confianca
            FROM projection_{sensor}
            WHERE wavy_id=@w
            ORDER BY base_timestamp DESC
            LIMIT 10", conn);

        cmd.Parameters.AddWithValue("@w", wavyId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new ForecastResult
            {
                Timestamp = reader.GetDateTime(0),
                Offset = reader.GetInt32(1),
                ModeloA = reader.GetDouble(2),
                ModeloB = reader.GetDouble(3),
                Melhor = reader.GetString(4),
                Classificacao = reader.GetString(5),
                Explicacao = reader.GetString(6),
                Confianca = reader.GetDouble(7)
            });
        }

        return Ok(lista);
    }

    /// <summary>
    /// Obtém os dados reais e as últimas previsões de todos os Wavies para um determinado tipo de sensor.
    /// </summary>
    /// <param name="sensor">Tipo de sensor a consultar (ex: temperature, gyroscope).</param>
    [HttpGet("{sensor}/wavies")]
    public async Task<IActionResult> GetWavySensorData(string sensor)
    {
        var resultado = new List<WavySensorData>();

        using var conn = new MySqlConnection(_config.GetConnectionString("CentralDB"));
        await conn.OpenAsync();

        string rawTable = $"raw_{sensor}";
        string projTable = $"projection_{sensor}";

        var wavies = new List<string>();

        // Identifica os Wavy IDs que possuem dados reais
        using (var cmd = new MySqlCommand($"SELECT DISTINCT wavy_id FROM {rawTable}", conn))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                wavies.Add(reader.GetString(0));
        }

        // Para cada Wavy, obtem os dados reais e as previsões mais recentes
        foreach (var wavyId in wavies)
        {
            var dados = new WavySensorData { WavyId = wavyId };

            // Dados reais ordenados por timestamp
            using (var cmd = new MySqlCommand(
                $"SELECT value FROM {rawTable} WHERE wavy_id=@w ORDER BY timestamp", conn))
            {
                cmd.Parameters.AddWithValue("@w", wavyId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    if (double.TryParse(r["value"].ToString(), out var val))
                        dados.DadosReais.Add(val);
            }

            // Últimas 5 previsões
            using (var cmd = new MySqlCommand(
                $"SELECT base_timestamp, minuto_offset, valor_previsto_modeloA, valor_previsto_modeloB, modelo_mais_confiavel, classificacao, explicacao, confianca " +
                $"FROM {projTable} WHERE wavy_id=@w ORDER BY base_timestamp DESC LIMIT 5", conn))
            {
                cmd.Parameters.AddWithValue("@w", wavyId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    dados.Previsoes.Add(new ForecastData
                    {
                        Timestamp = r.GetDateTime(0).ToString("s"),
                        Offset = r.GetInt32(1),
                        ModeloA = r.GetDouble(2),
                        ModeloB = r.GetDouble(3),
                        Melhor = r.GetString(4),
                        Classificacao = r.GetString(5),
                        Explicacao = r.GetString(6),
                        Confianca = r.GetDouble(7)
                    });
                }
            }

            resultado.Add(dados);
        }

        return Ok(resultado);
    }
}
