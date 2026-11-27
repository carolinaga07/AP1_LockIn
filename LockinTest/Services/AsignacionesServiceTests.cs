
using Lock_In.Core.Entities;
using Lock_In.Core.Enums;
using Lock_In.Infrastructure.Services;
using LockinTest.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LockinTest.Services;

public class AsignacionesServiceTests
{
    [Fact]
    public async Task EntregarLlave_AsignacionPendiente_ActivaAsignacion()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int asignacionId;
        int casilleroId;
        int llaveId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante19@test.com",
                Email = "estudiante19@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260021",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 20,
                Fila = 1,
                Estado = EstadoCasillero.Ocupado,
                LlaveDisponible = true
            };

            var llave = new Llaves
            {
                Casillero = casillero,
                Activa = true,
                Estado = EstadoLlave.Disponible
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow,
                FechaRespuesta = DateTime.UtcNow
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);

            await contexto.SaveChangesAsync();

            var asignacion = new Asignaciones
            {
                EstudianteId = estudiante.EstudianteId,
                CasilleroId = casillero.CasilleroId,
                SolicitudId = solicitud.SolicitudId,
                LlaveId = llave.LlaveId,
                Estado = EstadoAsignacion.PendienteEntrega
            };

            contexto.Asignaciones.Add(asignacion);
            await contexto.SaveChangesAsync();

            asignacionId = asignacion.AsignacionId;
            casilleroId = casillero.CasilleroId;
            llaveId = llave.LlaveId;
        }

        var servicio = new AsignacionesService(factory);

        // Act
        var resultado = await servicio.EntregarLlave(asignacionId);

        // Assert
        Assert.True(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var asignacionGuardada = await verificacion.Asignaciones
            .AsNoTracking()
            .SingleAsync(a => a.AsignacionId == asignacionId);

        Assert.Equal(
            EstadoAsignacion.Activa,
            asignacionGuardada.Estado);

        Assert.NotNull(asignacionGuardada.FechaEntrega);

        var casilleroGuardado = await verificacion.Casilleros
            .AsNoTracking()
            .SingleAsync(c => c.CasilleroId == casilleroId);

        Assert.Equal(
            EstadoCasillero.Ocupado,
            casilleroGuardado.Estado);

        Assert.False(casilleroGuardado.LlaveDisponible);

        var llaveGuardada = await verificacion.Llaves
            .AsNoTracking()
            .SingleAsync(l => l.LlaveId == llaveId);

        Assert.Equal(
            EstadoLlave.Entregada,
            llaveGuardada.Estado);

        Assert.True(llaveGuardada.Activa);
    }


    [Fact]
    public async Task EntregarLlave_AsignacionActiva_NoPermiteSegundaEntrega()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int asignacionId;
        int casilleroId;
        int llaveId;

        var fechaEntregaOriginal = DateTime.UtcNow.AddDays(-2);

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante20@test.com",
                Email = "estudiante20@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260022",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 21,
                Fila = 1,
                Estado = EstadoCasillero.Ocupado,
                LlaveDisponible = false
            };

            var llave = new Llaves
            {
                Casillero = casillero,
                Activa = true,
                Estado = EstadoLlave.Entregada
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow.AddDays(-3),
                FechaRespuesta = DateTime.UtcNow.AddDays(-2)
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);

            await contexto.SaveChangesAsync();

            var asignacion = new Asignaciones
            {
                EstudianteId = estudiante.EstudianteId,
                CasilleroId = casillero.CasilleroId,
                SolicitudId = solicitud.SolicitudId,
                LlaveId = llave.LlaveId,
                Estado = EstadoAsignacion.Activa,
                FechaEntrega = fechaEntregaOriginal
            };

            contexto.Asignaciones.Add(asignacion);
            await contexto.SaveChangesAsync();

            asignacionId = asignacion.AsignacionId;
            casilleroId = casillero.CasilleroId;
            llaveId = llave.LlaveId;
        }

        var servicio = new AsignacionesService(factory);

        // Act
        var resultado = await servicio.EntregarLlave(asignacionId);

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var asignacionGuardada = await verificacion.Asignaciones
            .AsNoTracking()
            .SingleAsync(a => a.AsignacionId == asignacionId);

        Assert.Equal(
            EstadoAsignacion.Activa,
            asignacionGuardada.Estado);

        Assert.Equal(
            fechaEntregaOriginal,
            asignacionGuardada.FechaEntrega);

        var casilleroGuardado = await verificacion.Casilleros
            .AsNoTracking()
            .SingleAsync(c => c.CasilleroId == casilleroId);

        Assert.Equal(
            EstadoCasillero.Ocupado,
            casilleroGuardado.Estado);

        Assert.False(casilleroGuardado.LlaveDisponible);

        var llaveGuardada = await verificacion.Llaves
            .AsNoTracking()
            .SingleAsync(l => l.LlaveId == llaveId);

        Assert.Equal(
            EstadoLlave.Entregada,
            llaveGuardada.Estado);

        Assert.True(llaveGuardada.Activa);
    }


    [Fact]
    public async Task FinalizarAsignacion_LlaveDevuelta_LiberaCasillero()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int asignacionId;
        int casilleroId;
        int llaveId;

        var fechaEntrega = DateTime.UtcNow.AddDays(-5);

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante21@test.com",
                Email = "estudiante21@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260023",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 22,
                Fila = 1,
                Estado = EstadoCasillero.Ocupado,
                LlaveDisponible = false
            };

            var llave = new Llaves
            {
                Casillero = casillero,
                Activa = true,
                Estado = EstadoLlave.Entregada
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow.AddDays(-6),
                FechaRespuesta = DateTime.UtcNow.AddDays(-5)
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);

            await contexto.SaveChangesAsync();

            var asignacion = new Asignaciones
            {
                EstudianteId = estudiante.EstudianteId,
                CasilleroId = casillero.CasilleroId,
                SolicitudId = solicitud.SolicitudId,
                LlaveId = llave.LlaveId,
                Estado = EstadoAsignacion.Activa,
                FechaEntrega = fechaEntrega
            };

            contexto.Asignaciones.Add(asignacion);
            await contexto.SaveChangesAsync();

            asignacionId = asignacion.AsignacionId;
            casilleroId = casillero.CasilleroId;
            llaveId = llave.LlaveId;
        }

        var servicio = new AsignacionesService(factory);

        // Act
        var resultado = await servicio.FinalizarAsignacion(
            asignacionId,
            EstadoLlave.Disponible);

        // Assert
        Assert.True(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var asignacionGuardada = await verificacion.Asignaciones
            .AsNoTracking()
            .SingleAsync(a => a.AsignacionId == asignacionId);

        Assert.Equal(
            EstadoAsignacion.Finalizada,
            asignacionGuardada.Estado);

        Assert.Equal(
            fechaEntrega,
            asignacionGuardada.FechaEntrega);

        Assert.NotNull(asignacionGuardada.FechaDevolucion);

        var casilleroGuardado = await verificacion.Casilleros
            .AsNoTracking()
            .SingleAsync(c => c.CasilleroId == casilleroId);

        Assert.Equal(
            EstadoCasillero.Disponible,
            casilleroGuardado.Estado);

        Assert.True(casilleroGuardado.LlaveDisponible);
        Assert.Null(casilleroGuardado.MotivoInaccesibilidad);

        var llaveGuardada = await verificacion.Llaves
            .AsNoTracking()
            .SingleAsync(l => l.LlaveId == llaveId);

        Assert.Equal(
            EstadoLlave.Disponible,
            llaveGuardada.Estado);

        Assert.True(llaveGuardada.Activa);
    }


    [Fact]
    public async Task FinalizarAsignacion_LlaveDeteriorada_BloqueaCasillero()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int asignacionId;
        int casilleroId;
        int llaveId;

        var fechaEntrega = DateTime.UtcNow.AddDays(-5);

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante22@test.com",
                Email = "estudiante22@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260024",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 23,
                Fila = 1,
                Estado = EstadoCasillero.Ocupado,
                LlaveDisponible = false
            };

            var llave = new Llaves
            {
                Casillero = casillero,
                Activa = true,
                Estado = EstadoLlave.Entregada
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow.AddDays(-6),
                FechaRespuesta = DateTime.UtcNow.AddDays(-5)
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);

            await contexto.SaveChangesAsync();

            var asignacion = new Asignaciones
            {
                EstudianteId = estudiante.EstudianteId,
                CasilleroId = casillero.CasilleroId,
                SolicitudId = solicitud.SolicitudId,
                LlaveId = llave.LlaveId,
                Estado = EstadoAsignacion.Activa,
                FechaEntrega = fechaEntrega
            };

            contexto.Asignaciones.Add(asignacion);
            await contexto.SaveChangesAsync();

            asignacionId = asignacion.AsignacionId;
            casilleroId = casillero.CasilleroId;
            llaveId = llave.LlaveId;
        }

        var servicio = new AsignacionesService(factory);

        // Act
        var resultado = await servicio.FinalizarAsignacion(
            asignacionId,
            EstadoLlave.Deteriorada);

        // Assert
        Assert.True(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var asignacionGuardada = await verificacion.Asignaciones
            .AsNoTracking()
            .SingleAsync(a => a.AsignacionId == asignacionId);

        Assert.Equal(
            EstadoAsignacion.Finalizada,
            asignacionGuardada.Estado);

        Assert.Equal(
            fechaEntrega,
            asignacionGuardada.FechaEntrega);

        Assert.NotNull(asignacionGuardada.FechaDevolucion);

        var casilleroGuardado = await verificacion.Casilleros
            .AsNoTracking()
            .SingleAsync(c => c.CasilleroId == casilleroId);

        Assert.Equal(
            EstadoCasillero.Inaccesible,
            casilleroGuardado.Estado);

        Assert.Equal(
            "Mantenimiento",
            casilleroGuardado.MotivoInaccesibilidad);

        Assert.False(casilleroGuardado.LlaveDisponible);

        var llaveGuardada = await verificacion.Llaves
            .AsNoTracking()
            .SingleAsync(l => l.LlaveId == llaveId);

        Assert.Equal(
            EstadoLlave.Deteriorada,
            llaveGuardada.Estado);

        Assert.False(llaveGuardada.Activa);
    }


    [Fact]
    public async Task FinalizarAsignacion_LlavePerdida_BloqueaCasillero()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int asignacionId;
        int casilleroId;
        int llaveId;

        var fechaEntrega = DateTime.UtcNow.AddDays(-5);

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante23@test.com",
                Email = "estudiante23@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260025",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 24,
                Fila = 1,
                Estado = EstadoCasillero.Ocupado,
                LlaveDisponible = false
            };

            var llave = new Llaves
            {
                Casillero = casillero,
                Activa = true,
                Estado = EstadoLlave.Entregada
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow.AddDays(-6),
                FechaRespuesta = DateTime.UtcNow.AddDays(-5)
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);

            await contexto.SaveChangesAsync();

            var asignacion = new Asignaciones
            {
                EstudianteId = estudiante.EstudianteId,
                CasilleroId = casillero.CasilleroId,
                SolicitudId = solicitud.SolicitudId,
                LlaveId = llave.LlaveId,
                Estado = EstadoAsignacion.Activa,
                FechaEntrega = fechaEntrega
            };

            contexto.Asignaciones.Add(asignacion);
            await contexto.SaveChangesAsync();

            asignacionId = asignacion.AsignacionId;
            casilleroId = casillero.CasilleroId;
            llaveId = llave.LlaveId;
        }

        var servicio = new AsignacionesService(factory);

        // Act
        var resultado = await servicio.FinalizarAsignacion(
            asignacionId,
            EstadoLlave.Perdida);

        // Assert
        Assert.True(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var asignacionGuardada = await verificacion.Asignaciones
            .AsNoTracking()
            .SingleAsync(a => a.AsignacionId == asignacionId);

        Assert.Equal(
            EstadoAsignacion.Finalizada,
            asignacionGuardada.Estado);

        Assert.Equal(
            fechaEntrega,
            asignacionGuardada.FechaEntrega);

        Assert.NotNull(asignacionGuardada.FechaDevolucion);

        var casilleroGuardado = await verificacion.Casilleros
            .AsNoTracking()
            .SingleAsync(c => c.CasilleroId == casilleroId);

        Assert.Equal(
            EstadoCasillero.Inaccesible,
            casilleroGuardado.Estado);

        Assert.Equal(
            "Llave Pérdida",
            casilleroGuardado.MotivoInaccesibilidad);

        Assert.False(casilleroGuardado.LlaveDisponible);

        var llaveGuardada = await verificacion.Llaves
            .AsNoTracking()
            .SingleAsync(l => l.LlaveId == llaveId);

        Assert.Equal(
            EstadoLlave.Perdida,
            llaveGuardada.Estado);

        Assert.False(llaveGuardada.Activa);
    }


    [Fact]
    public async Task CalcularMontoLlavePerdida_SinPerdidas_DevuelveCero()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int estudianteId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante24@test.com",
                Email = "estudiante24@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260026",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            contexto.Estudiantes.Add(estudiante);
            await contexto.SaveChangesAsync();

            estudianteId = estudiante.EstudianteId;
        }

        var servicio = new AsignacionesService(factory);

        // Act
        var cantidadPerdidas =
            await servicio.ContarLlavesPerdidas(estudianteId);

        var tienePenalizacion =
            await servicio.TienePenalizacionPorLlavesPerdidas(estudianteId);

        var monto =
            await servicio.CalcularMontoLlavePerdida(estudianteId);

        // Assert
        Assert.Equal(0, cantidadPerdidas);
        Assert.False(tienePenalizacion);
        Assert.Equal(0m, monto);
    }


    [Fact]
    public async Task CalcularMontoLlavePerdida_UnaPerdida_Devuelve300()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int estudianteId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante25@test.com",
                Email = "estudiante25@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260027",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 25,
                Fila = 1,
                Estado = EstadoCasillero.Inaccesible,
                LlaveDisponible = false
            };

            var llave = new Llaves
            {
                Casillero = casillero,
                Activa = false,
                Estado = EstadoLlave.Perdida
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow.AddDays(-10),
                FechaRespuesta = DateTime.UtcNow.AddDays(-9)
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);

            await contexto.SaveChangesAsync();

            var asignacion = new Asignaciones
            {
                EstudianteId = estudiante.EstudianteId,
                CasilleroId = casillero.CasilleroId,
                SolicitudId = solicitud.SolicitudId,
                LlaveId = llave.LlaveId,
                Estado = EstadoAsignacion.Finalizada,
                FechaEntrega = DateTime.UtcNow.AddDays(-9),
                FechaDevolucion = DateTime.UtcNow
            };

            contexto.Asignaciones.Add(asignacion);
            await contexto.SaveChangesAsync();

            estudianteId = estudiante.EstudianteId;
        }

        var servicio = new AsignacionesService(factory);

        // Act
        var cantidadPerdidas =
            await servicio.ContarLlavesPerdidas(estudianteId);

        var tienePenalizacion =
            await servicio.TienePenalizacionPorLlavesPerdidas(estudianteId);

        var monto =
            await servicio.CalcularMontoLlavePerdida(estudianteId);

        // Assert
        Assert.Equal(1, cantidadPerdidas);

        Assert.False(tienePenalizacion);

        Assert.Equal(300m, monto);
    }


    [Fact]
    public async Task TienePenalizacionPorLlavesPerdidas_TresPerdidas_DevuelveTrue()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int estudianteId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante26@test.com",
                Email = "estudiante26@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260028",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            contexto.Estudiantes.Add(estudiante);
            await contexto.SaveChangesAsync();

            estudianteId = estudiante.EstudianteId;

            // Crear tres asignaciones con llaves perdidas.
            for (int i = 1; i <= 3; i++)
            {
                var casillero = new Casilleros
                {
                    Numero = 30 + i,
                    Fila = 1,
                    Estado = EstadoCasillero.Inaccesible,
                    LlaveDisponible = false
                };

                var llave = new Llaves
                {
                    Casillero = casillero,
                    Activa = false,
                    Estado = EstadoLlave.Perdida
                };

                var solicitud = new Solicitudes
                {
                    EstudianteId = estudianteId,
                    Casillero = casillero,
                    Estado = EstadoSolicitud.Aprobada,
                    FechaSolicitud = DateTime.UtcNow.AddDays(-10),
                    FechaRespuesta = DateTime.UtcNow.AddDays(-9)
                };

                contexto.Solicitudes.Add(solicitud);
                contexto.Llaves.Add(llave);

                await contexto.SaveChangesAsync();

                var asignacion = new Asignaciones
                {
                    EstudianteId = estudianteId,
                    CasilleroId = casillero.CasilleroId,
                    SolicitudId = solicitud.SolicitudId,
                    LlaveId = llave.LlaveId,
                    Estado = EstadoAsignacion.Finalizada,
                    FechaEntrega = DateTime.UtcNow.AddDays(-9),
                    FechaDevolucion = DateTime.UtcNow.AddDays(-8)
                };

                contexto.Asignaciones.Add(asignacion);
                await contexto.SaveChangesAsync();
            }
        }

        var servicio = new AsignacionesService(factory);

        // Act
        var cantidadPerdidas =
            await servicio.ContarLlavesPerdidas(estudianteId);

        var tienePenalizacion =
            await servicio.TienePenalizacionPorLlavesPerdidas(estudianteId);

        var monto =
            await servicio.CalcularMontoLlavePerdida(estudianteId);

        // Assert
        Assert.Equal(3, cantidadPerdidas);

        Assert.True(tienePenalizacion);

        Assert.Equal(300m, monto);
    }


    [Fact]
    public async Task CalcularMontoLlavePerdida_CuatroPerdidas_Devuelve550()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int estudianteId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante27@test.com",
                Email = "estudiante27@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260029",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            contexto.Estudiantes.Add(estudiante);
            await contexto.SaveChangesAsync();

            estudianteId = estudiante.EstudianteId;

            // Registrar cuatro llaves perdidas.
            for (int i = 1; i <= 4; i++)
            {
                var casillero = new Casilleros
                {
                    Numero = 40 + i,
                    Fila = 2,
                    Estado = EstadoCasillero.Inaccesible,
                    LlaveDisponible = false
                };

                var llave = new Llaves
                {
                    Casillero = casillero,
                    Activa = false,
                    Estado = EstadoLlave.Perdida
                };

                var solicitud = new Solicitudes
                {
                    EstudianteId = estudianteId,
                    Casillero = casillero,
                    Estado = EstadoSolicitud.Aprobada,
                    FechaSolicitud = DateTime.UtcNow.AddDays(-10),
                    FechaRespuesta = DateTime.UtcNow.AddDays(-9)
                };

                contexto.Solicitudes.Add(solicitud);
                contexto.Llaves.Add(llave);

                await contexto.SaveChangesAsync();

                var asignacion = new Asignaciones
                {
                    EstudianteId = estudianteId,
                    CasilleroId = casillero.CasilleroId,
                    SolicitudId = solicitud.SolicitudId,
                    LlaveId = llave.LlaveId,
                    Estado = EstadoAsignacion.Finalizada,
                    FechaEntrega = DateTime.UtcNow.AddDays(-9),
                    FechaDevolucion = DateTime.UtcNow.AddDays(-8)
                };

                contexto.Asignaciones.Add(asignacion);
                await contexto.SaveChangesAsync();
            }
        }

        var servicio = new AsignacionesService(factory);

        // Act
        var cantidadPerdidas =
            await servicio.ContarLlavesPerdidas(estudianteId);

        var tienePenalizacion =
            await servicio.TienePenalizacionPorLlavesPerdidas(estudianteId);

        var monto =
            await servicio.CalcularMontoLlavePerdida(estudianteId);

        // Assert
        Assert.Equal(4, cantidadPerdidas);

        Assert.True(tienePenalizacion);

        Assert.Equal(550m, monto);
    }


    [Fact]
    public async Task FinalizarAsignacion_PendienteEntrega_NoPermiteFinalizar()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int asignacionId;
        int casilleroId;
        int llaveId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante28@test.com",
                Email = "estudiante28@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260030",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 50,
                Fila = 2,
                Estado = EstadoCasillero.Ocupado,
                LlaveDisponible = true
            };

            var llave = new Llaves
            {
                Casillero = casillero,
                Activa = true,
                Estado = EstadoLlave.Disponible
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow.AddDays(-3),
                FechaRespuesta = DateTime.UtcNow.AddDays(-2)
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);

            await contexto.SaveChangesAsync();

            var asignacion = new Asignaciones
            {
                EstudianteId = estudiante.EstudianteId,
                CasilleroId = casillero.CasilleroId,
                SolicitudId = solicitud.SolicitudId,
                LlaveId = llave.LlaveId,
                Estado = EstadoAsignacion.PendienteEntrega
            };

            contexto.Asignaciones.Add(asignacion);
            await contexto.SaveChangesAsync();

            asignacionId = asignacion.AsignacionId;
            casilleroId = casillero.CasilleroId;
            llaveId = llave.LlaveId;
        }

        var servicio = new AsignacionesService(factory);

        // Act
        var resultado = await servicio.FinalizarAsignacion(
            asignacionId,
            EstadoLlave.Disponible);

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var asignacionGuardada = await verificacion.Asignaciones
            .AsNoTracking()
            .SingleAsync(a => a.AsignacionId == asignacionId);

        Assert.Equal(
            EstadoAsignacion.PendienteEntrega,
            asignacionGuardada.Estado);

        Assert.Null(asignacionGuardada.FechaDevolucion);

        var casilleroGuardado = await verificacion.Casilleros
            .AsNoTracking()
            .SingleAsync(c => c.CasilleroId == casilleroId);

        Assert.Equal(
            EstadoCasillero.Ocupado,
            casilleroGuardado.Estado);

        Assert.True(casilleroGuardado.LlaveDisponible);

        var llaveGuardada = await verificacion.Llaves
            .AsNoTracking()
            .SingleAsync(l => l.LlaveId == llaveId);

        Assert.Equal(
            EstadoLlave.Disponible,
            llaveGuardada.Estado);

        Assert.True(llaveGuardada.Activa);
    }


    [Fact]
    public async Task Guardar_AsignacionPendiente_NoPermiteActivacionDirecta()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int asignacionId;
        int casilleroId;
        int llaveId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante29@test.com",
                Email = "estudiante29@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260031",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 51,
                Fila = 2,
                Estado = EstadoCasillero.Ocupado,
                LlaveDisponible = true
            };

            var llave = new Llaves
            {
                Casillero = casillero,
                Activa = true,
                Estado = EstadoLlave.Disponible
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow.AddDays(-3),
                FechaRespuesta = DateTime.UtcNow.AddDays(-2)
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);

            await contexto.SaveChangesAsync();

            var asignacion = new Asignaciones
            {
                EstudianteId = estudiante.EstudianteId,
                CasilleroId = casillero.CasilleroId,
                SolicitudId = solicitud.SolicitudId,
                LlaveId = llave.LlaveId,
                Estado = EstadoAsignacion.PendienteEntrega
            };

            contexto.Asignaciones.Add(asignacion);
            await contexto.SaveChangesAsync();

            asignacionId = asignacion.AsignacionId;
            casilleroId = casillero.CasilleroId;
            llaveId = llave.LlaveId;
        }

        var servicio = new AsignacionesService(factory);

        // Intentar activar directamente mediante Guardar.
        var modificacion = new Asignaciones
        {
            AsignacionId = asignacionId,
            Estado = EstadoAsignacion.Activa,
            FechaEntrega = DateTime.UtcNow
        };

        // Act
        var resultado = await servicio.Guardar(modificacion);

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var asignacionGuardada = await verificacion.Asignaciones
            .AsNoTracking()
            .SingleAsync(a => a.AsignacionId == asignacionId);

        Assert.Equal(
            EstadoAsignacion.PendienteEntrega,
            asignacionGuardada.Estado);

        Assert.Null(asignacionGuardada.FechaEntrega);

        var casilleroGuardado = await verificacion.Casilleros
            .AsNoTracking()
            .SingleAsync(c => c.CasilleroId == casilleroId);

        Assert.Equal(
            EstadoCasillero.Ocupado,
            casilleroGuardado.Estado);

        Assert.True(casilleroGuardado.LlaveDisponible);

        var llaveGuardada = await verificacion.Llaves
            .AsNoTracking()
            .SingleAsync(l => l.LlaveId == llaveId);

        Assert.Equal(
            EstadoLlave.Disponible,
            llaveGuardada.Estado);

        Assert.True(llaveGuardada.Activa);
    }


    [Fact]
    public async Task FinalizarAsignacion_YaFinalizada_NoPermiteFinalizar()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int asignacionId;
        int casilleroId;
        int llaveId;

        var fechaEntrega = DateTime.UtcNow.AddDays(-5);
        var fechaDevolucion = DateTime.UtcNow.AddDays(-1);

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante30@test.com",
                Email = "estudiante30@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260032",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 52,
                Fila = 2,
                Estado = EstadoCasillero.Disponible,
                LlaveDisponible = true
            };

            var llave = new Llaves
            {
                Casillero = casillero,
                Activa = true,
                Estado = EstadoLlave.Disponible
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow.AddDays(-7),
                FechaRespuesta = DateTime.UtcNow.AddDays(-6)
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);
            await contexto.SaveChangesAsync();

            var asignacion = new Asignaciones
            {
                EstudianteId = estudiante.EstudianteId,
                CasilleroId = casillero.CasilleroId,
                SolicitudId = solicitud.SolicitudId,
                LlaveId = llave.LlaveId,
                Estado = EstadoAsignacion.Finalizada,
                FechaEntrega = fechaEntrega,
                FechaDevolucion = fechaDevolucion
            };

            contexto.Asignaciones.Add(asignacion);
            await contexto.SaveChangesAsync();

            asignacionId = asignacion.AsignacionId;
            casilleroId = casillero.CasilleroId;
            llaveId = llave.LlaveId;
        }

        var servicio = new AsignacionesService(factory);

        // Act: intentar finalizar por segunda vez,
        // indicando que ahora la llave está perdida.
        var resultado = await servicio.FinalizarAsignacion(
            asignacionId,
            EstadoLlave.Perdida);

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var asignacionGuardada = await verificacion.Asignaciones
            .AsNoTracking()
            .SingleAsync(a => a.AsignacionId == asignacionId);

        Assert.Equal(
            EstadoAsignacion.Finalizada,
            asignacionGuardada.Estado);

        Assert.Equal(
            fechaEntrega,
            asignacionGuardada.FechaEntrega);

        Assert.Equal(
            fechaDevolucion,
            asignacionGuardada.FechaDevolucion);

        var casilleroGuardado = await verificacion.Casilleros
            .AsNoTracking()
            .SingleAsync(c => c.CasilleroId == casilleroId);

        Assert.Equal(
            EstadoCasillero.Disponible,
            casilleroGuardado.Estado);

        Assert.True(casilleroGuardado.LlaveDisponible);

        var llaveGuardada = await verificacion.Llaves
            .AsNoTracking()
            .SingleAsync(l => l.LlaveId == llaveId);

        Assert.Equal(
            EstadoLlave.Disponible,
            llaveGuardada.Estado);

        Assert.True(llaveGuardada.Activa);
    }


    [Fact]
    public async Task FinalizarAsignacion_AsignacionInexistente_DevuelveFailure()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        var servicio = new AsignacionesService(factory);

        // No existe ninguna asignación con este identificador.
        int asignacionIdInexistente = 99999;

        // Act
        var resultado = await servicio.FinalizarAsignacion(
            asignacionIdInexistente,
            EstadoLlave.Disponible);

        // Assert
        Assert.False(resultado.IsSuccess);

        // Verificar que no se haya creado ninguna asignación.
        await using var verificacion = factory.CreateDbContext();

        var cantidadAsignaciones = await verificacion.Asignaciones
            .AsNoTracking()
            .CountAsync();

        Assert.Equal(0, cantidadAsignaciones);
    }


    [Fact]
    public async Task FinalizarAsignacion_EstadoLlaveInvalido_DevuelveFailure()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int asignacionId;
        int casilleroId;
        int llaveId;

        var fechaEntrega = DateTime.UtcNow.AddDays(-3);

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante32@test.com",
                Email = "estudiante32@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260034",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 54,
                Fila = 2,
                Estado = EstadoCasillero.Ocupado,
                LlaveDisponible = false
            };

            var llave = new Llaves
            {
                Casillero = casillero,
                Activa = true,
                Estado = EstadoLlave.Entregada
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow.AddDays(-5),
                FechaRespuesta = DateTime.UtcNow.AddDays(-4)
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);

            await contexto.SaveChangesAsync();

            var asignacion = new Asignaciones
            {
                EstudianteId = estudiante.EstudianteId,
                CasilleroId = casillero.CasilleroId,
                SolicitudId = solicitud.SolicitudId,
                LlaveId = llave.LlaveId,
                Estado = EstadoAsignacion.Activa,
                FechaEntrega = fechaEntrega
            };

            contexto.Asignaciones.Add(asignacion);
            await contexto.SaveChangesAsync();

            asignacionId = asignacion.AsignacionId;
            casilleroId = casillero.CasilleroId;
            llaveId = llave.LlaveId;
        }

        var servicio = new AsignacionesService(factory);

        // Act: intentar finalizar usando un estado inválido.
        var resultado = await servicio.FinalizarAsignacion(
            asignacionId,
            EstadoLlave.Entregada);

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var asignacionGuardada = await verificacion.Asignaciones
            .AsNoTracking()
            .SingleAsync(a => a.AsignacionId == asignacionId);

        Assert.Equal(
            EstadoAsignacion.Activa,
            asignacionGuardada.Estado);

        Assert.Equal(
            fechaEntrega,
            asignacionGuardada.FechaEntrega);

        Assert.Null(asignacionGuardada.FechaDevolucion);

        var casilleroGuardado = await verificacion.Casilleros
            .AsNoTracking()
            .SingleAsync(c => c.CasilleroId == casilleroId);

        Assert.Equal(
            EstadoCasillero.Ocupado,
            casilleroGuardado.Estado);

        Assert.False(casilleroGuardado.LlaveDisponible);

        var llaveGuardada = await verificacion.Llaves
            .AsNoTracking()
            .SingleAsync(l => l.LlaveId == llaveId);

        Assert.Equal(
            EstadoLlave.Entregada,
            llaveGuardada.Estado);

        Assert.True(llaveGuardada.Activa);
    }


    [Fact]
    public async Task EntregarLlave_AsignacionInexistente_DevuelveFailure()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        var servicio = new AsignacionesService(factory);

        int asignacionIdInexistente = 99999;

        // Act
        var resultado = await servicio.EntregarLlave(
            asignacionIdInexistente);

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var cantidadAsignaciones = await verificacion.Asignaciones
            .AsNoTracking()
            .CountAsync();

        var cantidadCasilleros = await verificacion.Casilleros
            .AsNoTracking()
            .CountAsync();

        var cantidadLlaves = await verificacion.Llaves
            .AsNoTracking()
            .CountAsync();

        Assert.Equal(0, cantidadAsignaciones);
        Assert.Equal(0, cantidadCasilleros);
        Assert.Equal(0, cantidadLlaves);
    }


    [Fact]
    public async Task Buscar_AsignacionExistente_DevuelveRelaciones()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int asignacionId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante34@test.com",
                Email = "estudiante34@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260036",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 56,
                Fila = 2,
                Estado = EstadoCasillero.Ocupado,
                LlaveDisponible = false
            };

            var llave = new Llaves
            {
                Casillero = casillero,
                Activa = true,
                Estado = EstadoLlave.Entregada
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow.AddDays(-5),
                FechaRespuesta = DateTime.UtcNow.AddDays(-4)
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);

            await contexto.SaveChangesAsync();

            var asignacion = new Asignaciones
            {
                EstudianteId = estudiante.EstudianteId,
                CasilleroId = casillero.CasilleroId,
                SolicitudId = solicitud.SolicitudId,
                LlaveId = llave.LlaveId,
                Estado = EstadoAsignacion.Activa,
                FechaEntrega = DateTime.UtcNow.AddDays(-3)
            };

            contexto.Asignaciones.Add(asignacion);
            await contexto.SaveChangesAsync();

            asignacionId = asignacion.AsignacionId;
        }

        var servicio = new AsignacionesService(factory);

        // Act
        var resultado = await servicio.Buscar(asignacionId);

        // Assert
        Assert.True(resultado.IsSuccess);

        Assert.NotNull(resultado.Value);

        var asignacionEncontrada = resultado.Value!;

        Assert.Equal(
            asignacionId,
            asignacionEncontrada.AsignacionId);

        Assert.Equal(
            EstadoAsignacion.Activa,
            asignacionEncontrada.Estado);

        // Verificar la relación con el estudiante.
        Assert.NotNull(asignacionEncontrada.Estudiante);

        Assert.Equal(
            "20260036",
            asignacionEncontrada.Estudiante.Matricula);

        // Verificar la relación con el casillero.
        Assert.NotNull(asignacionEncontrada.Casillero);

        Assert.Equal(
            56,
            asignacionEncontrada.Casillero.Numero);

        Assert.Equal(
            EstadoCasillero.Ocupado,
            asignacionEncontrada.Casillero.Estado);

        // Verificar la relación con la solicitud.
        Assert.NotNull(asignacionEncontrada.Solicitud);

        Assert.Equal(
            EstadoSolicitud.Aprobada,
            asignacionEncontrada.Solicitud.Estado);

        // Verificar la relación con la llave.
        Assert.NotNull(asignacionEncontrada.Llave);

        Assert.Equal(
            EstadoLlave.Entregada,
            asignacionEncontrada.Llave.Estado);

        Assert.True(asignacionEncontrada.Llave.Activa);
    }


    [Fact]
    public async Task Buscar_AsignacionInexistente_DevuelveNull()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        var servicio = new AsignacionesService(factory);

        int asignacionIdInexistente = 99999;

        // Act
        var resultado = await servicio.Buscar(
            asignacionIdInexistente);

        // Assert
        Assert.True(resultado.IsSuccess);

        Assert.Null(resultado.Value);

        // Verificar que la consulta no creó registros.
        await using var verificacion =
            factory.CreateDbContext();

        var cantidadAsignaciones =
            await verificacion.Asignaciones
                .AsNoTracking()
                .CountAsync();

        Assert.Equal(0, cantidadAsignaciones);
    }


    [Fact]
    public async Task GetList_FiltraAsignacionesPorEstado()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante36@test.com",
                Email = "estudiante36@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260038",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            contexto.Estudiantes.Add(estudiante);
            await contexto.SaveChangesAsync();

            // Crear tres asignaciones con estados diferentes.
            var estados = new[]
            {
            EstadoAsignacion.PendienteEntrega,
            EstadoAsignacion.Activa,
            EstadoAsignacion.Finalizada
        };

            for (int i = 0; i < estados.Length; i++)
            {
                var estado = estados[i];

                var casillero = new Casilleros
                {
                    Numero = 60 + i,
                    Fila = 3,
                    Estado = estado == EstadoAsignacion.Finalizada
                        ? EstadoCasillero.Disponible
                        : EstadoCasillero.Ocupado,
                    LlaveDisponible = estado != EstadoAsignacion.Activa
                };

                var llave = new Llaves
                {
                    Casillero = casillero,
                    Activa = true,
                    Estado = estado == EstadoAsignacion.Activa
                        ? EstadoLlave.Entregada
                        : EstadoLlave.Disponible
                };

                var solicitud = new Solicitudes
                {
                    EstudianteId = estudiante.EstudianteId,
                    Casillero = casillero,
                    Estado = EstadoSolicitud.Aprobada,
                    FechaSolicitud = DateTime.UtcNow.AddDays(-5),
                    FechaRespuesta = DateTime.UtcNow.AddDays(-4)
                };

                contexto.Solicitudes.Add(solicitud);
                contexto.Llaves.Add(llave);
                await contexto.SaveChangesAsync();

                var asignacion = new Asignaciones
                {
                    EstudianteId = estudiante.EstudianteId,
                    CasilleroId = casillero.CasilleroId,
                    SolicitudId = solicitud.SolicitudId,
                    LlaveId = llave.LlaveId,
                    Estado = estado,
                    FechaEntrega = estado == EstadoAsignacion.PendienteEntrega
                        ? null
                        : DateTime.UtcNow.AddDays(-3),
                    FechaDevolucion = estado == EstadoAsignacion.Finalizada
                        ? DateTime.UtcNow.AddDays(-1)
                        : null
                };

                contexto.Asignaciones.Add(asignacion);
                await contexto.SaveChangesAsync();
            }
        }

        var servicio = new AsignacionesService(factory);

        // Act: obtener únicamente asignaciones activas.
        var resultado = await servicio.GetList(
            a => a.Estado == EstadoAsignacion.Activa);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.NotNull(resultado.Value);

        var asignaciones = resultado.Value!;

        Assert.Single(asignaciones);

        Assert.All(asignaciones, a =>
            Assert.Equal(EstadoAsignacion.Activa, a.Estado));

        // Verificar que las relaciones se hayan cargado.
        var asignacionActiva = asignaciones.Single();

        Assert.NotNull(asignacionActiva.Estudiante);
        Assert.NotNull(asignacionActiva.Casillero);
        Assert.NotNull(asignacionActiva.Solicitud);
        Assert.NotNull(asignacionActiva.Llave);

        Assert.Equal(
            EstadoLlave.Entregada,
            asignacionActiva.Llave.Estado);
    }


    [Fact]
    public async Task GetList_OrdenaAsignacionesPorIdDescendente()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        var idsEsperados = new List<int>();

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante37@test.com",
                Email = "estudiante37@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260039",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            contexto.Estudiantes.Add(estudiante);
            await contexto.SaveChangesAsync();

            // Crear cinco asignaciones.
            for (int i = 1; i <= 5; i++)
            {
                var casillero = new Casilleros
                {
                    Numero = 70 + i,
                    Fila = 3,
                    Estado = EstadoCasillero.Ocupado,
                    LlaveDisponible = true
                };

                var llave = new Llaves
                {
                    Casillero = casillero,
                    Activa = true,
                    Estado = EstadoLlave.Disponible
                };

                var solicitud = new Solicitudes
                {
                    EstudianteId = estudiante.EstudianteId,
                    Casillero = casillero,
                    Estado = EstadoSolicitud.Aprobada,
                    FechaSolicitud = DateTime.UtcNow.AddDays(-5),
                    FechaRespuesta = DateTime.UtcNow.AddDays(-4)
                };

                contexto.Solicitudes.Add(solicitud);
                contexto.Llaves.Add(llave);

                await contexto.SaveChangesAsync();

                var asignacion = new Asignaciones
                {
                    EstudianteId = estudiante.EstudianteId,
                    CasilleroId = casillero.CasilleroId,
                    SolicitudId = solicitud.SolicitudId,
                    LlaveId = llave.LlaveId,
                    Estado = EstadoAsignacion.PendienteEntrega
                };

                contexto.Asignaciones.Add(asignacion);
                await contexto.SaveChangesAsync();

                idsEsperados.Add(asignacion.AsignacionId);
            }
        }

        var servicio = new AsignacionesService(factory);

        // Act
        var resultado = await servicio.GetList(a => true);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.NotNull(resultado.Value);

        var asignaciones = resultado.Value!;

        Assert.Equal(5, asignaciones.Count);

        var idsObtenidos = asignaciones
            .Select(a => a.AsignacionId)
            .ToList();

        // Orden esperado: del ID mayor al menor.
        var ordenEsperado = idsEsperados
            .OrderByDescending(id => id)
            .ToList();

        Assert.Equal(ordenEsperado, idsObtenidos);

        // Comprobar también cada par consecutivo.
        for (int i = 0; i < idsObtenidos.Count - 1; i++)
        {
            Assert.True(
                idsObtenidos[i] > idsObtenidos[i + 1]);
        }
    }


    [Fact]
    public async Task Eliminar_AsignacionExistente_NoPermiteEliminar()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int asignacionId;
        int estudianteId;
        int casilleroId;
        int llaveId;
        int solicitudId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante38@test.com",
                Email = "estudiante38@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260040",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 80,
                Fila = 3,
                Estado = EstadoCasillero.Ocupado,
                LlaveDisponible = false
            };

            var llave = new Llaves
            {
                Casillero = casillero,
                Activa = true,
                Estado = EstadoLlave.Entregada
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow.AddDays(-5),
                FechaRespuesta = DateTime.UtcNow.AddDays(-4)
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);

            await contexto.SaveChangesAsync();

            var asignacion = new Asignaciones
            {
                EstudianteId = estudiante.EstudianteId,
                CasilleroId = casillero.CasilleroId,
                SolicitudId = solicitud.SolicitudId,
                LlaveId = llave.LlaveId,
                Estado = EstadoAsignacion.Activa,
                FechaEntrega = DateTime.UtcNow.AddDays(-3)
            };

            contexto.Asignaciones.Add(asignacion);
            await contexto.SaveChangesAsync();

            asignacionId = asignacion.AsignacionId;
            estudianteId = estudiante.EstudianteId;
            casilleroId = casillero.CasilleroId;
            llaveId = llave.LlaveId;
            solicitudId = solicitud.SolicitudId;
        }

        var servicio = new AsignacionesService(factory);

        // Act
        var resultado = await servicio.Eliminar(asignacionId);

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        // La asignación debe continuar existiendo.
        var asignacionGuardada = await verificacion.Asignaciones
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.AsignacionId == asignacionId);

        Assert.NotNull(asignacionGuardada);

        Assert.Equal(
            EstadoAsignacion.Activa,
            asignacionGuardada.Estado);

        // Sus relaciones también deben conservarse.
        Assert.Equal(
            estudianteId,
            asignacionGuardada.EstudianteId);

        Assert.Equal(
            casilleroId,
            asignacionGuardada.CasilleroId);

        Assert.Equal(
            llaveId,
            asignacionGuardada.LlaveId);

        Assert.Equal(
            solicitudId,
            asignacionGuardada.SolicitudId);

        // Verificar que no se eliminaron entidades relacionadas.
        Assert.True(await verificacion.Estudiantes
            .AnyAsync(e => e.EstudianteId == estudianteId));

        Assert.True(await verificacion.Casilleros
            .AnyAsync(c => c.CasilleroId == casilleroId));

        Assert.True(await verificacion.Llaves
            .AnyAsync(l => l.LlaveId == llaveId));

        Assert.True(await verificacion.Solicitudes
            .AnyAsync(s => s.SolicitudId == solicitudId));

        // La cantidad de asignaciones debe permanecer igual.
        Assert.Equal(
            1,
            await verificacion.Asignaciones.CountAsync());
    }


}
