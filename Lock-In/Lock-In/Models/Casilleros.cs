using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Models;

public class Casilleros
{
    [Key]
    public int CasilleroId { get; set; }
    public int Numero { get; set; }
    public int Fila { get; set; }
    public EstadoCasillero Estado { get; set; } = EstadoCasillero.Disponible;

    public string? MotivoInaccesibilidad { get; set; }
    public ICollection<Solicitudes> Solicitudes { get; set; } = new List<Solicitudes>();
    public ICollection<Asignaciones> Asignaciones { get; set; } = new List<Asignaciones>();
    public bool LlaveDisponible { get; set; } = true;

}

public enum EstadoCasillero{
    Disponible, 
    Ocupado,
    Inaccesible
}
