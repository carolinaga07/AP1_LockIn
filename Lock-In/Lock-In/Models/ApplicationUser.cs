using Microsoft.AspNetCore.Identity;

namespace Lock_In.Models;

public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;
    public Estudiantes? Estudiante { get; set; }

    public ICollection<Notificaciones> Notificaciones { get; set; }
       = new List<Notificaciones>();

    public ICollection<Solicitudes> SolicitudesRevisadas { get; set; }
        = new List<Solicitudes>();

    public ICollection<HistorialMovimientos> MovimientosRegistrados { get; set; }
        = new List<HistorialMovimientos>();
}
