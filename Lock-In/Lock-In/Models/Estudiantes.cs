using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Models
{
    public class Estudiantes
    {
        [Key]
        public int EstudianteId { get; set; }
        
        [Required(ErrorMessage = "La matricula es obligatoria")]
        public string Matricula { get; set; } = string.Empty;

        [Required(ErrorMessage = " la carrera es obligatoria")]
        public string Carrera { get; set; } = string.Empty;
        public string ApplicationUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser ApplicationUser { get; set; } = null!;

        public ICollection<Solicitudes> Solicitudes { get; set; } = new List<Solicitudes>();

        public ICollection<Asignaciones> Asignaciones { get; set; } = new List<Asignaciones>(); 
    }
}
