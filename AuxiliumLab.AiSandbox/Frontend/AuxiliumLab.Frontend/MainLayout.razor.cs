using AuxiliumLab.Frontend.Services;

namespace AuxiliumLab.Frontend;

public partial class MainLayout
{
    private bool _notifOpen;
    private readonly List<string> _messages = [];

    protected override void OnInitialized()
    {
        Notifications.OnChange += HandleNotification;
    }

    private void HandleNotification(NotificationMessage msg)
    {
        _messages.Insert(0, $"[{msg.CreatedAt:HH:mm:ss}] {msg.Message}");
        if (_messages.Count > 50)
            _messages.RemoveAt(_messages.Count - 1);
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Notifications.OnChange -= HandleNotification;
    }
}
