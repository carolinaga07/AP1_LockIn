
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Core.Entities;

public class Notificaciones
{
    [Key]
    public int NotificacionId { get; set; }

    public string UsuarioId { get; set; } = string.Empty;

    [ForeignKey(nameof(UsuarioId))]
    [InverseProperty(nameof(ApplicationUser.Notificaciones))]
    public ApplicationUser Usuario { get; set; } = null!;

    public string Titulo { get; set; } = string.Empty;

    public string Mensaje { get; set; } = string.Empty;

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public bool Leida { get; set; } = false;

    public int? SolicitudId { get; set; }

    [ForeignKey(nameof(SolicitudId))]
    public Solicitudes? Solicitud { get; set; }

    public string? Url { get; set; }
}
