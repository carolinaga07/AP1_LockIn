
using Aplicada1.Core;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Lock_In.Core.Entities;
using Lock_In.Infrastructure.Context;

namespace Lock_In.Infrastructure.Services;

public class NotificacionesService(
    IDbContextFactory<Contexto> contextFactory
) : IServiceResult<Notificaciones, int>
{
    public async Task<Result> Guardar(Notificaciones notificacion)
    {
        await using var contexto =
            await contextFactory.CreateDbContextAsync();

        if (string.IsNullOrWhiteSpace(notificacion.UsuarioId))
        {
            return Result.Failure(
                new Error("USUARIO_REQUERIDO",
                    "Debe indicar el destinatario."));
        }

        bool usuarioExiste = await contexto.Users
            .AnyAsync(u => u.Id == notificacion.UsuarioId);

        if (!usuarioExiste)
        {
            return Result.Failure(new Error("USUARIO_NO_EXISTE","El destinatario no existe."));
        }

        if (notificacion.NotificacionId == 0)
        {
            contexto.Notificaciones.Add(new Notificaciones
            {
                UsuarioId = notificacion.UsuarioId,
                Titulo = notificacion.Titulo,
                Mensaje = notificacion.Mensaje,
                Fecha = DateTime.UtcNow,
                Leida = false,
                SolicitudId = notificacion.SolicitudId,
                Url = notificacion.Url
            });
        }
        else
        {
            var existente = await contexto.Notificaciones
                .FirstOrDefaultAsync(n =>
                    n.NotificacionId == notificacion.NotificacionId &&
                    n.UsuarioId == notificacion.UsuarioId);

            if (existente is null)
            {
                return Result.Failure( new Error("NOTIFICACION_NO_ENCONTRADA","La notificación no existe."));
            }

            existente.Leida = notificacion.Leida;
        }

        var guardado = await contexto.SaveChangesAsync();

        return guardado > 0 ? Result.Success() : Result.Failure(new Error("ERROR_GUARDADO","No se guardaron los cambios."));
    }

    public async Task<Result<Notificaciones?>> Buscar(int id)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var notificacion = await contexto.Notificaciones
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.NotificacionId == id);

        return Result.Success(notificacion);
    }

    public async Task<Result<List<Notificaciones>>> GetList(
        Expression<Func<Notificaciones, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var notificaciones = await contexto.Notificaciones
            .AsNoTracking()
            .Where(criterio)
            .OrderByDescending(n => n.Fecha)
            .ToListAsync();

        return Result.Success(notificaciones);
    }

    public async Task<Result> Eliminar(int id)
    {
        await using var contexto =
            await contextFactory.CreateDbContextAsync();

        var notificacion = await contexto.Notificaciones.FirstOrDefaultAsync(n => n.NotificacionId == id);

        if (notificacion is null)
        {
            return Result.Failure( new Error("NOTIFICACION_NO_ENCONTRADA", "La notificación no existe."));
        }

        contexto.Notificaciones.Remove(notificacion);

        var eliminado = await contexto.SaveChangesAsync();

        return eliminado > 0
            ? Result.Success()
            : Result.Failure(
                new Error("ERROR_ELIMINACION",
                    "No se pudo eliminar la notificación."));
    }

    // Obtener notificaciones de un usuario
    public async Task<List<Notificaciones>> ObtenerPorUsuario(
        string usuarioId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        return await contexto.Notificaciones
            .AsNoTracking()
            .Where(n => n.UsuarioId == usuarioId)
            .OrderByDescending(n => n.Fecha)
            .ToListAsync();
    }

    // Contar notificaciones pendientes de lectura
    public async Task<int> ContarNoLeidas(string usuarioId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        return await contexto.Notificaciones
            .CountAsync(n =>
                n.UsuarioId == usuarioId &&
                !n.Leida);
    }

    // Marcar una notificación como leída
    public async Task<Result> MarcarComoLeida(
        int notificacionId,
        string usuarioId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var notificacion = await contexto.Notificaciones
            .FirstOrDefaultAsync(n =>
                n.NotificacionId == notificacionId &&
                n.UsuarioId == usuarioId);

        if (notificacion is null)
        {
            return Result.Failure(
                new Error("NOTIFICACION_NO_ENCONTRADA",
                    "La notificación no existe."));
        }

        if (notificacion.Leida)
            return Result.Success();

        notificacion.Leida = true;

        return await contexto.SaveChangesAsync() > 0
            ? Result.Success()
            : Result.Failure(
                new Error("ERROR_GUARDADO",
                    "No se pudo marcar como leída."));
    }

    // Marcar todas las notificaciones como leídas

    public async Task<Result> MarcarTodasComoLeidas(string usuarioId)
    {
        await using var contexto =
            await contextFactory.CreateDbContextAsync();

        var notificaciones = await contexto.Notificaciones
            .Where(n => n.UsuarioId == usuarioId && !n.Leida)
            .ToListAsync();

        foreach (var notificacion in notificaciones)
        {
            notificacion.Leida = true;
        }

        await contexto.SaveChangesAsync();

        return Result.Success();
    }

}
