using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using ServicoHPC;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(opt =>
{
    opt.ListenAnyIP(5001, listenOpt =>
    {
        listenOpt.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc();
var app = builder.Build();
app.MapGrpcService<HpcForecastService>();
app.Run();
