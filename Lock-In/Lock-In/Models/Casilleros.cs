using System.ComponentModel.DataAnnotations;

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
    public ICollection<Llaves> Llaves { get; set; } = new List<Llaves>();

}

public enum EstadoCasillero{
    Disponible, 
    Ocupado,
    Inaccesible
}
