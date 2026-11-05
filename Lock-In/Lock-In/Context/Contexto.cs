using Lock_In.Data;
using Lock_In.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Lock_In.Context;

public class Contexto : IdentityDbContext<ApplicationUser>
{
    public Contexto(DbContextOptions<Contexto>options): base(options) { }
    
    public DbSet<Estudiantes> Estudiantes { get; set; }
    public DbSet<Casilleros> Casilleros { get; set; }
    public DbSet<Llaves> Llaves { get; set; }
    public DbSet<Solicitudes> Solicitudes { get; set; }
    public DbSet<Asignaciones> Asignaciones { get; set; }
    public DbSet<Notificaciones> Notificaciones { get; set; }
    public DbSet<HistorialMovimientos> HistorialMovimientos { get; set; }


}
