using Lock_In.Models;
using Microsoft.AspNetCore.Identity;

namespace Lock_In.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
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

}
