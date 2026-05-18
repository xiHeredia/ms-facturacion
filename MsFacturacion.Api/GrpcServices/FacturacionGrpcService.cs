using System.Text.Json;
using Atracciones.Grpc;
using Atracciones.Shared.Models;
using Grpc.Core;
using MsFacturacion.Api.Dtos;
using MsFacturacion.Api.Services;

namespace MsFacturacion.Api.GrpcServices;

public class FacturacionGrpcService : FacturacionGrpc.FacturacionGrpcBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly FacturaService _facturaService;

    public FacturacionGrpcService(FacturaService facturaService)
    {
        _facturaService = facturaService;
    }

    public override async Task<JsonReply> CrearFactura(JsonRequest request, ServerCallContext context)
    {
        var body = JsonSerializer.Deserialize<CrearFacturaRequest>(request.Json, JsonOptions) ?? new CrearFacturaRequest();
        var result = await _facturaService.CrearAsync(body, context.CancellationToken);
        return Ok(ApiResponse<FacturaResponse>.Ok(result, "Factura creada correctamente."));
    }

    public override async Task<JsonReply> ListarFacturas(EmptyRequest request, ServerCallContext context)
    {
        var result = await _facturaService.ListarAsync(context.CancellationToken);
        return Ok(ApiResponse<IReadOnlyList<FacturaResponse>>.Ok(result, "Consulta exitosa."));
    }

    public override async Task<JsonReply> ListarDatosFacturacion(EmptyRequest request, ServerCallContext context)
    {
        var result = await _facturaService.ListarDatosFacturacionAsync(context.CancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DatosFacturacionResponse>>.Ok(result, "Consulta exitosa."));
    }

    private static JsonReply Ok<T>(T value)
    {
        return new JsonReply
        {
            StatusCode = StatusCodes.Status200OK,
            Json = JsonSerializer.Serialize(value, JsonOptions)
        };
    }
}
