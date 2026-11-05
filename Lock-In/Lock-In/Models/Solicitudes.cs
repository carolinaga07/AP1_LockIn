using Lock_In.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Models;

public class Solicitudes
{
    [Key]
    public int SolicitudId { get; set; }
    public int EstudianteId { get; set; }
    [ForeignKey(nameof(EstudianteId))]
    public Estudiantes Estudiante { get; set; } = null!;
    public int CasilleroId { get; set; }
    [ForeignKey(nameof(CasilleroId))]
    public Casilleros Casillero { get; set; } = null!;
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;
    public DateTime? FechaRespuestaq { get; set; }
    public string? MotivoRechazo { get; set; }
    public string? RevisadoPorId { get; set; }
    [ForeignKey(nameof(RevisadoPorId))]
    public ApplicationUser? RevisadoPor { get; set; }

}

public enum EstadoSolicitud
{
    Pendiente,
    Aprobada,
    Rechazada,
    Cancelada
}