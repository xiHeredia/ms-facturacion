using Microsoft.EntityFrameworkCore;
using MsFacturacion.Api.Data.Entities;

namespace MsFacturacion.Api.Data;

public class FacturacionDbContext : DbContext
{
    public FacturacionDbContext(DbContextOptions<FacturacionDbContext> options)
        : base(options)
    {
    }

    public DbSet<FacturaEntity> Facturas => Set<FacturaEntity>();
    public DbSet<DatosFacturacionEntity> DatosFacturacion => Set<DatosFacturacionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FacturaEntity>(builder =>
        {
            builder.ToTable("facturas");
            builder.HasKey(x => x.FacId);
            builder.Property(x => x.FacId).HasColumnName("fac_id").ValueGeneratedOnAdd();
            builder.Property(x => x.FacGuid).HasColumnName("fac_guid").IsRequired();
            builder.Property(x => x.RevGuid).HasColumnName("rev_guid").IsRequired();
            builder.Property(x => x.FacNumero).HasColumnName("fac_numero").HasMaxLength(20).IsRequired();
            builder.Property(x => x.FacFechaEmision).HasColumnName("fac_fecha_emision").IsRequired();
            builder.Property(x => x.FacTotal).HasColumnName("fac_total").HasPrecision(10, 2).IsRequired();
            builder.Property(x => x.FacObservacion).HasColumnName("fac_observacion").HasMaxLength(500);
            builder.Property(x => x.FacOrigenCanal).HasColumnName("fac_origen_canal").HasMaxLength(50);
            builder.Property(x => x.FacUsuarioIngreso).HasColumnName("fac_usuario_ingreso").HasMaxLength(100).IsRequired();
            builder.Property(x => x.FacIpIngreso).HasColumnName("fac_ip_ingreso").HasMaxLength(45).IsRequired();
            builder.Property(x => x.FacFechaMod).HasColumnName("fac_fecha_mod");
            builder.Property(x => x.FacUsuarioMod).HasColumnName("fac_usuario_mod").HasMaxLength(100);
            builder.Property(x => x.FacIpMod).HasColumnName("fac_ip_mod").HasMaxLength(45);
            builder.Property(x => x.FacFechaEliminacion).HasColumnName("fac_fecha_eliminacion");
            builder.Property(x => x.FacUsuarioEliminacion).HasColumnName("fac_usuario_eliminacion").HasMaxLength(100);
            builder.Property(x => x.FacIpEliminacion).HasColumnName("fac_ip_eliminacion").HasMaxLength(45);
            builder.Property(x => x.FacEstado).HasColumnName("fac_estado").HasMaxLength(1).IsRequired();
            builder.Property(x => x.FacMotivoInhabilitacion).HasColumnName("fac_motivo_inhabilitacion").HasMaxLength(300);
            builder.Property(x => x.FacRowVersion).HasColumnName("fac_row_version").IsRequired();
            builder.HasIndex(x => x.FacGuid).IsUnique();
            builder.HasIndex(x => x.RevGuid).IsUnique();
            builder.HasIndex(x => x.FacNumero).IsUnique();
        });

        modelBuilder.Entity<DatosFacturacionEntity>(builder =>
        {
            builder.ToTable("datos_facturacion");
            builder.HasKey(x => x.DfacId);
            builder.Property(x => x.DfacId).HasColumnName("dfac_id").ValueGeneratedOnAdd();
            builder.Property(x => x.DfacGuid).HasColumnName("dfac_guid").IsRequired();
            builder.Property(x => x.FacId).HasColumnName("fac_id").IsRequired();
            builder.Property(x => x.DfacNombre).HasColumnName("dfac_nombre").HasMaxLength(100).IsRequired();
            builder.Property(x => x.DfacApellido).HasColumnName("dfac_apellido").HasMaxLength(100);
            builder.Property(x => x.DfacCorreo).HasColumnName("dfac_correo").HasMaxLength(150).IsRequired();
            builder.Property(x => x.DfacTelefono).HasColumnName("dfac_telefono").HasMaxLength(20);
            builder.HasIndex(x => x.DfacGuid).IsUnique();
            builder.HasIndex(x => x.FacId).IsUnique();
            builder.HasOne(x => x.Factura).WithOne(x => x.DatosFacturacion).HasForeignKey<DatosFacturacionEntity>(x => x.FacId);
        });
    }
}
