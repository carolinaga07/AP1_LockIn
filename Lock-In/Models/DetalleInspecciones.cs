
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Models;

public class DetalleInspecciones
{
    [Key]
    public int DetalleId { get; set; }

    [Required]
    public int InspeccionId { get; set; }

    [ForeignKey(nameof(InspeccionId))]
    public Inspecciones? Inspeccion { get; set; }

    [Required]
    public int CasilleroId { get; set; }

    [ForeignKey(nameof(CasilleroId))]
    public Casilleros? Casillero { get; set; }

    [Required]
    [MaxLength(30)]
    public string Condicion { get; set; } = "Bueno";

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public bool RequiereMantenimiento { get; set; }
}
