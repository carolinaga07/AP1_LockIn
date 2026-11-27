
using Lock_In.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Core.Entities;

public class Llaves
{
    [Key]
    public int LlaveId { get; set; }

    public int Numero { get; set; }

    public int CasilleroId { get; set; }

    [ForeignKey(nameof(CasilleroId))]
    [InverseProperty(nameof(Casilleros.Llave))]
    public Casilleros Casillero { get; set; } = null!;

    public EstadoLlave Estado { get; set; }
        = EstadoLlave.Disponible;

    public bool Activa { get; set; } = true;

    [InverseProperty("Llave")]
    public ICollection<Asignaciones> Asignaciones { get; set; }
     = new List<Asignaciones>();
}
