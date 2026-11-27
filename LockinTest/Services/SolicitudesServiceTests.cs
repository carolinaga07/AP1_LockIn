
using LockinTest.Helpers;
using Lock_In.Core.Entities;
using Lock_In.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Lock_In.Infrastructure.Services;


namespace LockinTest.Services;

public class SolicitudesServiceTests
{
    [Fact]
    public async Task SolicitudPendiente_SeGuardaCorrectamente()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int solicitudId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante@test.com",
                Email = "estudiante@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260001",
                Carrera = "Ingenieria",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 1,
                Fila = 1,
                Estado = EstadoCasillero.Disponible,
                LlaveDisponible = true
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            contexto.Solicitudes.Add(solicitud);
            await contexto.SaveChangesAsync();

            solicitudId = solicitud.SolicitudId;
        }

        // Act
        await using var verificacion = factory.CreateDbContext();

        var guardada = await verificacion.Solicitudes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SolicitudId == solicitudId);

        // Assert
        Assert.NotNull(guardada);
        Assert.Equal(EstadoSolicitud.Pendiente, guardada.Estado);
        Assert.True(guardada.SolicitudId > 0);
    }


    [Fact]
    public async Task Guardar_EstudianteConSolicitudPendiente_NoPermiteDuplicados()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int estudianteId;
        int casilleroId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante2@test.com",
                Email = "estudiante2@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260002",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 2,
                Fila = 1,
                Estado = EstadoCasillero.Disponible,
                LlaveDisponible = true
            };

            contexto.Estudiantes.Add(estudiante);
            contexto.Casilleros.Add(casillero);

            await contexto.SaveChangesAsync();

            estudianteId = estudiante.EstudianteId;
            casilleroId = casillero.CasilleroId;

            // Ya existe una solicitud pendiente
            contexto.Solicitudes.Add(new Solicitudes
            {
                EstudianteId = estudianteId,
                CasilleroId = casilleroId,
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            });

            await contexto.SaveChangesAsync();
        }

        var notificaciones = new NotificacionesTiempoRealService();

        var servicio = new SolicitudesService(
            factory,
            notificaciones
        );

        // Act
        var resultado = await servicio.Guardar(new Solicitudes
        {
            EstudianteId = estudianteId,
            CasilleroId = casilleroId
        });

        // Assert
        Assert.True(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        int cantidad = await verificacion.Solicitudes
            .CountAsync(s => s.EstudianteId == estudianteId);

        Assert.Equal(1, cantidad);
    }


    [Fact]
    public async Task Guardar_EstudianteConAsignacionActiva_NoPermiteSolicitud()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int estudianteId;
        int casilleroId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante3@test.com",
                Email = "estudiante3@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260003",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casilleroAsignado = new Casilleros
            {
                Numero = 3,
                Fila = 1,
                Estado = EstadoCasillero.Ocupado,
                LlaveDisponible = true
            };

            var casilleroDisponible = new Casilleros
            {
                Numero = 4,
                Fila = 1,
                Estado = EstadoCasillero.Disponible,
                LlaveDisponible = true
            };

            contexto.Estudiantes.Add(estudiante);
            contexto.Casilleros.AddRange(
                casilleroAsignado,
                casilleroDisponible);

            await contexto.SaveChangesAsync();

            estudianteId = estudiante.EstudianteId;
            casilleroId = casilleroDisponible.CasilleroId;

            // Crear la solicitud correspondiente a la asignación existente
            var solicitudAnterior = new Solicitudes
            {
                EstudianteId = estudianteId,
                CasilleroId = casilleroAsignado.CasilleroId,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow
            };

            contexto.Solicitudes.Add(solicitudAnterior);
            await contexto.SaveChangesAsync();

            // La asignación requiere una llave
            var llave = new Llaves
            {
                CasilleroId = casilleroAsignado.CasilleroId
            };

            contexto.Llaves.Add(llave);
            await contexto.SaveChangesAsync();

            contexto.Asignaciones.Add(new Asignaciones
            {
                EstudianteId = estudianteId,
                CasilleroId = casilleroAsignado.CasilleroId,
                SolicitudId = solicitudAnterior.SolicitudId,
                LlaveId = llave.LlaveId,
                Estado = EstadoAsignacion.PendienteEntrega
            });

            await contexto.SaveChangesAsync();
        }

        var servicio = new SolicitudesService(
            factory,
            new NotificacionesTiempoRealService());

        // Act
        var resultado = await servicio.Guardar(new Solicitudes
        {
            EstudianteId = estudianteId,
            CasilleroId = casilleroId
        });

        // Assert
        Assert.True(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        int cantidad = await verificacion.Solicitudes
            .CountAsync(s => s.EstudianteId == estudianteId);

        Assert.Equal(1, cantidad);
    }


    [Fact]
    public async Task Guardar_CasilleroOcupado_NoPermiteSolicitud()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int estudianteId;
        int casilleroId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante4@test.com",
                Email = "estudiante4@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260004",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 5,
                Fila = 1,
                Estado = EstadoCasillero.Ocupado,
                LlaveDisponible = true
            };

            contexto.Estudiantes.Add(estudiante);
            contexto.Casilleros.Add(casillero);

            await contexto.SaveChangesAsync();

            estudianteId = estudiante.EstudianteId;
            casilleroId = casillero.CasilleroId;
        }

        var servicio = new SolicitudesService(
            factory,
            new NotificacionesTiempoRealService());

        // Act
        var resultado = await servicio.Guardar(new Solicitudes
        {
            EstudianteId = estudianteId,
            CasilleroId = casilleroId
        });

        // Assert
        Assert.True(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        int cantidad = await verificacion.Solicitudes
            .CountAsync(s => s.EstudianteId == estudianteId);

        Assert.Equal(0, cantidad);
    }


    [Fact]
    public async Task Cancelar_SolicitudPendiente_CambiaEstado()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int estudianteId;
        int solicitudId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante5@test.com",
                Email = "estudiante5@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260005",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 6,
                Fila = 1,
                Estado = EstadoCasillero.Disponible,
                LlaveDisponible = true
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            contexto.Solicitudes.Add(solicitud);
            await contexto.SaveChangesAsync();

            estudianteId = estudiante.EstudianteId;
            solicitudId = solicitud.SolicitudId;
        }

        var servicio = new SolicitudesService(
            factory,
            new NotificacionesTiempoRealService());

        // Act
        var resultado = await servicio.Cancelar(
            solicitudId,
            estudianteId);

        // Assert
        Assert.True(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var solicitudGuardada = await verificacion.Solicitudes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SolicitudId == solicitudId);

        Assert.NotNull(solicitudGuardada);

        Assert.Equal(
            EstadoSolicitud.Cancelada,
            solicitudGuardada.Estado);

        Assert.NotNull(solicitudGuardada.FechaRespuesta);
    }


    [Fact]
    public async Task Cancelar_SolicitudAprobada_NoPermiteCancelacion()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int estudianteId;
        int solicitudId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante6@test.com",
                Email = "estudiante6@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260006",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 7,
                Fila = 1,
                Estado = EstadoCasillero.Ocupado,
                LlaveDisponible = true
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow
            };

            contexto.Solicitudes.Add(solicitud);
            await contexto.SaveChangesAsync();

            estudianteId = estudiante.EstudianteId;
            solicitudId = solicitud.SolicitudId;
        }

        var servicio = new SolicitudesService(
            factory,
            new NotificacionesTiempoRealService());

        // Act
        var resultado = await servicio.Cancelar(
            solicitudId,
            estudianteId);

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var solicitudGuardada = await verificacion.Solicitudes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SolicitudId == solicitudId);

        Assert.NotNull(solicitudGuardada);

        Assert.Equal(
            EstadoSolicitud.Aprobada,
            solicitudGuardada.Estado);
    }


    [Fact]
    public async Task Aprobar_SinLlaveDisponible_DevuelveError()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int solicitudId;
        string administradorId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante7@test.com",
                Email = "estudiante7@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260007",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 8,
                Fila = 1,
                Estado = EstadoCasillero.Disponible,
                LlaveDisponible = true
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            var administrador = new ApplicationUser
            {
                UserName = "admin@test.com",
                Email = "admin@test.com"
            };

            var rol = new Microsoft.AspNetCore.Identity.IdentityRole
            {
                Name = "Administrador",
                NormalizedName = "ADMINISTRADOR"
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Users.Add(administrador);
            contexto.Roles.Add(rol);

            await contexto.SaveChangesAsync();

            contexto.UserRoles.Add(
                new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
                {
                    UserId = administrador.Id,
                    RoleId = rol.Id
                });

            await contexto.SaveChangesAsync();

            solicitudId = solicitud.SolicitudId;
            administradorId = administrador.Id;
        }

        var servicio = new SolicitudesService(
            factory,
            new NotificacionesTiempoRealService());

        // Act
        var resultado = await servicio.Aprobar(
            solicitudId,
            administradorId);

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var solicitudGuardada = await verificacion.Solicitudes
            .AsNoTracking()
            .FirstAsync(s => s.SolicitudId == solicitudId);

        Assert.Equal(
            EstadoSolicitud.Pendiente,
            solicitudGuardada.Estado);

        int asignaciones = await verificacion.Asignaciones
            .CountAsync(a => a.SolicitudId == solicitudId);

        Assert.Equal(0, asignaciones);
    }


    [Fact]
    public async Task Aprobar_SolicitudValida_CreaAsignacion()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int solicitudId;
        int casilleroId;
        int llaveId;
        string administradorId;
        string estudianteUsuarioId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante8@test.com",
                Email = "estudiante8@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260008",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 9,
                Fila = 1,
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
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            var administrador = new ApplicationUser
            {
                UserName = "admin8@test.com",
                Email = "admin8@test.com"
            };

            var rol = new Microsoft.AspNetCore.Identity.IdentityRole
            {
                Name = "Administrador",
                NormalizedName = "ADMINISTRADOR"
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);
            contexto.Users.Add(administrador);
            contexto.Roles.Add(rol);

            await contexto.SaveChangesAsync();

            contexto.UserRoles.Add(
                new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
                {
                    UserId = administrador.Id,
                    RoleId = rol.Id
                });

            await contexto.SaveChangesAsync();

            solicitudId = solicitud.SolicitudId;
            casilleroId = casillero.CasilleroId;
            llaveId = llave.LlaveId;
            administradorId = administrador.Id;
            estudianteUsuarioId = usuario.Id;
        }

        var notificaciones = new NotificacionesTiempoRealService();

        var servicio = new SolicitudesService(
            factory,
            notificaciones);

        // Act
        var resultado = await servicio.Aprobar(
            solicitudId,
            administradorId);

        // Assert
        Assert.True(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var solicitudGuardada = await verificacion.Solicitudes
            .AsNoTracking()
            .FirstAsync(s => s.SolicitudId == solicitudId);

        Assert.Equal(
            EstadoSolicitud.Aprobada,
            solicitudGuardada.Estado);

        Assert.NotNull(solicitudGuardada.FechaRespuesta);
        Assert.Equal(administradorId, solicitudGuardada.RevisadoPorId);

        var casilleroGuardado = await verificacion.Casilleros
            .AsNoTracking()
            .FirstAsync(c => c.CasilleroId == casilleroId);

        Assert.Equal(
            EstadoCasillero.Ocupado,
            casilleroGuardado.Estado);

        var asignacion = await verificacion.Asignaciones
            .AsNoTracking()
            .SingleAsync(a => a.SolicitudId == solicitudId);

        Assert.Equal(casilleroId, asignacion.CasilleroId);
        Assert.Equal(llaveId, asignacion.LlaveId);
        Assert.Equal(
            EstadoAsignacion.PendienteEntrega,
            asignacion.Estado);

        var notificacion = await verificacion.Notificaciones
            .AsNoTracking()
            .SingleAsync(n =>
                n.SolicitudId == solicitudId &&
                n.UsuarioId == estudianteUsuarioId);

        Assert.Equal("Solicitud aprobada", notificacion.Titulo);
        Assert.False(notificacion.Leida);
    }


    [Fact]
    public async Task Rechazar_SolicitudPendiente_CambiaEstado()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int solicitudId;
        string administradorId;
        string estudianteUsuarioId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante9@test.com",
                Email = "estudiante9@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260009",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 10,
                Fila = 1,
                Estado = EstadoCasillero.Disponible,
                LlaveDisponible = true
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            var administrador = new ApplicationUser
            {
                UserName = "admin9@test.com",
                Email = "admin9@test.com"
            };


            contexto.Solicitudes.Add(solicitud);
            contexto.Users.Add(administrador);

            var rol = new Microsoft.AspNetCore.Identity.IdentityRole
            {
                Name = "Administrador",
                NormalizedName = "ADMINISTRADOR"
            };

            contexto.Roles.Add(rol);

            await contexto.SaveChangesAsync();

            contexto.UserRoles.Add(
                new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
                {
                    UserId = administrador.Id,
                    RoleId = rol.Id
                });

            await contexto.SaveChangesAsync();


            solicitudId = solicitud.SolicitudId;
            administradorId = administrador.Id;
            estudianteUsuarioId = usuario.Id;
        }

        var servicio = new SolicitudesService(
            factory,
            new NotificacionesTiempoRealService());

        // Act
        var resultado = await servicio.Rechazar(
            solicitudId,
            administradorId,
            "El casillero requiere mantenimiento");

        // Assert
        Assert.True(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var solicitudGuardada = await verificacion.Solicitudes
            .AsNoTracking()
            .FirstAsync(s => s.SolicitudId == solicitudId);

        Assert.Equal(
            EstadoSolicitud.Rechazada,
            solicitudGuardada.Estado);

        Assert.Equal(
            "El casillero requiere mantenimiento",
            solicitudGuardada.MotivoRechazo);

        Assert.Equal(
            administradorId,
            solicitudGuardada.RevisadoPorId);

        Assert.NotNull(solicitudGuardada.FechaRespuesta);

        var notificacion = await verificacion.Notificaciones
            .AsNoTracking()
            .SingleAsync(n =>
                n.SolicitudId == solicitudId &&
                n.UsuarioId == estudianteUsuarioId);

        Assert.Equal("Solicitud rechazada", notificacion.Titulo);
        Assert.False(notificacion.Leida);

        int asignaciones = await verificacion.Asignaciones
            .CountAsync(a => a.SolicitudId == solicitudId);

        Assert.Equal(0, asignaciones);
    }


    [Fact]
    public async Task Rechazar_UsuarioSinPermisos_NoPermiteRechazo()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int solicitudId;
        string usuarioSinPermisosId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante10@test.com",
                Email = "estudiante10@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260010",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 11,
                Fila = 1,
                Estado = EstadoCasillero.Disponible,
                LlaveDisponible = true
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            contexto.Solicitudes.Add(solicitud);
            await contexto.SaveChangesAsync();

            solicitudId = solicitud.SolicitudId;
            usuarioSinPermisosId = usuario.Id;
        }

        var servicio = new SolicitudesService(
            factory,
            new NotificacionesTiempoRealService());

        // Act
        var resultado = await servicio.Rechazar(
            solicitudId,
            usuarioSinPermisosId,
            "Rechazo sin autorización");

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var solicitudGuardada = await verificacion.Solicitudes
            .AsNoTracking()
            .SingleAsync(s => s.SolicitudId == solicitudId);

        Assert.Equal(
            EstadoSolicitud.Pendiente,
            solicitudGuardada.Estado);

        Assert.Null(solicitudGuardada.MotivoRechazo);

        Assert.Equal(
            0,
            await verificacion.Notificaciones.CountAsync());
    }


    [Fact]
    public async Task Aprobar_UsuarioSinPermisos_NoPermiteAprobacion()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int solicitudId;
        int casilleroId;
        string usuarioSinPermisosId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante12@test.com",
                Email = "estudiante12@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260012",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 12,
                Fila = 1,
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
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);

            await contexto.SaveChangesAsync();

            solicitudId = solicitud.SolicitudId;
            casilleroId = casillero.CasilleroId;
            usuarioSinPermisosId = usuario.Id;
        }

        var servicio = new SolicitudesService(
            factory,
            new NotificacionesTiempoRealService());

        // Act
        var resultado = await servicio.Aprobar(
            solicitudId,
            usuarioSinPermisosId);

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var solicitudGuardada = await verificacion.Solicitudes
            .AsNoTracking()
            .SingleAsync(s => s.SolicitudId == solicitudId);

        Assert.Equal(
            EstadoSolicitud.Pendiente,
            solicitudGuardada.Estado);

        Assert.Null(solicitudGuardada.FechaRespuesta);

        var casilleroGuardado = await verificacion.Casilleros
            .AsNoTracking()
            .SingleAsync(c => c.CasilleroId == casilleroId);

        Assert.Equal(
            EstadoCasillero.Disponible,
            casilleroGuardado.Estado);

        Assert.Equal(
            0,
            await verificacion.Asignaciones.CountAsync());

        Assert.Equal(
            0,
            await verificacion.Notificaciones.CountAsync());
    }


    [Fact]
    public async Task Aprobar_CasilleroConAsignacionActiva_NoPermiteAprobacion()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int segundaSolicitudId;
        int casilleroId;
        string administradorId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario1 = new ApplicationUser
            {
                UserName = "estudiante13a@test.com",
                Email = "estudiante13a@test.com"
            };

            var estudiante1 = new Estudiantes
            {
                Matricula = "20260013",
                Carrera = "Fisica",
                ApplicationUser = usuario1
            };

            var usuario2 = new ApplicationUser
            {
                UserName = "estudiante13b@test.com",
                Email = "estudiante13b@test.com"
            };

            var estudiante2 = new Estudiantes
            {
                Matricula = "20260014",
                Carrera = "Fisica",
                ApplicationUser = usuario2
            };

            var casillero = new Casilleros
            {
                Numero = 13,
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

            var primeraSolicitud = new Solicitudes
            {
                Estudiante = estudiante1,
                Casillero = casillero,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow,
                FechaRespuesta = DateTime.UtcNow
            };

            var segundaSolicitud = new Solicitudes
            {
                Estudiante = estudiante2,
                Casillero = casillero,
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            var administrador = new ApplicationUser
            {
                UserName = "admin13@test.com",
                Email = "admin13@test.com"
            };

            var rol = new Microsoft.AspNetCore.Identity.IdentityRole
            {
                Name = "Administrador",
                NormalizedName = "ADMINISTRADOR"
            };

            contexto.Solicitudes.AddRange(
                primeraSolicitud,
                segundaSolicitud);

            contexto.Llaves.Add(llave);
            contexto.Users.Add(administrador);
            contexto.Roles.Add(rol);

            await contexto.SaveChangesAsync();

            var asignacion = new Asignaciones
            {
                EstudianteId = estudiante1.EstudianteId,
                CasilleroId = casillero.CasilleroId,
                SolicitudId = primeraSolicitud.SolicitudId,
                LlaveId = llave.LlaveId,
                Estado = EstadoAsignacion.PendienteEntrega
            };

            contexto.Asignaciones.Add(asignacion);

            contexto.UserRoles.Add(
                new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
                {
                    UserId = administrador.Id,
                    RoleId = rol.Id
                });

            await contexto.SaveChangesAsync();

            segundaSolicitudId = segundaSolicitud.SolicitudId;
            casilleroId = casillero.CasilleroId;
            administradorId = administrador.Id;
        }

        var servicio = new SolicitudesService(
            factory,
            new NotificacionesTiempoRealService());

        // Act
        var resultado = await servicio.Aprobar(
            segundaSolicitudId,
            administradorId);

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var solicitudGuardada = await verificacion.Solicitudes
            .AsNoTracking()
            .SingleAsync(s =>
                s.SolicitudId == segundaSolicitudId);

        Assert.Equal(
            EstadoSolicitud.Pendiente,
            solicitudGuardada.Estado);

        Assert.Null(solicitudGuardada.FechaRespuesta);

        var casilleroGuardado = await verificacion.Casilleros
            .AsNoTracking()
            .SingleAsync(c => c.CasilleroId == casilleroId);

        Assert.Equal(
            EstadoCasillero.Ocupado,
            casilleroGuardado.Estado);

        var asignaciones = await verificacion.Asignaciones
            .AsNoTracking()
            .Where(a => a.CasilleroId == casilleroId)
            .ToListAsync();

        Assert.Single(asignaciones);

        Assert.Equal(
            EstadoAsignacion.PendienteEntrega,
            asignaciones[0].Estado);

        Assert.NotEqual(
            segundaSolicitudId,
            asignaciones[0].SolicitudId);

        Assert.Equal(
            0,
            await verificacion.Notificaciones.CountAsync());
    }


    [Fact]
    public async Task Aprobar_EstudianteConAsignacionActiva_NoPermiteAprobacion()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int segundaSolicitudId;
        int segundoCasilleroId;
        int primeraAsignacionId;
        string administradorId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante14@test.com",
                Email = "estudiante14@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260015",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero1 = new Casilleros
            {
                Numero = 14,
                Fila = 1,
                Estado = EstadoCasillero.Ocupado,
                LlaveDisponible = true
            };

            var casillero2 = new Casilleros
            {
                Numero = 15,
                Fila = 1,
                Estado = EstadoCasillero.Disponible,
                LlaveDisponible = true
            };

            var llave1 = new Llaves
            {
                Casillero = casillero1,
                Activa = true,
                Estado = EstadoLlave.Disponible
            };

            var llave2 = new Llaves
            {
                Casillero = casillero2,
                Activa = true,
                Estado = EstadoLlave.Disponible
            };

            var primeraSolicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero1,
                Estado = EstadoSolicitud.Aprobada,
                FechaSolicitud = DateTime.UtcNow,
                FechaRespuesta = DateTime.UtcNow
            };

            var segundaSolicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero2,
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            var administrador = new ApplicationUser
            {
                UserName = "admin14@test.com",
                Email = "admin14@test.com"
            };

            var rol = new Microsoft.AspNetCore.Identity.IdentityRole
            {
                Name = "Administrador",
                NormalizedName = "ADMINISTRADOR"
            };

            contexto.Solicitudes.AddRange(
                primeraSolicitud,
                segundaSolicitud);

            contexto.Llaves.AddRange(llave1, llave2);
            contexto.Users.Add(administrador);
            contexto.Roles.Add(rol);

            await contexto.SaveChangesAsync();

            var asignacion = new Asignaciones
            {
                EstudianteId = estudiante.EstudianteId,
                CasilleroId = casillero1.CasilleroId,
                SolicitudId = primeraSolicitud.SolicitudId,
                LlaveId = llave1.LlaveId,
                Estado = EstadoAsignacion.PendienteEntrega
            };

            contexto.Asignaciones.Add(asignacion);

            contexto.UserRoles.Add(
                new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
                {
                    UserId = administrador.Id,
                    RoleId = rol.Id
                });

            await contexto.SaveChangesAsync();

            segundaSolicitudId = segundaSolicitud.SolicitudId;
            segundoCasilleroId = casillero2.CasilleroId;
            primeraAsignacionId = asignacion.AsignacionId;
            administradorId = administrador.Id;
        }

        var servicio = new SolicitudesService(
            factory,
            new NotificacionesTiempoRealService());

        // Act
        var resultado = await servicio.Aprobar(
            segundaSolicitudId,
            administradorId);

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var solicitudGuardada = await verificacion.Solicitudes
            .AsNoTracking()
            .SingleAsync(s =>
                s.SolicitudId == segundaSolicitudId);

        Assert.Equal(
            EstadoSolicitud.Pendiente,
            solicitudGuardada.Estado);

        Assert.Null(solicitudGuardada.FechaRespuesta);

        var segundoCasillero = await verificacion.Casilleros
            .AsNoTracking()
            .SingleAsync(c =>
                c.CasilleroId == segundoCasilleroId);

        Assert.Equal(
            EstadoCasillero.Disponible,
            segundoCasillero.Estado);

        var asignaciones = await verificacion.Asignaciones
            .AsNoTracking()
            .ToListAsync();

        Assert.Single(asignaciones);

        Assert.Equal(
            primeraAsignacionId,
            asignaciones[0].AsignacionId);

        Assert.Equal(
            EstadoAsignacion.PendienteEntrega,
            asignaciones[0].Estado);

        Assert.Equal(
            0,
            await verificacion.Notificaciones.CountAsync());
    }


    [Fact]
    public async Task Rechazar_SinMotivo_NoPermiteRechazo()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int solicitudId;
        string administradorId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante15@test.com",
                Email = "estudiante15@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260016",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 16,
                Fila = 1,
                Estado = EstadoCasillero.Disponible,
                LlaveDisponible = true
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            var administrador = new ApplicationUser
            {
                UserName = "admin15@test.com",
                Email = "admin15@test.com"
            };

            var rol = new Microsoft.AspNetCore.Identity.IdentityRole
            {
                Name = "Administrador",
                NormalizedName = "ADMINISTRADOR"
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Users.Add(administrador);
            contexto.Roles.Add(rol);

            await contexto.SaveChangesAsync();

            contexto.UserRoles.Add(
                new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
                {
                    UserId = administrador.Id,
                    RoleId = rol.Id
                });

            await contexto.SaveChangesAsync();

            solicitudId = solicitud.SolicitudId;
            administradorId = administrador.Id;
        }

        var servicio = new SolicitudesService(
            factory,
            new NotificacionesTiempoRealService());

        // Act
        var resultado = await servicio.Rechazar(
            solicitudId,
            administradorId,
            "");

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var solicitudGuardada = await verificacion.Solicitudes
            .AsNoTracking()
            .SingleAsync(s => s.SolicitudId == solicitudId);

        Assert.Equal(
            EstadoSolicitud.Pendiente,
            solicitudGuardada.Estado);

        Assert.Null(solicitudGuardada.MotivoRechazo);
        Assert.Null(solicitudGuardada.FechaRespuesta);

        Assert.Equal(
            0,
            await verificacion.Asignaciones.CountAsync());

        Assert.Equal(
            0,
            await verificacion.Notificaciones.CountAsync());
    }


    [Fact]
    public async Task Cancelar_SolicitudDeOtroEstudiante_NoPermiteCancelacion()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int solicitudId;
        int estudianteNoPropietarioId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario1 = new ApplicationUser
            {
                UserName = "estudiante16a@test.com",
                Email = "estudiante16a@test.com"
            };

            var estudiante1 = new Estudiantes
            {
                Matricula = "20260017",
                Carrera = "Fisica",
                ApplicationUser = usuario1
            };

            var usuario2 = new ApplicationUser
            {
                UserName = "estudiante16b@test.com",
                Email = "estudiante16b@test.com"
            };

            var estudiante2 = new Estudiantes
            {
                Matricula = "20260018",
                Carrera = "Fisica",
                ApplicationUser = usuario2
            };

            var casillero = new Casilleros
            {
                Numero = 17,
                Fila = 1,
                Estado = EstadoCasillero.Disponible,
                LlaveDisponible = true
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante1,
                Casillero = casillero,
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Estudiantes.Add(estudiante2);

            await contexto.SaveChangesAsync();

            solicitudId = solicitud.SolicitudId;
            estudianteNoPropietarioId = estudiante2.EstudianteId;
        }

        var servicio = new SolicitudesService(
            factory,
            new NotificacionesTiempoRealService());

        // Act
        var resultado = await servicio.Cancelar(
            solicitudId,
            estudianteNoPropietarioId);

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var solicitudGuardada = await verificacion.Solicitudes
            .AsNoTracking()
            .SingleAsync(s => s.SolicitudId == solicitudId);

        Assert.Equal(
            EstadoSolicitud.Pendiente,
            solicitudGuardada.Estado);

        Assert.Null(solicitudGuardada.FechaRespuesta);
        Assert.Null(solicitudGuardada.RevisadoPorId);

        Assert.Equal(
            0,
            await verificacion.Notificaciones.CountAsync());

        Assert.Equal(
            0,
            await verificacion.Asignaciones.CountAsync());
    }


    [Fact]
    public async Task Aprobar_LlaveInactiva_NoPermiteAprobacion()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int solicitudId;
        int casilleroId;
        string administradorId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante17@test.com",
                Email = "estudiante17@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260019",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 18,
                Fila = 1,
                Estado = EstadoCasillero.Disponible,
                LlaveDisponible = true
            };

            var llave = new Llaves
            {
                Casillero = casillero,
                Activa = false,
                Estado = EstadoLlave.Disponible
            };

            var solicitud = new Solicitudes
            {
                Estudiante = estudiante,
                Casillero = casillero,
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            var administrador = new ApplicationUser
            {
                UserName = "admin17@test.com",
                Email = "admin17@test.com"
            };

            var rol = new Microsoft.AspNetCore.Identity.IdentityRole
            {
                Name = "Administrador",
                NormalizedName = "ADMINISTRADOR"
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);
            contexto.Users.Add(administrador);
            contexto.Roles.Add(rol);

            await contexto.SaveChangesAsync();

            contexto.UserRoles.Add(
                new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
                {
                    UserId = administrador.Id,
                    RoleId = rol.Id
                });

            await contexto.SaveChangesAsync();

            solicitudId = solicitud.SolicitudId;
            casilleroId = casillero.CasilleroId;
            administradorId = administrador.Id;
        }

        var servicio = new SolicitudesService(
            factory,
            new NotificacionesTiempoRealService());

        // Act
        var resultado = await servicio.Aprobar(
            solicitudId,
            administradorId);

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var solicitudGuardada = await verificacion.Solicitudes
            .AsNoTracking()
            .SingleAsync(s => s.SolicitudId == solicitudId);

        Assert.Equal(
            EstadoSolicitud.Pendiente,
            solicitudGuardada.Estado);

        Assert.Null(solicitudGuardada.FechaRespuesta);

        var casilleroGuardado = await verificacion.Casilleros
            .AsNoTracking()
            .SingleAsync(c => c.CasilleroId == casilleroId);

        Assert.Equal(
            EstadoCasillero.Disponible,
            casilleroGuardado.Estado);

        Assert.Equal(
            0,
            await verificacion.Asignaciones.CountAsync());

        Assert.Equal(
            0,
            await verificacion.Notificaciones.CountAsync());
    }


    [Fact]
    public async Task Aprobar_LlaveNoDisponible_NoPermiteAprobacion()
    {
        // Arrange
        await using var factory =
            await TestDbContextFactory.CreateAsync();

        int solicitudId;
        int casilleroId;
        int llaveId;
        string administradorId;

        await using (var contexto = factory.CreateDbContext())
        {
            var usuario = new ApplicationUser
            {
                UserName = "estudiante18@test.com",
                Email = "estudiante18@test.com"
            };

            var estudiante = new Estudiantes
            {
                Matricula = "20260020",
                Carrera = "Fisica",
                ApplicationUser = usuario
            };

            var casillero = new Casilleros
            {
                Numero = 19,
                Fila = 1,
                Estado = EstadoCasillero.Disponible,
                LlaveDisponible = true
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
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            var administrador = new ApplicationUser
            {
                UserName = "admin18@test.com",
                Email = "admin18@test.com"
            };

            var rol = new Microsoft.AspNetCore.Identity.IdentityRole
            {
                Name = "Administrador",
                NormalizedName = "ADMINISTRADOR"
            };

            contexto.Solicitudes.Add(solicitud);
            contexto.Llaves.Add(llave);
            contexto.Users.Add(administrador);
            contexto.Roles.Add(rol);

            await contexto.SaveChangesAsync();

            contexto.UserRoles.Add(
                new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
                {
                    UserId = administrador.Id,
                    RoleId = rol.Id
                });

            await contexto.SaveChangesAsync();

            solicitudId = solicitud.SolicitudId;
            casilleroId = casillero.CasilleroId;
            llaveId = llave.LlaveId;
            administradorId = administrador.Id;
        }

        var servicio = new SolicitudesService(
            factory,
            new NotificacionesTiempoRealService());

        // Act
        var resultado = await servicio.Aprobar(
            solicitudId,
            administradorId);

        // Assert
        Assert.False(resultado.IsSuccess);

        await using var verificacion = factory.CreateDbContext();

        var solicitudGuardada = await verificacion.Solicitudes
            .AsNoTracking()
            .SingleAsync(s => s.SolicitudId == solicitudId);

        Assert.Equal(
            EstadoSolicitud.Pendiente,
            solicitudGuardada.Estado);

        Assert.Null(solicitudGuardada.FechaRespuesta);

        var casilleroGuardado = await verificacion.Casilleros
            .AsNoTracking()
            .SingleAsync(c => c.CasilleroId == casilleroId);

        Assert.Equal(
            EstadoCasillero.Disponible,
            casilleroGuardado.Estado);

        var llaveGuardada = await verificacion.Llaves
            .AsNoTracking()
            .SingleAsync(l => l.LlaveId == llaveId);

        Assert.True(llaveGuardada.Activa);
        Assert.Equal(
            EstadoLlave.Entregada,
            llaveGuardada.Estado);

        Assert.Equal(
            0,
            await verificacion.Asignaciones.CountAsync());

        Assert.Equal(
            0,
            await verificacion.Notificaciones.CountAsync());
    }

}
