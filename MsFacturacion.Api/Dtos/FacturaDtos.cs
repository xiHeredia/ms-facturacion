namespace MsFacturacion.Api.Dtos;

public class CrearFacturaRequest
{
    public Guid ReservaGuid { get; set; }
    public string? Numero { get; set; }
    public decimal Total { get; set; }
    public string? Observacion { get; set; }
    public string? OrigenCanal { get; set; }
    public CrearDatosFacturacionRequest DatosFacturacion { get; set; } = null!;
}

public class CrearDatosFacturacionRequest
{
    public string Nombre { get; set; } = null!;
    public string? Apellido { get; set; }
    public string Correo { get; set; } = null!;
    public string? Telefono { get; set; }
}

public class InhabilitarFacturaRequest
{
    public string Motivo { get; set; } = null!;
}

public class FacturaResponse
{
    public Guid Guid { get; set; }
    public Guid ReservaGuid { get; set; }
    public string Numero { get; set; } = null!;
    public DateTimeOffset FechaEmision { get; set; }
    public decimal Total { get; set; }
    public string? Observacion { get; set; }
    public string? OrigenCanal { get; set; }
    public string Estado { get; set; } = null!;
    public DatosFacturacionResponse? DatosFacturacion { get; set; }
}

public class DatosFacturacionResponse
{
    public Guid Guid { get; set; }
    public Guid FacturaGuid { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Apellido { get; set; }
    public string Correo { get; set; } = null!;
    public string? Telefono { get; set; }
}
