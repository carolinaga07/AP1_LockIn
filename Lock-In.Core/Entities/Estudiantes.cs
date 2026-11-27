
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Core.Entities;

public class Estudiantes
{
    [Key]
    public int EstudianteId { get; set; }

    [Required(ErrorMessage = "La matrícula es obligatoria")]
    public string Matricula { get; set; } = string.Empty;

    [Required(ErrorMessage = "La carrera es obligatoria")]
    public string Carrera { get; set; } = string.Empty;

    public string ApplicationUserId { get; set; } = string.Empty;

    [ForeignKey(nameof(ApplicationUserId))]
    [InverseProperty(nameof(ApplicationUser.Estudiante))]
    public ApplicationUser ApplicationUser { get; set; } = null!;

    [InverseProperty("Estudiante")]
    public ICollection<Solicitudes> Solicitudes { get; set; }
        = new List<Solicitudes>();
   
    [InverseProperty("Estudiante")]
    public ICollection<Asignaciones> Asignaciones { get; set; }
        = new List<Asignaciones>();
}
