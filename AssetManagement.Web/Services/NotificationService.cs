using System;
using System.Threading.Tasks;

namespace AssetManagement.Web.Services;

public enum ToastType
{
    Success,
    Error,
    Info,
    Warning
}

public class ToastNotification
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string Message { get; set; } = string.Empty;
    public string? Title { get; set; }
    public ToastType Type { get; set; } = ToastType.Success;
    public int DurationMs { get; set; } = 3200;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
}

public class ConfirmDialogModel
{
    public string Title { get; set; } = "Are you sure?";
    public string Message { get; set; } = string.Empty;
    public string ConfirmButtonText { get; set; } = "Confirm";
    public string CancelButtonText { get; set; } = "Cancel";
    public bool IsDestructive { get; set; } = true;
    public TaskCompletionSource<bool> CompletionSource { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public class NotificationService
{
    public event Action<ToastNotification>? OnToastAdded;
    public event Action<string>? OnToastDismissed;
    public event Action<ConfirmDialogModel>? OnConfirmRequested;

    public void ShowSuccess(string message, string? title = null, int durationMs = 3200)
    {
        AddToast(new ToastNotification
        {
            Title = title ?? "Success",
            Message = message,
            Type = ToastType.Success,
            DurationMs = durationMs
        });
    }

    public void ShowError(string message, string? title = null, int durationMs = 4500)
    {
        AddToast(new ToastNotification
        {
            Title = title ?? "Error",
            Message = message,
            Type = ToastType.Error,
            DurationMs = durationMs
        });
    }

    public void ShowWarning(string message, string? title = null, int durationMs = 4000)
    {
        AddToast(new ToastNotification
        {
            Title = title ?? "Warning",
            Message = message,
            Type = ToastType.Warning,
            DurationMs = durationMs
        });
    }

    public void ShowInfo(string message, string? title = null, int durationMs = 3500)
    {
        AddToast(new ToastNotification
        {
            Title = title ?? "Notice",
            Message = message,
            Type = ToastType.Info,
            DurationMs = durationMs
        });
    }

    public void Dismiss(string id)
    {
        OnToastDismissed?.Invoke(id);
    }

    public Task<bool> ConfirmAsync(string message, string title = "Are you sure?", string confirmText = "Confirm", string cancelText = "Cancel", bool isDestructive = true)
    {
        var model = new ConfirmDialogModel
        {
            Title = title,
            Message = message,
            ConfirmButtonText = confirmText,
            CancelButtonText = cancelText,
            IsDestructive = isDestructive
        };

        if (OnConfirmRequested != null)
        {
            OnConfirmRequested.Invoke(model);
            return model.CompletionSource.Task;
        }

        return Task.FromResult(true);
    }

    private void AddToast(ToastNotification toast)
    {
        OnToastAdded?.Invoke(toast);
    }
}
