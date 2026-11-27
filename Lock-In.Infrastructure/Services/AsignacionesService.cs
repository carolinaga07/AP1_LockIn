using Aplicada1.Core;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Lock_In.Core.Entities;
using Lock_In.Core.Enums;
using Lock_In.Infrastructure.Context;

namespace Lock_In.Infrastructure.Services;
    public class AsignacionesService(IDbContextFactory<Contexto> contextFactory) : IServiceResult<Asignaciones, int>
    {

    public async Task<Result> Guardar(Asignaciones asignacion)
    {
        if (asignacion.AsignacionId == 0)
        {
            return Result.Failure(
                new Error(
                    "Error al Crear",
                    "No se pudo generar una asignación."));
        }

        await using var contexto =
            await contextFactory.CreateDbContextAsync();

        var existe = await contexto.Asignaciones
            .FirstOrDefaultAsync(a =>
                a.AsignacionId == asignacion.AsignacionId);

        if (existe == null)
        {
            return Result.Failure(
                new Error(
                    "Asignación no encontrada",
                    "La asignación indicada no existe."));
        }

        // Impedir modificaciones directas del ciclo de vida.
        if (existe.Estado != asignacion.Estado ||
            existe.FechaEntrega != asignacion.FechaEntrega ||
            existe.FechaDevolucion != asignacion.FechaDevolucion)
        {
            return Result.Failure(
                new Error(
                    "Modificación no permitida",
                    "El estado y las fechas de entrega o devolución " +
                    "solo pueden modificarse mediante los métodos " +
                    "correspondientes."));
        }

        return Result.Failure(
            new Error(
                "Sin cambios permitidos",
                "No hay propiedades administrativas editables " +
                "mediante este método."));
    }


    public async Task<Result<Asignaciones?>> Buscar(int asignacionId)
        {
           await using var contexto = await contextFactory.CreateDbContextAsync();

            var asignacion = await contexto.Asignaciones
            .AsNoTracking()
            .Include(a => a.Estudiante)
            .Include(a => a.Casillero)
            .Include(a => a.Solicitud)
            .Include(a => a.Llave)
            .FirstOrDefaultAsync(a => a.AsignacionId == asignacionId);

            return Result.Success(asignacion);
        }
        public async Task<Result<List<Asignaciones>>> GetList(Expression<Func<Asignaciones, bool>> criterio)
        {
            await using var contexto = await contextFactory.CreateDbContextAsync();

            var lista = await contexto.Asignaciones
                .AsNoTracking()
                .Include(a => a.Estudiante)
                .Include(a => a.Casillero)
                .Include(a => a.Solicitud)
                .Include(a => a.Llave)
                .Where(criterio)
                .OrderByDescending(a => a.AsignacionId)
                .ToListAsync();

            return Result.Success(lista);
        }
        public async Task<Result> Eliminar(int asignacionId)
        {
            return Result.Failure(new Error(
                "Eliminación no permitida",
                "No se permite eliminar asignaciones para preservar el historial."));
        }

        public async Task<Result> EntregarLlave(int asignacionId)
        {
            await using var contexto = await contextFactory.CreateDbContextAsync();

            var asignacion = await contexto.Asignaciones
                .Include(a => a.Casillero)
                .Include(a => a.Llave)
                .FirstOrDefaultAsync(a => a.AsignacionId == asignacionId);

            if (asignacion == null)
            {
                return Result.Failure(new Error("Asignación no encontrada", "La asignación no existe."));
            }

            if (asignacion.Estado != EstadoAsignacion.PendienteEntrega)
            {
                return Result.Failure(new Error(
                    "Estado Inválido",
                    "Solo se puede entregar la llave de una asignación pendiente de entrega."));
            }

            asignacion.Estado = EstadoAsignacion.Activa;
            asignacion.FechaEntrega = DateTime.UtcNow;

            if (asignacion.Casillero != null)
            {
                asignacion.Casillero.Estado = EstadoCasillero.Ocupado;
                asignacion.Casillero.LlaveDisponible = false;
            }

            if (asignacion.Llave != null)
            {
                asignacion.Llave.Estado = EstadoLlave.Entregada;
                asignacion.Llave.Activa = true;
            }

            var guardado = await contexto.SaveChangesAsync();
            return guardado > 0
                ? Result.Success()
                : Result.Failure(new Error("Error al guardar", "No se pudo registrar la entrega de la llave."));
        }

        public async Task<Result> FinalizarAsignacion(int asignacionId, EstadoLlave estadoFinalLlave)
        {
            if (estadoFinalLlave is not (EstadoLlave.Disponible or EstadoLlave.Deteriorada or EstadoLlave.Perdida))
            {
                return Result.Failure(new Error(
                    "Estado de llave inválido",
                    "Seleccione un estado final válido para la llave."));
            }

            await using var contexto = await contextFactory.CreateDbContextAsync();

            var asignacion = await contexto.Asignaciones
                .Include(a => a.Casillero)
                .Include(a => a.Llave)
                .FirstOrDefaultAsync(a => a.AsignacionId == asignacionId);

            if (asignacion == null)
            {
                return Result.Failure(new Error("Asignación no encontrada", "La asignación no existe."));
            }
  
            if (asignacion.Estado == EstadoAsignacion.Finalizada)
            {
                return Result.Failure(
                new Error(
                    "Ya finalizada",
                    "La asignación ya se encuentra finalizada."));
            }

            if (asignacion.Estado != EstadoAsignacion.Activa)
            {
            return Result.Failure(
                new Error(
                    "Estado inválido",
                    "Solo se pueden finalizar asignaciones activas."));
            }


            asignacion.Estado = EstadoAsignacion.Finalizada;
            asignacion.FechaDevolucion = DateTime.UtcNow;

            switch (estadoFinalLlave)
            {
                case EstadoLlave.Disponible:
                    if (asignacion.Llave != null)
                    {
                        asignacion.Llave.Estado = EstadoLlave.Disponible;
                        asignacion.Llave.Activa = true;
                    }
                    if (asignacion.Casillero != null)
                    {
                        asignacion.Casillero.Estado = EstadoCasillero.Disponible;
                        asignacion.Casillero.MotivoInaccesibilidad = null;
                        asignacion.Casillero.LlaveDisponible = true;
                    }
                    break;

                case EstadoLlave.Deteriorada:
                    if (asignacion.Llave != null)
                    {
                        asignacion.Llave.Estado = EstadoLlave.Deteriorada;
                        asignacion.Llave.Activa = false;
                    }
                    if (asignacion.Casillero != null)
                    {
                        asignacion.Casillero.Estado = EstadoCasillero.Inaccesible;
                        asignacion.Casillero.MotivoInaccesibilidad = "Mantenimiento";
                        asignacion.Casillero.LlaveDisponible = false;
                    }
                    break;

                case EstadoLlave.Perdida:
                    if (asignacion.Llave != null)
                    {
                        asignacion.Llave.Estado = EstadoLlave.Perdida;
                        asignacion.Llave.Activa = false;
                    }
                    if (asignacion.Casillero != null)
                    {
                        asignacion.Casillero.Estado = EstadoCasillero.Inaccesible;
                        asignacion.Casillero.MotivoInaccesibilidad = "Llave Pérdida";
                        asignacion.Casillero.LlaveDisponible = false;
                    }
                    break;
            }

            var guardado = await contexto.SaveChangesAsync();
            return guardado > 0
                ? Result.Success()
                : Result.Failure(new Error("Error al guardar", "No se pudo finalizar la asignación."));
        }

        public async Task<int> ContarLlavesPerdidas(int estudianteId)
        {
            await using var contexto = await contextFactory.CreateDbContextAsync();

            return await contexto.Asignaciones
                .AsNoTracking()
                .Where(a => a.EstudianteId == estudianteId && a.Llave != null && a.Llave.Estado == EstadoLlave.Perdida)
                .CountAsync();
        }

        public async Task<bool> TienePenalizacionPorLlavesPerdidas(int estudianteId)
        {
            return await ContarLlavesPerdidas(estudianteId) >= 3;
        }

        public async Task<decimal> CalcularMontoLlavePerdida(int estudianteId)
        {
            int cantidadLlavesPerdidas = await ContarLlavesPerdidas(estudianteId);

            if (cantidadLlavesPerdidas <= 0)
            {
                return 0m;
            }

            decimal montoBase = 300m;
            decimal recargoPorBloque = 250m;

            int bloquesAdicionales = (cantidadLlavesPerdidas - 1) / 3;

            return montoBase + ((decimal)bloquesAdicionales * recargoPorBloque);
        }

    }

