using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using ServicoHPC;

/// <summary>
/// Ponto de entrada da aplicação gRPC. Configura e inicia o servidor web
/// com suporte a HTTP/2 e mapeia o serviço HpcForecastService.
/// </summary>
var builder = WebApplication.CreateBuilder(args);

// Configura o Kestrel para escutar em qualquer IP na porta 5001 com protocolo HTTP/2 (requisito para gRPC)
builder.WebHost.ConfigureKestrel(opt =>
{
    opt.ListenAnyIP(5001, listenOpt =>
    {
        listenOpt.Protocols = HttpProtocols.Http2;
    });
});

// Regista o serviço gRPC na coleção de serviços da aplicação
builder.Services.AddGrpc();

// Constrói a aplicação web
var app = builder.Build();

// Mapeia o serviço gRPC implementado (HpcForecastService)
app.MapGrpcService<HpcForecastService>();

// Inicia o servidor da aplicação
app.Run();
