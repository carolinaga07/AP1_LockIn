
using Lock_In.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Core.Entities;

public class DetalleInspecciones
{
    [Key]
    public int DetalleId { get; set; }

    public int InspeccionId { get; set; }

    [ForeignKey(nameof(InspeccionId))]
    [InverseProperty(nameof(Inspecciones.Detalles))]
    public Inspecciones? Inspeccion { get; set; }

    public int CasilleroId { get; set; }

    [ForeignKey(nameof(CasilleroId))]
    public Casilleros? Casillero { get; set; }

    [Required]
    public CondicionCasillero Condicion { get; set; }
        = CondicionCasillero.Bueno;

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public bool RequiereMantenimiento { get; set; }
}
