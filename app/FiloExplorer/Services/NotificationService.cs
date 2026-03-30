using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace FiloExplorer.Services;

public class NotificationService : INotificationService
{
    public void Show(string title, string message)
    {
        var content = new AppNotificationBuilder()
            .AddText(title)
            .AddText(message)
            .BuildNotification();

        AppNotificationManager.Default.Show(content);
    }
}
