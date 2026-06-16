using Atracciones.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using MsFacturacion.Api.Data;
using MsFacturacion.Api.Data.Entities;
using MsFacturacion.Api.Dtos;

namespace MsFacturacion.Api.Services;

public class FacturaService
{
    private readonly FacturacionDbContext _context;

    public FacturaService(FacturacionDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<FacturaResponse>> ListarAsync(CancellationToken cancellationToken)
    {
        var facturas = await _context.Facturas
            .AsNoTracking()
            .Include(x => x.DatosFacturacion)
            .OrderByDescending(x => x.FacFechaEmision)
            .ToListAsync(cancellationToken);

        return facturas.Select(ToResponse).ToList();
    }

    public async Task<FacturaResponse> ObtenerPorGuidAsync(Guid guid, CancellationToken cancellationToken)
    {
        var factura = await _context.Facturas
            .AsNoTracking()
            .Include(x => x.DatosFacturacion)
            .FirstOrDefaultAsync(x => x.FacGuid == guid, cancellationToken);

        if (factura is null)
            throw new NotFoundException("No se encontro la factura.");

        return ToResponse(factura);
    }

    public async Task<FacturaResponse> ObtenerPorReservaGuidAsync(Guid reservaGuid, CancellationToken cancellationToken)
    {
        var factura = await _context.Facturas
            .AsNoTracking()
            .Include(x => x.DatosFacturacion)
            .FirstOrDefaultAsync(x => x.RevGuid == reservaGuid, cancellationToken);

        if (factura is null)
            throw new NotFoundException("No se encontro la factura de la reserva.");

        return ToResponse(factura);
    }

    public async Task<IReadOnlyList<DatosFacturacionResponse>> ListarDatosFacturacionAsync(CancellationToken cancellationToken)
    {
        var rows = await _context.DatosFacturacion
            .AsNoTracking()
            .Include(x => x.Factura)
            .OrderBy(x => x.DfacNombre)
            .ThenBy(x => x.DfacCorreo)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new DatosFacturacionResponse
        {
            Guid = x.DfacGuid,
            FacturaGuid = x.Factura.FacGuid,
            Nombre = x.DfacNombre,
            Apellido = x.DfacApellido,
            Correo = x.DfacCorreo,
            Telefono = x.DfacTelefono
        }).ToList();
    }

    public async Task<FacturaResponse> CrearAsync(CrearFacturaRequest request, CancellationToken cancellationToken)
    {
        ValidateCrear(request);

        var existing = await _context.Facturas
            .AsNoTracking()
            .Include(x => x.DatosFacturacion)
            .FirstOrDefaultAsync(
            x => x.RevGuid == request.ReservaGuid,
            cancellationToken);

        if (existing is not null)
            return ToResponse(existing);

        var factura = new FacturaEntity
        {
            FacGuid = Guid.NewGuid(),
            RevGuid = request.ReservaGuid,
            FacNumero = string.IsNullOrWhiteSpace(request.Numero) ? GenerateNumber() : request.Numero.Trim(),
            FacFechaEmision = DateTimeOffset.UtcNow,
            FacTotal = request.Total,
            FacObservacion = request.Observacion,
            FacOrigenCanal = request.OrigenCanal ?? "BOOKING",
            FacUsuarioIngreso = "api",
            FacIpIngreso = "127.0.0.1",
            FacEstado = "A",
            FacRowVersion = 1,
            DatosFacturacion = new DatosFacturacionEntity
            {
                DfacGuid = Guid.NewGuid(),
                DfacNombre = request.DatosFacturacion.Nombre.Trim(),
                DfacApellido = request.DatosFacturacion.Apellido,
                DfacCorreo = request.DatosFacturacion.Correo.Trim(),
                DfacTelefono = request.DatosFacturacion.Telefono
            }
        };

        try
        {
            await _context.Facturas.AddAsync(factura, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var saved = await _context.Facturas
                .AsNoTracking()
                .Include(x => x.DatosFacturacion)
                .FirstOrDefaultAsync(x => x.RevGuid == request.ReservaGuid, cancellationToken);

            if (saved is not null)
                return ToResponse(saved);

            throw;
        }

        return ToResponse(factura);
    }

    public async Task<bool> InhabilitarAsync(Guid guid, InhabilitarFacturaRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo))
            throw new ValidationException("El motivo de inhabilitacion es obligatorio.");

        var factura = await _context.Facturas.FirstOrDefaultAsync(
            x => x.FacGuid == guid && x.FacEstado == "A",
            cancellationToken);

        if (factura is null)
            throw new NotFoundException("No se encontro la factura activa.");

        factura.FacEstado = "I";
        factura.FacMotivoInhabilitacion = request.Motivo.Trim();
        factura.FacFechaEliminacion = DateTimeOffset.UtcNow;
        factura.FacUsuarioEliminacion = "api";
        factura.FacIpEliminacion = "127.0.0.1";
        factura.FacRowVersion++;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateCrear(CrearFacturaRequest request)
    {
        var errors = new List<string>();

        if (request.ReservaGuid == Guid.Empty)
            errors.Add("El reservaGuid es obligatorio.");

        if (request.Total < 0)
            errors.Add("El total no puede ser negativo.");

        if (request.DatosFacturacion is null)
        {
            errors.Add("Los datos de facturacion son obligatorios.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.DatosFacturacion.Nombre))
                errors.Add("El nombre de facturacion es obligatorio.");

            if (string.IsNullOrWhiteSpace(request.DatosFacturacion.Correo))
                errors.Add("El correo de facturacion es obligatorio.");
        }

        if (errors.Count > 0)
            throw new ValidationException("Error de validacion.", errors);
    }

    private static FacturaResponse ToResponse(FacturaEntity entity)
    {
        return new FacturaResponse
        {
            Guid = entity.FacGuid,
            ReservaGuid = entity.RevGuid,
            Numero = entity.FacNumero,
            FechaEmision = entity.FacFechaEmision,
            Total = entity.FacTotal,
            Observacion = entity.FacObservacion,
            OrigenCanal = entity.FacOrigenCanal,
            Estado = entity.FacEstado,
            DatosFacturacion = entity.DatosFacturacion is null
                ? null
                : new DatosFacturacionResponse
                {
                    Guid = entity.DatosFacturacion.DfacGuid,
                    FacturaGuid = entity.FacGuid,
                    Nombre = entity.DatosFacturacion.DfacNombre,
                    Apellido = entity.DatosFacturacion.DfacApellido,
                    Correo = entity.DatosFacturacion.DfacCorreo,
                    Telefono = entity.DatosFacturacion.DfacTelefono
                }
        };
    }

    private static string GenerateNumber()
    {
        return $"FAC{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(10, 99)}";
    }
}
