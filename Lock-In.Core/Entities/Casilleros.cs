
using Lock_In.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lock_In.Core.Entities;

public class Casilleros
{
    [Key]
    public int CasilleroId { get; set; }

    public int Numero { get; set; }

    public int Fila { get; set; }

    public EstadoCasillero Estado { get; set; }
        = EstadoCasillero.Disponible;

    public string? MotivoInaccesibilidad { get; set; }
    
    [InverseProperty("Casillero")]
    public ICollection<Solicitudes> Solicitudes { get; set; }
        = new List<Solicitudes>();

    [InverseProperty("Casillero")]
    public ICollection<Asignaciones> Asignaciones { get; set; }
      = new List<Asignaciones>();
    public bool LlaveDisponible { get; set; } = true;

    [InverseProperty(nameof(Llaves.Casillero))]
    public Llaves? Llave { get; set; }
}
