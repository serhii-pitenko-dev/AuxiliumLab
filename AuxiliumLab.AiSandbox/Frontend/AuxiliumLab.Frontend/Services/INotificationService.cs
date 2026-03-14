namespace AuxiliumLab.Frontend.Services;

/// <summary>Notification message severity levels.</summary>
public enum NotificationSeverity { Success, Info, Warning, Error }

/// <summary>A single notification entry.</summary>
public record NotificationMessage(
    Guid Id,
    string Message,
    NotificationSeverity Severity,
    DateTime CreatedAt);

/// <summary>
/// Application-wide notification service.
/// Components subscribe to <see cref="OnChange"/> to be notified of new messages.
/// </summary>
public interface INotificationService
{
    event Action<NotificationMessage>? OnChange;

    void Notify(string message, NotificationSeverity severity = NotificationSeverity.Info);
    void Success(string message);
    void Error(string message);
    void Warning(string message);
}
