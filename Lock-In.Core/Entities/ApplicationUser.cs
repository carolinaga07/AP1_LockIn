
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;

    [InverseProperty(nameof(Estudiantes.ApplicationUser))]
    public Estudiantes? Estudiante { get; set; }

    public ICollection<Notificaciones> Notificaciones { get; set; }
        = new List<Notificaciones>();

    [InverseProperty("RevisadoPor")]
    public ICollection<Solicitudes> SolicitudesRevisadas { get; set; }
     = new List<Solicitudes>();

    [InverseProperty(nameof(HistorialMovimientos.RegistradoPor))]
    public ICollection<HistorialMovimientos> MovimientosRegistrados { get; set; }
        = new List<HistorialMovimientos>();
}
