using BlazorBootstrap;
namespace Lock_In.Components.Extentors;

public static class ToastServiceExtentions
{
    public static ToastMessage ShowToast(this ToastService toastService, ToastType toastType,
    string title, string customMessage = null)
    {
        var message = new ToastMessage()
        {
            Type = toastType,
            Title = title,
            Message = customMessage ?? $"A las {DateTime.Now.ToString("hh:mm tt")}"
        };
        toastService.Notify(message);
        return message;
    }

    public static ToastMessage ShowSuccess(this ToastService toastService, string customMessage = null,
        string title = "Exito")
    {
        return toastService.ShowToast(ToastType.Success, title, customMessage);
    }

    public static ToastMessage ShowError(this ToastService toastService, string customMessage = null,
        string title = "Error")
    {
        return toastService.ShowToast(ToastType.Danger, title, customMessage);
    }
}
