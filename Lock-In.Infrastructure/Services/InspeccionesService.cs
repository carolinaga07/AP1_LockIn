
using Aplicada1.Core;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Lock_In.Core.Entities;
using Lock_In.Core.Enums;
using Lock_In.Infrastructure.Context;

namespace Lock_In.Infrastructure.Services;

public class InspeccionesService(
    IDbContextFactory<Contexto> contextFactory
) : IServiceResult<Inspecciones, int>
{
    public async Task<Result> Guardar(Inspecciones inspeccion)
    {
        if (inspeccion.Detalles == null || !inspeccion.Detalles.Any())
        {
            return Result.Failure(new Error(
                "Sin detalles",
                "Debe agregar al menos un casillero a la inspección."));
        }

        if (string.IsNullOrWhiteSpace(inspeccion.ResponsableId))
        {
            return Result.Failure(new Error(
                "Responsable requerido",
                "Debe indicar el responsable de la inspección."));
        }

        if (inspeccion.Detalles
            .GroupBy(d => d.CasilleroId)
            .Any(g => g.Count() > 1))
        {
            return Result.Failure(new Error(
                "Casilleros duplicados",
                "No puede agregar el mismo casillero dos veces."));
        }

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var ids = inspeccion.Detalles
            .Select(d => d.CasilleroId)
            .ToList();

        if (ids.Any(id => id <= 0))
        {
            return Result.Failure(new Error(
                "Casillero inválido",
                "Seleccione casilleros válidos."));
        }

        var cantidadExistentes = await contexto.Casilleros
            .CountAsync(c => ids.Contains(c.CasilleroId));

        if (cantidadExistentes != ids.Count)
        {
            return Result.Failure(new Error(
                "Casilleros no encontrados",
                "Uno o más casilleros no existen."));
        }

        var responsableExiste = await contexto.Users
            .AnyAsync(u => u.Id == inspeccion.ResponsableId);

        if (!responsableExiste)
        {
            return Result.Failure(new Error(
                "Responsable no encontrado",
                "El usuario responsable no existe."));
        }

        var guardado = inspeccion.InspeccionId == 0
            ? await Insertar(contexto, inspeccion)
            : await Modificar(contexto, inspeccion);

        return guardado
            ? Result.Success()
            : Result.Failure(new Error(
                "Error al guardar",
                "No se pudo guardar la inspección."));
    }

    private static async Task<bool> Insertar(
        Contexto contexto,
        Inspecciones inspeccion)
    {
        contexto.Inspecciones.Add(inspeccion);
        return await contexto.SaveChangesAsync() > 0;
    }

    private static async Task<bool> Modificar(
        Contexto contexto,
        Inspecciones inspeccion)
    {
        var existente = await contexto.Inspecciones
            .Include(i => i.Detalles)
            .FirstOrDefaultAsync(
                i => i.InspeccionId == inspeccion.InspeccionId);

        if (existente == null)
            return false;

        existente.Fecha = inspeccion.Fecha;
        existente.ResponsableId = inspeccion.ResponsableId;
        existente.Observaciones = inspeccion.Observaciones;
        existente.Estado = inspeccion.Estado;

        contexto.DetalleInspecciones.RemoveRange(existente.Detalles);

        existente.Detalles = inspeccion.Detalles
            .Select(d => new DetalleInspecciones
            {
                CasilleroId = d.CasilleroId,
                Condicion = d.Condicion,
                Observaciones = d.Observaciones,
                RequiereMantenimiento = d.RequiereMantenimiento
            })
            .ToList();

        await contexto.SaveChangesAsync();
        return true;
    }

    public async Task<Result<Inspecciones?>> Buscar(int inspeccionId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var inspeccion = await contexto.Inspecciones
            .AsNoTracking()
            .Include(i => i.Responsable)
            .Include(i => i.Detalles)
                .ThenInclude(d => d.Casillero)
            .FirstOrDefaultAsync(
                i => i.InspeccionId == inspeccionId);

        return Result.Success(inspeccion);
    }

    public async Task<Result<List<Inspecciones>>> GetList(
        Expression<Func<Inspecciones, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var lista = await contexto.Inspecciones
            .AsNoTracking()
            .Include(i => i.Responsable)
            .Include(i => i.Detalles)
                .ThenInclude(d => d.Casillero)
            .Where(criterio)
            .OrderByDescending(i => i.Fecha)
            .ToListAsync();

        return Result.Success(lista);
    }


    public async Task<Result> Eliminar(int inspeccionId)
    {
        await using var contexto =
            await contextFactory.CreateDbContextAsync();

        var inspeccion = await contexto.Inspecciones
            .FirstOrDefaultAsync(i =>
                i.InspeccionId == inspeccionId);

        if (inspeccion == null)
        {
            return Result.Failure(
                new Error(
                    "Inspección no encontrada",
                    "La inspección indicada no existe."));
        }

        if (inspeccion.Estado == EstadoInspeccion.Completada)
        {
            return Result.Failure(
                new Error(
                    "Inspección finalizada",
                    "No se puede eliminar una inspección finalizada."));
        }

        contexto.Inspecciones.Remove(inspeccion);

        await contexto.SaveChangesAsync();

        return Result.Success();
    }

}
