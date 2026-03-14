namespace AuxiliumLab.Frontend.Services;

/// <inheritdoc />
public sealed class NotificationService : INotificationService
{
    public event Action<NotificationMessage>? OnChange;

    public void Notify(string message, NotificationSeverity severity = NotificationSeverity.Info)
        => OnChange?.Invoke(new NotificationMessage(Guid.NewGuid(), message, severity, DateTime.UtcNow));

    public void Success(string message) => Notify(message, NotificationSeverity.Success);

    public void Error(string message) => Notify(message, NotificationSeverity.Error);

    public void Warning(string message) => Notify(message, NotificationSeverity.Warning);
}
