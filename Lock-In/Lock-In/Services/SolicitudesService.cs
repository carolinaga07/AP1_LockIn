using Aplicada1.Core;
using Lock_In.Context;
using Lock_In.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Lock_In.Services;

public class SolicitudesService(IDbContextFactory<Contexto> contextFactory) 
    : IServiceResult<Solicitudes, int>
{
     public async Task<Result> Guardar(Solicitudes solicitud)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        if(solicitud.SolicitudId != 0)
        {
            return Result.Success(false);
        }
        bool existeEstudiante = await contexto.Estudiantes
            .AnyAsync(e => e.EstudianteId == solicitud.EstudianteId);

        if (!existeEstudiante)
        {
            return Result.Success(false);
        }
        bool tienePendiente = await contexto.Solicitudes
            .AnyAsync(s => s.EstudianteId == solicitud.EstudianteId && s.Estado == EstadoSolicitud.Pendiente);

        if (tienePendiente)
        {
            return Result.Success(false);
        }
        bool tieneAsignacion = await contexto.Asignaciones
            .AnyAsync(a => a.EstudianteId == solicitud.EstudianteId && a.Estado != EstadoAsignacion.Finalizada);

        if (tieneAsignacion)
        {
            return Result.Success(false);
        }

        bool casilleroDisponible = await contexto.Casilleros
            .AnyAsync(c => c.CasilleroId == solicitud.CasilleroId &&
            c.Estado == EstadoCasillero.Disponible &&
            c.LlaveDisponible);

        if (!casilleroDisponible)
        {
            return Result.Success(false);
        }

        var nuevaSolicitud = new Solicitudes
        {
            EstudianteId = solicitud.EstudianteId,
            CasilleroId = solicitud.CasilleroId,
            FechaSolicitud = DateTime.UtcNow,
            Estado = EstadoSolicitud.Pendiente
        };

        contexto.Solicitudes.Add(nuevaSolicitud);

        return Result.Success(await contexto.SaveChangesAsync() > 0);
     }

    public async Task<Result<Solicitudes?>> Buscar(int solicitudId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var solicitud = await contexto.Solicitudes.AsNoTracking()
            .Include(s => s.Estudiante)
            .Include(s => s.Casillero)
            .Include(s => s.RevisadoPor)
            .FirstOrDefaultAsync(s => s.SolicitudId == solicitudId);
        return Result.Success(solicitud);
    }
    public async Task<Result<List<Solicitudes>>> GetList(Expression<Func<Solicitudes, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var solicitudes = await contexto.Solicitudes
            .AsNoTracking()
            .Include(s => s.Estudiante)
            .Include(s => s.Casillero)
            .Include(s => s.RevisadoPor)
            .Where(criterio)
            .OrderByDescending(s => s.FechaSolicitud)
            .ToListAsync();
        return Result.Success(solicitudes);
    }
    public async Task<Result> Eliminar(int solicitudId)
    {
        return Result.Success(false);
    }

    public async Task<Result> Cancelar(int solicitudId, int estudianteId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var solicitud = await contexto.Solicitudes
            .FirstOrDefaultAsync(s => s.SolicitudId == solicitudId
            && s.EstudianteId == estudianteId);

        if(solicitud is null || solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            return Result.Success(false);
        }
        solicitud.Estado = EstadoSolicitud.Cancelada;
        solicitud.FechaRespuesta = DateTime.UtcNow;

        return Result.Success(await contexto.SaveChangesAsync() > 0);
    }


    public async Task<Result> Aprobar(int solicitudId, string revisadoPor)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var solicitud = await contexto.Solicitudes
            .FirstOrDefaultAsync(s => s.SolicitudId == solicitudId);

        if (solicitud == null)
        {
            return Result.Failure(new Error("Solicitud no encontrada", "La solicitud no existe."));
        }
        if(solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            return Result.Failure(new Error("Solicitud no pendiente", "La solicitud ya fue procesada"));
        }

        var revisorExiste = await contexto.Users
            .AnyAsync(u => u.Id == revisadoPor &&
                (contexto.UserRoles.Any(ur =>
                    ur.UserId == u.Id &&
                    contexto.Roles.Any(r =>
                        r.Id == ur.RoleId &&
                        (r.Name == "Administrador" ||
                         r.Name == "Bibliotecario")))));

        if (!revisorExiste)
        {
            return Result.Failure(new Error("Revisor Invalido", "El usuario no esta autorizado para aprobar"));
        }


        var casillero = await contexto.Casilleros
            .FirstOrDefaultAsync(c =>
                c.CasilleroId == solicitud.CasilleroId);

        if (casillero == null ||
            casillero.Estado != EstadoCasillero.Disponible ||
            !casillero.LlaveDisponible)
        {
            return Result.Failure(new Error("Casillero no disponible", "El casillero no esta disponible"));
        }
         

        bool estudianteAsignado = await contexto.Asignaciones
            .AnyAsync(a =>
                a.EstudianteId == solicitud.EstudianteId &&
                a.Estado != EstadoAsignacion.Finalizada);

        if (estudianteAsignado)
        {
            return Result.Failure(new Error("Estudiante asignado", "El estudiante ya tiene una asignacion activa."));
        }

        bool casilleroAsignado = await contexto.Asignaciones
            .AnyAsync(a =>
                a.CasilleroId == solicitud.CasilleroId &&
                a.Estado != EstadoAsignacion.Finalizada);

        if (casilleroAsignado)
        {
            return Result.Failure(new Error("Casillero asignado", "El casillero ya tiene una asignacion."));
        }

        var llave = await contexto.Llaves.FirstOrDefaultAsync(
                l =>
                l.CasilleroId == solicitud.CasilleroId &&
                l.Activa &&
                l.Estado == EstadoLlave.Disponible);
           

        if (llave is null)
        {
            new Error("Llave no disponible",
                "El casillero no tiene una llave disponible.");
        }

        int llaveId = llave.LlaveId;

        bool llaveAsignada = await contexto.Asignaciones
            .AnyAsync(a =>
                a.LlaveId == llave.LlaveId &&
                a.Estado != EstadoAsignacion.Finalizada);

        if (llaveAsignada)
        {
            return Result.Failure( new Error("LLAVE_ASIGNADA",
                 "La llave ya está asignada."));
        }
            

        // Actualizar solicitud
        solicitud.Estado = EstadoSolicitud.Aprobada;
        solicitud.FechaRespuesta = DateTime.UtcNow;
        solicitud.RevisadoPorId = revisadoPor;

        // Actualizar casillero
        casillero.Estado = EstadoCasillero.Ocupado;

        // Crear asignación
        var asignacion = new Asignaciones
        {
            EstudianteId = solicitud.EstudianteId,
            CasilleroId = solicitud.CasilleroId,
            SolicitudId = solicitud.SolicitudId,
            LlaveId = llave.LlaveId,
            Estado = EstadoAsignacion.PendienteEntrega
        };

        contexto.Asignaciones.Add(asignacion);
        var guardado = await contexto.SaveChangesAsync();

        return guardado > 0 ? Result.Success() : Result.Failure(new Error("Error guardado", "No se guradaron los cambios"));

    }


    public async Task<Result> Rechazar(int solicitudId, string revisadoPor, string motivo)
    {   
        await using var contexto = await contextFactory.CreateDbContextAsync();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return Result.Failure(
             new Error(
            "MOTIVO_OBLIGATORIO",
            "Debe indicar el motivo del rechazo."
              )
            );
        }

        

        var revisorExiste = await contexto.Users.AnyAsync(u => u.Id == revisadoPor);

        if (!revisorExiste)
        {
            return Result.Success(false);
        }

        var solicitud = await contexto.Solicitudes
            .FirstOrDefaultAsync(s => s.SolicitudId == solicitudId);
        if (solicitud is null || solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            return Result.Success(false);
        }
        solicitud.Estado = EstadoSolicitud.Rechazada;
        solicitud.FechaRespuesta = DateTime.UtcNow;
        solicitud.RevisadoPorId = revisadoPor;
        solicitud.MotivoRechazo = motivo.Trim();

        var guardado = await contexto.SaveChangesAsync();

        return guardado > 0
            ? Result.Success()
            : Result.Failure(
                new Error(
                    "ERROR_GUARDADO",
                    "No se guardaron los cambios."
                )
            );
    }

    public async Task<Estudiantes?> BuscarEstudiantePorUsuario(string usuarioId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        return await contexto.Estudiantes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ApplicationUserId == usuarioId);
    }

    public async Task<Result> GuardarSolicitudEstudiante(string usuarioId, int casilleroId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var estudiante = await contexto.Estudiantes.FirstOrDefaultAsync(e => e.ApplicationUserId == usuarioId);

        if(estudiante == null)
        {
            return Result.Success(false);
        }

        var solicitud = new Solicitudes
        {
            EstudianteId = estudiante.EstudianteId,
            CasilleroId = casilleroId
        };

        return await Guardar(solicitud);
    }



}
