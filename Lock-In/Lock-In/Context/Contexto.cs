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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Asignaciones
        modelBuilder.Entity<Asignaciones>()
            .HasOne(a => a.Estudiante)
            .WithMany(e => e.Asignaciones)
            .HasForeignKey(a => a.EstudianteId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Asignaciones>()
            .HasOne(a => a.Casillero)
            .WithMany(c => c.Asignaciones)
            .HasForeignKey(a => a.CasilleroId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Asignaciones>()
            .HasOne(a => a.Solicitud)
            .WithOne(s => s.Asignaciones)
            .HasForeignKey<Asignaciones>(a => a.SolicitudId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Asignaciones>()
            .HasOne(a => a.Llave)
            .WithMany(l => l.Asignaciones)
            .HasForeignKey(a => a.LlaveId)
            .OnDelete(DeleteBehavior.NoAction);

        // Solicitudes
        modelBuilder.Entity<Solicitudes>()
            .HasOne(s => s.Estudiante)
            .WithMany(e => e.Solicitudes)
            .HasForeignKey(s => s.EstudianteId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Solicitudes>()
            .HasOne(s => s.Casillero)
            .WithMany(c => c.Solicitudes)
            .HasForeignKey(s => s.CasilleroId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
