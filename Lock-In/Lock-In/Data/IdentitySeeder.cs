using Microsoft.AspNetCore.Identity;

namespace Lock_In.Data;

public static class IdentitySeeder
{
    public static async Task InicializarAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles =
        {
            "Administrador",
            "Bibliotecario",
            "Estudiante"
        };

        foreach (var rol in roles)
        {
            if (!await roleManager.RoleExistsAsync(rol))
            {
                var resultado = await roleManager.CreateAsync(new IdentityRole(rol));
                if (!resultado.Succeeded)
                {
                    throw new Exception(string.Join(",", resultado.Errors.Select(e => e.Description)));
                }
            }
        }

        var correo = configuration["AdminSeed:Email"];
        var contrasena = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
        {
            throw new InvalidOperationException("Configura AdminSeed:Email y AdminSeed:Password.");
        }

        var administrador = await userManager.FindByEmailAsync(correo);

        if (administrador == null)
        {
            administrador = new ApplicationUser
            {
                UserName = correo,
                Email = correo,
                NombreCompleto = "Administrador Lock-in",
                EmailConfirmed = true
            };

            var resultado = await userManager.CreateAsync(
                administrador, contrasena);

            if (!resultado.Succeeded)
            {
                throw new Exception(
                    string.Join(", ",
                        resultado.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(
           administrador, "Administrador"))
        {
            var resultado = await userManager.AddToRoleAsync(
                administrador, "Administrador");

            if (!resultado.Succeeded)
            {
                throw new Exception(
                    string.Join(", ",
                        resultado.Errors.Select(e => e.Description)));
            }
        }
    }
}
