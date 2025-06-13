using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;

namespace ServidorRPC
{
    /// <summary>
    /// Classe principal da aplicação gRPC. Responsável por configurar e iniciar o servidor.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Ponto de entrada da aplicação. Constrói e inicia o host web.
        /// </summary>
        /// <param name="args">Argumentos da linha de comandos.</param>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Cria e configura o host web com suporte a gRPC (via HTTP/2 na porta 8080).
        /// </summary>
        /// <param name="args">Argumentos da linha de comandos.</param>
        /// <returns>Instância configurada de <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // Configura o servidor Kestrel para escutar em qualquer IP na porta 8080, com protocolo HTTP/2 (necessário para gRPC)
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.ListenAnyIP(8080, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http2;
                        });
                    });

                    // Define a classe Startup como responsável pela configuração da aplicação
                    webBuilder.UseStartup<Startup>();
                });
    }
}
