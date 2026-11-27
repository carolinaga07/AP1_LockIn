
using Lock_In.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Core.Entities;

public class Solicitudes
{
    [Key]
    public int SolicitudId { get; set; }

    public int EstudianteId { get; set; }

    [ForeignKey(nameof(EstudianteId))]
    [InverseProperty("Solicitudes")]
    public Estudiantes Estudiante { get; set; } = null!;

    public int CasilleroId { get; set; }

    [ForeignKey(nameof(CasilleroId))]
    [InverseProperty(nameof(Casilleros.Solicitudes))]
    public Casilleros Casillero { get; set; } = null!;

    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

    public EstadoSolicitud Estado { get; set; }
        = EstadoSolicitud.Pendiente;

    public DateTime? FechaRespuesta { get; set; }

    public string? MotivoRechazo { get; set; }

    public string? RevisadoPorId { get; set; }

    [ForeignKey(nameof(RevisadoPorId))]
    [InverseProperty(nameof(ApplicationUser.SolicitudesRevisadas))]
    public ApplicationUser? RevisadoPor { get; set; }

    public Asignaciones? Asignaciones { get; set; }
}
