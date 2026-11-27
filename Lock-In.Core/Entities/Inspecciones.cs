
using Lock_In.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Core.Entities;

public class Inspecciones
{
    [Key]
    public int InspeccionId { get; set; }

    [Required]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [Required]
    public string ResponsableId { get; set; } = string.Empty;

    [ForeignKey(nameof(ResponsableId))]
    public ApplicationUser? Responsable { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    [Required]
    public EstadoInspeccion Estado { get; set; }
        = EstadoInspeccion.Pendiente;

    [InverseProperty(nameof(DetalleInspecciones.Inspeccion))]
    public ICollection<DetalleInspecciones> Detalles { get; set; }
        = new List<DetalleInspecciones>();
}
