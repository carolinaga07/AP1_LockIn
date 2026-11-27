using Lock_In.Components;
using Lock_In.Components.Account;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Lock_In.Infrastructure.Context;
using Lock_In.Core.Entities;
using Lock_In.Infrastructure.Services;
using Lock_In.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

//Autenticacion
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

//Conexion de Sql Server
var connectionString = builder.Configuration.GetConnectionString("ConStr") ?? throw new InvalidOperationException("Connection string 'ConStr' not found.");

//Cconexion con el Contexto
builder.Services.AddDbContextFactory<Contexto>(options =>
    options.UseSqlServer(
        connectionString,
        sql => sql.MigrationsAssembly(
            typeof(Contexto).Assembly.GetName().Name)));
//Roles de Identity
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;

        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 4;
        options.Password.RequiredUniqueChars = 1;

        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<Contexto>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddScoped<RegistroEstudiantesService>();
builder.Services.AddScoped<UsuariosService>();
builder.Services.AddScoped<CasillerosService>();
builder.Services.AddScoped<SolicitudesService>();
builder.Services.AddScoped<AsignacionesService>();
builder.Services.AddScoped<NotificacionesService>();
builder.Services.AddScoped<NotificacionesTiempoRealService>();
builder.Services.AddScoped<InspeccionesService>();
builder.Services.AddBlazorBootstrap();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.InicializarAsync(
        scope.ServiceProvider,
        app.Configuration);
}

app.Run();
