using Atracciones.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MsFacturacion.Api.Dtos;
using MsFacturacion.Api.Services;

namespace MsFacturacion.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/facturas")]
public class FacturasController : ControllerBase
{
    private readonly FacturaService _facturaService;

    public FacturasController(FacturaService facturaService)
    {
        _facturaService = facturaService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var result = await _facturaService.ListarAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<FacturaResponse>>.Ok(result, "Consulta exitosa."));
    }

    [HttpGet("{guid:guid}")]
    public async Task<IActionResult> ObtenerPorGuid(Guid guid, CancellationToken cancellationToken)
    {
        var result = await _facturaService.ObtenerPorGuidAsync(guid, cancellationToken);
        return Ok(ApiResponse<FacturaResponse>.Ok(result, "Consulta exitosa."));
    }

    [HttpGet("reserva/{reservaGuid:guid}")]
    public async Task<IActionResult> ObtenerPorReserva(Guid reservaGuid, CancellationToken cancellationToken)
    {
        var result = await _facturaService.ObtenerPorReservaGuidAsync(reservaGuid, cancellationToken);
        return Ok(ApiResponse<FacturaResponse>.Ok(result, "Consulta exitosa."));
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearFacturaRequest request, CancellationToken cancellationToken)
    {
        var result = await _facturaService.CrearAsync(request, cancellationToken);
        return Ok(ApiResponse<FacturaResponse>.Ok(result, "Factura creada correctamente."));
    }

    [HttpPatch("{guid:guid}/inhabilitar")]
    public async Task<IActionResult> Inhabilitar(Guid guid, [FromBody] InhabilitarFacturaRequest request, CancellationToken cancellationToken)
    {
        var ok = await _facturaService.InhabilitarAsync(guid, request, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(ok, "Factura inhabilitada correctamente."));
    }
}
