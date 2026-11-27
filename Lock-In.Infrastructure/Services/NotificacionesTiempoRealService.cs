
namespace Lock_In.Infrastructure.Services;

public class NotificacionesTiempoRealService
{
    public event Func<string, Task>? NotificacionCreada;

    public async Task NotificarUsuario(string usuarioId)
    {
        var manejadores = NotificacionCreada;

        if (manejadores is null)
            return;

        foreach (Func<string, Task> manejador
            in manejadores.GetInvocationList())
        {
            await manejador(usuarioId);
        }
    }
}
