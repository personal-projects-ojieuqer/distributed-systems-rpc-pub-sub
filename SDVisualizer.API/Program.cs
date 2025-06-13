namespace OceanMonitor.API
{
    /// <summary>
    /// Classe de entrada principal para a aplicação API do OceanMonitor.
    /// Responsável pela configuração de serviços, middleware e arranque do servidor web.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Ponto de entrada da aplicação. Configura e arranca o servidor Web.
        /// </summary>
        /// <param name="args">Argumentos de linha de comandos (não utilizados).</param>
        private static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Registo de serviços para a API
            builder.Services.AddControllers(); // Ativa o suporte para controladores MVC
            builder.Services.AddEndpointsApiExplorer(); // Suporte para endpoints via minimal API (caso seja necessário)
            builder.Services.AddSwaggerGen(); // Geração automática da documentação Swagger (OpenAPI)

            var app = builder.Build();

            // Middleware de documentação da API
            app.UseSwagger(); // Gera ficheiro Swagger JSON
            app.UseSwaggerUI(); // Interface gráfica interativa para testar endpoints

            // Middleware de autorização (pode ser expandido com autenticação)
            app.UseAuthorization();

            // Mapeia os controladores atribuídos por rota
            app.MapControllers();

            // Inicia a aplicação
            app.Run();
        }
    }
}
