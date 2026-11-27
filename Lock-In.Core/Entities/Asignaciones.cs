
using Lock_In.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Core.Entities;

public class Asignaciones
{
    [Key]
    public int AsignacionId { get; set; }

    public int EstudianteId { get; set; }

    [ForeignKey(nameof(EstudianteId))]
    [InverseProperty(nameof(Estudiantes.Asignaciones))]
    public Estudiantes Estudiante { get; set; } = null!;

    public int CasilleroId { get; set; }

    [ForeignKey(nameof(CasilleroId))]
    [InverseProperty(nameof(Casilleros.Asignaciones))]
    public Casilleros Casillero { get; set; } = null!;

    public int SolicitudId { get; set; }

    [ForeignKey(nameof(SolicitudId))]
    [InverseProperty(nameof(Solicitudes.Asignaciones))]
    public Solicitudes Solicitud { get; set; } = null!;

    public DateTime? FechaEntrega { get; set; }

    public DateTime? FechaDevolucion { get; set; }

    public EstadoAsignacion Estado { get; set; }
        = EstadoAsignacion.PendienteEntrega;

    public int LlaveId { get; set; }

    [ForeignKey(nameof(LlaveId))]
    [InverseProperty(nameof(Llaves.Asignaciones))]
    public Llaves? Llave { get; set; }
}
