using System.Runtime.InteropServices;

namespace Vibecheck.Engine;

public static class NotificationProviderFactory
{
    public static INotificationProvider? GetNotificationProvider()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsNotificationProvider();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // TODO: One day
            return null;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // TODO: One day
            return null;
        }

        // You're not actually using FreeBSD, are you? Please.
        return null;
    }
}
