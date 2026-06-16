using Atracciones.Shared.Extensions;
using Atracciones.Shared.Messaging;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using MsFacturacion.Api.Data;
using MsFacturacion.Api.GrpcServices;
using MsFacturacion.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(listenOptions =>
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2);
});

builder.Services.AddAtraccionesApiDefaults(builder.Configuration, "ms-facturacion");
builder.Services.AddDbContext<FacturacionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FacturacionDb")));
builder.Services.AddRabbitMqEventBus(builder.Configuration);
builder.Services.AddScoped<FacturaService>();
builder.Services.AddHostedService<ReservasPagadasEventConsumer>();
builder.Services.AddGrpc();

var app = builder.Build();

app.UseAtraccionesApiDefaults();
app.MapGrpcService<FacturacionGrpcService>();

app.Run();
