using Lock_In.Context;
using Lock_In.Data;
using Lock_In.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lock_In.Services;

public class RegistroEstudiantesService(
    Contexto contexto, 
    UserManager<ApplicationUser> userManager)
{
    public async Task<(bool Exito, string Mensaje)> Registrar(
        string nombre,
        string matricula,
        string carrera,
        string correo,
        string contrasena)
    {
        matricula = matricula.Trim();
        correo = correo.Trim();

        if (await contexto.Estudiantes
            .AnyAsync(e => e.Matricula == matricula))
        {
            return (false, "La matrícula ya está registrada.");
        }

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync();

        var usuario = new ApplicationUser
        {
            UserName = correo,
            Email = correo,
            NombreCompleto = nombre.Trim(),
            EmailConfirmed = true
        };

        var resultado = await userManager.CreateAsync(
            usuario, contrasena);

        if (!resultado.Succeeded)
        {
            string mensaje = "";

            foreach (var error in resultado.Errors)
            {
                mensaje += error.Description + " ";
            }

            return (false, mensaje.Trim());
        }

        var resultadoRol = await userManager.AddToRoleAsync(
            usuario, "Estudiante");

        if (!resultadoRol.Succeeded)
        {
            string mensaje = "";

            foreach (var error in resultadoRol.Errors)
            {
                mensaje += error.Description + " ";
            }

            return (false, mensaje.Trim());
        }

        contexto.Estudiantes.Add(new Estudiantes
        {
            ApplicationUserId = usuario.Id,
            Matricula = matricula,
            Carrera = carrera.Trim()
        });

        await contexto.SaveChangesAsync();
        await transaccion.CommitAsync();

        return (true, "Registro completado.");
    }

    public async Task<bool> ExisteMatriculaAsync(string matricula)
    {
        if (string.IsNullOrWhiteSpace(matricula))
            return false;

        return await contexto.Estudiantes
            .AnyAsync((Estudiantes e) => e.Matricula == matricula);
    }
}


