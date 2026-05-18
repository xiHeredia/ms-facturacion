using Atracciones.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MsFacturacion.Api.Dtos;
using MsFacturacion.Api.Services;

namespace MsFacturacion.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/datos-facturacion")]
public class DatosFacturacionController : ControllerBase
{
    private readonly FacturaService _facturaService;

    public DatosFacturacionController(FacturaService facturaService)
    {
        _facturaService = facturaService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var result = await _facturaService.ListarDatosFacturacionAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DatosFacturacionResponse>>.Ok(result, "Consulta exitosa."));
    }
}
