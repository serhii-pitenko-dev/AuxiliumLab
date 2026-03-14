using AuxiliumLab.Frontend.Services;

namespace AuxiliumLab.AiSandbox.Frontend.UnitTests.Services;

[TestClass]
public class NotificationServiceTests
{
    [TestMethod]
    public void Notify_TriggersOnChangeEvent()
    {
        // Arrange
        var svc = new NotificationService();
        NotificationMessage? received = null;
        svc.OnChange += msg => received = msg;

        // Act
        svc.Notify("hello");

        // Assert
        received.Should().NotBeNull();
        received!.Message.Should().Be("hello");
        received.Severity.Should().Be(NotificationSeverity.Info);
    }

    [TestMethod]
    public void Success_SetsSeverityToSuccess()
    {
        var svc = new NotificationService();
        NotificationMessage? received = null;
        svc.OnChange += msg => received = msg;

        svc.Success("done");

        received!.Severity.Should().Be(NotificationSeverity.Success);
    }

    [TestMethod]
    public void Notify_WithNoSubscribers_DoesNotThrow()
    {
        var svc = new NotificationService();
        var act = () => svc.Notify("test");
        act.Should().NotThrow();
    }
}
