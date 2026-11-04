using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Models;

public class Asignaciones
{
    [Key]
    public int AsignacionId { get; set; }
    public int EstudianteId { get; set; }
    [ForeignKey(nameof(EstudianteId))]
    public Estudiantes Estudiante { get; set; } = null!;
    public int CasilleroId { get; set; }
    [ForeignKey(nameof(CasilleroId))]
    public Casilleros Casillero { get; set; } = null!;
    public int SolicitudId { get; set; }
    [ForeignKey(nameof(SolicitudId))]
    public Solicitudes Solicitud { get; set; } = null!;
    public DateTime? FechaEntrega { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public EstadoAsignacion Estado { get; set; }
      = EstadoAsignacion.PendienteEntrega;
    public int LlaveId { get; set; }

    [ForeignKey(nameof(LlaveId))]
    public Llaves? Llave { get; set; }

}
public enum EstadoAsignacion
{
    PendienteEntrega,
    Activa,
    Finalizada
}