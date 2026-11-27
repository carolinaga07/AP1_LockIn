
using Lock_In.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Lock_In.Infrastructure.Context;

public class Contexto : IdentityDbContext<ApplicationUser>
{
    public Contexto(DbContextOptions<Contexto> options)
        : base(options)
    {
    }

    public DbSet<Estudiantes> Estudiantes { get; set; }
    public DbSet<Casilleros> Casilleros { get; set; }
    public DbSet<Llaves> Llaves { get; set; }
    public DbSet<Solicitudes> Solicitudes { get; set; }
    public DbSet<Asignaciones> Asignaciones { get; set; }
    public DbSet<Notificaciones> Notificaciones { get; set; }
    public DbSet<HistorialMovimientos> HistorialMovimientos { get; set; }
    public DbSet<Inspecciones> Inspecciones { get; set; }
    public DbSet<DetalleInspecciones> DetalleInspecciones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Relaciones sin eliminación en cascada
        modelBuilder.Entity<Asignaciones>()
            .HasOne(a => a.Estudiante)
            .WithMany(e => e.Asignaciones)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Asignaciones>()
            .HasOne(a => a.Casillero)
            .WithMany(c => c.Asignaciones)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Asignaciones>()
            .HasOne(a => a.Solicitud)
            .WithOne(s => s.Asignaciones)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Asignaciones>()
            .HasOne(a => a.Llave)
            .WithMany(l => l.Asignaciones)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Solicitudes>()
            .HasOne(s => s.Estudiante)
            .WithMany(e => e.Solicitudes)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Solicitudes>()
            .HasOne(s => s.Casillero)
            .WithMany(c => c.Solicitudes)
            .OnDelete(DeleteBehavior.NoAction);

        // Relaciones especiales
        modelBuilder.Entity<DetalleInspecciones>()
            .HasOne(d => d.Casillero)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Inspecciones>()
            .HasOne(i => i.Responsable)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // Enums almacenados como texto
        modelBuilder.Entity<Inspecciones>()
            .Property(i => i.Estado)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<DetalleInspecciones>()
            .Property(d => d.Condicion)
            .HasConversion<string>()
            .HasMaxLength(30);
    }
}
