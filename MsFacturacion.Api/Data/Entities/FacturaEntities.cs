namespace MsFacturacion.Api.Data.Entities;

public class FacturaEntity
{
    public int FacId { get; set; }
    public Guid FacGuid { get; set; }
    public Guid RevGuid { get; set; }
    public string FacNumero { get; set; } = null!;
    public DateTimeOffset FacFechaEmision { get; set; }
    public decimal FacTotal { get; set; }
    public string? FacObservacion { get; set; }
    public string? FacOrigenCanal { get; set; }
    public string FacUsuarioIngreso { get; set; } = null!;
    public string FacIpIngreso { get; set; } = null!;
    public DateTimeOffset? FacFechaMod { get; set; }
    public string? FacUsuarioMod { get; set; }
    public string? FacIpMod { get; set; }
    public DateTimeOffset? FacFechaEliminacion { get; set; }
    public string? FacUsuarioEliminacion { get; set; }
    public string? FacIpEliminacion { get; set; }
    public string FacEstado { get; set; } = "A";
    public string? FacMotivoInhabilitacion { get; set; }
    public long FacRowVersion { get; set; } = 1;
    public DatosFacturacionEntity? DatosFacturacion { get; set; }
}

public class DatosFacturacionEntity
{
    public int DfacId { get; set; }
    public Guid DfacGuid { get; set; }
    public int FacId { get; set; }
    public string DfacNombre { get; set; } = null!;
    public string? DfacApellido { get; set; }
    public string DfacCorreo { get; set; } = null!;
    public string? DfacTelefono { get; set; }
    public FacturaEntity Factura { get; set; } = null!;
}
