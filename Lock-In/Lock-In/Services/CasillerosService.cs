using Aplicada1.Core;
using Lock_In.Context;
using Lock_In.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Lock_In.Services
{
    public class CasillerosService(IDbContextFactory<Contexto> contextFactory) : IServiceResult<Casilleros, int>
    {
        public async Task<Result> Guardar(Casilleros casillero)
        {

            if (!await Existe(casillero.CasilleroId))
            {
                var result = await Insertar(casillero);
                return Result.Success(result);
            }
            else
            {
                var result = await Modificar(casillero);
                return Result.Success(result);
            }

        }
        private async Task<bool> Existe(int CasilleroId)
        {
            await using var contexto = await contextFactory.CreateDbContextAsync();
            return await contexto.Casilleros.AnyAsync(c => c.CasilleroId == CasilleroId);
        }

        private async Task<bool> Insertar(Casilleros casillero)
        {
            await using var contexto = await contextFactory.CreateDbContextAsync();
            contexto.Casilleros.Add(casillero);
            return await contexto.SaveChangesAsync() > 0;
        }

        public async Task<Result> Eliminar(int casilleroId)
        {
            await using var contexto = await contextFactory.CreateDbContextAsync();
            var result = await contexto.Casilleros
                .Where(c => c.CasilleroId == casilleroId)
                .ExecuteDeleteAsync() > 0;
            return Result.Success(result);
        }

        public async Task<Result<List<Casilleros>>> GetList(Expression<Func<Casilleros, bool>> criterio)
        {
            await using var contexto = await contextFactory.CreateDbContextAsync();
            return await contexto.Casilleros
                .Where(criterio)
                .AsNoTracking()
                .ToListAsync();
        }

        private async Task<bool> Modificar(Casilleros casillero)
        {
            await using var contexto = await contextFactory.CreateDbContextAsync();
            contexto.Casilleros.Update(casillero);
            return await contexto.SaveChangesAsync() > 0;

        }

        public async Task<Result<Casilleros?>> Buscar(int casilleroId)
        {
            await using var contexto = await contextFactory.CreateDbContextAsync();
            return await contexto.Casilleros.FirstOrDefaultAsync(c => c.CasilleroId == casilleroId);
        }
    }
}
