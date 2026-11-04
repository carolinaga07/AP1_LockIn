using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Models;

public class Llaves
{
    [Key]
    public int LlaveId { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int CasilleroId { get; set; }

    [ForeignKey(nameof(CasilleroId))]
    public Casilleros Casillero { get; set; } = null!;

    public EstadoLlave Estado { get; set; }
        = EstadoLlave.Disponible;

    public bool Activa { get; set; } = true;

    public ICollection<Asignaciones> Asignaciones { get; set; }
        = new List<Asignaciones>();
}

public enum EstadoLlave
{
    Disponible,
    Entregada,
    Perdida,
    Deteriorada
}
