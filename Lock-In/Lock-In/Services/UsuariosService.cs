using Aplicada1.Core;
using Lock_In.Context;
using Lock_In.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lock_In.Services;

public class UsuariosService(UserManager<ApplicationUser> userManager, IDbContextFactory<Contexto> contextFactory)
{
    public async Task<Result> CrearUsuario(
        string nombre,
        string correo,
        string contrasena,
        string rol)
    {
        if(rol != "Administrador" && rol != "Bibliotecario")
        {
            return Result.Success(false);
        }

        var usuario = new ApplicationUser
        {
            UserName = correo.Trim(),
            Email = correo.Trim(),
            NombreCompleto = nombre.Trim(),
            EmailConfirmed = true
        };

        var resultado = await userManager.CreateAsync(usuario, contrasena);

        if (!resultado.Succeeded)
        {
            return Result.Success(false);
        }

        var resultadoRol = await userManager.AddToRoleAsync(usuario, rol);
        if (!resultadoRol.Succeeded)
        {
            await userManager.DeleteAsync(usuario);
            return Result.Success(false);
        }
        return Result.Success(true);
    }

    public async Task<Result<List<ApplicationUser>>> GetList()
    {
        await using var contexto =
            await contextFactory.CreateDbContextAsync();

        var usuarios = await contexto.Users
            .AsNoTracking()
            .ToListAsync();

        return usuarios;
    }

    public async Task<Result<ApplicationUser?>> Buscar(string id)
    {
        return await userManager.FindByIdAsync(id);
    }

    public async Task<Result> Modificar(ApplicationUser usuario,string rol)
    {
        var usuarioExistente = await userManager.FindByIdAsync(usuario.Id);

        if (usuarioExistente is null)
            return Result.Success(false);

        if (rol != "Administrador" && rol != "Bibliotecario" && rol != "Estudiante")
            return Result.Success(false);

        usuarioExistente.NombreCompleto = usuario.NombreCompleto;
        usuarioExistente.Email = usuario.Email;
        usuarioExistente.UserName = usuario.Email;

        var resultado = await userManager.UpdateAsync(usuarioExistente);

        if (!resultado.Succeeded)
            return Result.Success(false);

        var rolesActuales = await userManager.GetRolesAsync(usuarioExistente);

        if (!rolesActuales.Contains(rol))
        {
            var eliminacion = await userManager.RemoveFromRolesAsync(
                usuarioExistente, rolesActuales);

            if (!eliminacion.Succeeded)
                return Result.Success(false);

            var asignacion = await userManager.AddToRoleAsync(
                usuarioExistente, rol);

            if (!asignacion.Succeeded)
                return Result.Success(false);
        }

        return Result.Success(true);
    }

    public async Task<Dictionary<string, string>> ObtenerRoles()
    {
        await using var contexto =
            await contextFactory.CreateDbContextAsync();

        var usuariosRoles = await contexto.UserRoles.ToListAsync();
        var listaRoles = await contexto.Roles.ToListAsync();

        var roles = new Dictionary<string, string>();

        foreach (var usuarioRol in usuariosRoles)
        {
            foreach (var rol in listaRoles)
            {
                if (usuarioRol.RoleId == rol.Id)
                {
                    if (!roles.ContainsKey(usuarioRol.UserId))
                    {
                        roles[usuarioRol.UserId] = rol.Name ?? "";
                    }
                    else
                    {
                        roles[usuarioRol.UserId] += ", " + rol.Name;
                    }
                }
            }
        }

        return roles;
    }
}
