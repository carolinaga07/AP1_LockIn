using Aplicada1.Core;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Lock_In.Infrastructure.Context;
using Lock_In.Core.Entities;
using Lock_In.Core.Enums;

using Lock_In.Infrastructure.Services;

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
        
        casillero.Estado = EstadoCasillero.Disponible;
        casillero.LlaveDisponible = true;
        casillero.Llave = new Llaves
        {
            Numero = casillero.Numero,
            Estado = EstadoLlave.Disponible,
            Activa = true
        };

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
            .Include(c => c.Llave)
            .Where(criterio)
            .AsNoTracking()
            .ToListAsync();
    }

    private async Task<bool> Modificar(Casilleros casillero)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var llave = await contexto.Llaves.FirstOrDefaultAsync(l => l.CasilleroId == casillero.CasilleroId);
        if (llave != null)
        {
            switch (casillero.Estado)
            {
                case EstadoCasillero.Inaccesible:
                    {
                        casillero.LlaveDisponible = false;
                        if(casillero.MotivoInaccesibilidad == "Llave Pérdida")
                        {
                            llave.Estado = EstadoLlave.Perdida;
                            llave.Activa = false;
                            contexto.Llaves.Update(llave);
                        }
                        break;
                    }
                case EstadoCasillero.Disponible:
                    {
                        llave.Estado = EstadoLlave.Disponible;
                        llave.Activa = true;
                        casillero.LlaveDisponible = true;
                        contexto.Llaves.Update(llave);
                        break;
                    }
                case EstadoCasillero.Ocupado:
                    {

                        llave.Estado = EstadoLlave.Entregada;
                        casillero.LlaveDisponible = false;
                        contexto.Llaves.Update(llave);
                        break;
                    }
            }
          
        }
        casillero.Llave = null;
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
        return await contexto.Casilleros
            .Include(c => c.Llave)
            .FirstOrDefaultAsync(c => c.CasilleroId == casilleroId);
    }
}
