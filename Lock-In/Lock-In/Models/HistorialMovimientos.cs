using Lock_In.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Models;

public class HistorialMovimientos
{
    public int HistorialMovimientoId { get; set; }

    public int AsignacionId { get; set; }
    [ForeignKey(nameof(AsignacionId))]
    public Asignaciones Asignacion { get; set; } = null!;

    public string TipoMovimiento { get; set; } = string.Empty;

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public string? Observacion { get; set; }

    public string? RegistradoPorId { get; set; }
    [ForeignKey(nameof(RegistradoPorId))]
    public ApplicationUser? RegistradoPor { get; set; }
}
