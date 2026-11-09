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
            bool existe;
            await using var contexto = await contextFactory.CreateDbContextAsync();

            if (casillero.CasilleroId == 0)
            {
                existe = await contexto.Casilleros
                    .AnyAsync(c => c.Fila == casillero.Fila && c.Numero == casillero.Numero);
            }
            else
            {
                existe = await contexto.Casilleros
                    .AnyAsync(c => c.Fila == casillero.Fila && c.Numero == casillero.Numero && c.CasilleroId != casillero.CasilleroId);
            }

            if (existe)
            {
                return Result.Success(false); 
            }

            if (!await Existe(casillero.CasilleroId))
            {
                var existio = await Insertar(casillero);
                return Result.Success(existio);
            }
            else
            {
                var modifico = await Modificar(casillero);
                return Result.Success(modifico);
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

        public async Task<bool> ExisteCasillero(int fila, int numero, int casilleroId)
        {
            await using var contexto = await contextFactory.CreateDbContextAsync();
            return await contexto.Casilleros
                .AnyAsync(c => c.Fila == fila && c.Numero == numero && c.CasilleroId != casilleroId);
        }

        public async Task<Result<Casilleros?>> Buscar(int casilleroId)
        {
            await using var contexto = await contextFactory.CreateDbContextAsync();
            return await contexto.Casilleros.FirstOrDefaultAsync(c => c.CasilleroId == casilleroId);
        }
    }
}
