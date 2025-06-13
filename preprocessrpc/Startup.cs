using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ServidorRPC
{
    /// <summary>
    /// Classe responsável pela configuração da aplicação gRPC no servidor.
    /// Define os serviços e o pipeline de execução HTTP.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Regista os serviços necessários para a aplicação, nomeadamente o suporte a gRPC.
        /// </summary>
        /// <param name="services">Coleção de serviços para injeção de dependência.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc(); // Ativa o serviço gRPC
        }

        /// <summary>
        /// Configura o pipeline da aplicação, incluindo o mapeamento do serviço gRPC.
        /// </summary>
        /// <param name="app">Construtor de middleware da aplicação.</param>
        /// <param name="env">Informações sobre o ambiente de execução.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Ativa página de erro detalhada durante o desenvolvimento
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            // Ativa sistema de routing (obrigatório para gRPC)
            app.UseRouting();

            // Mapeia o serviço gRPC personalizado
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<PreprocessServiceImpl>(); // Classe que implementa o serviço gRPC
            });
        }
    }
}
