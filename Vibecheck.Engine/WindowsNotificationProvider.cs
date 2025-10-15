using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Vibecheck.Engine;

public sealed class WindowsNotificationProvider : INotificationProvider
{
    /// <summary>
    /// Sends a toast notification via PowerShell on Windows.
    /// </summary>
    /// <param name="message">The notification message.</param>
    public void SendNotification(string message)
    {
        // Yeah, this is HACKY!
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        try
        {
            // Escape quotes, or we will have an issue
            var escapedMessage = message.Replace("\"", "`\"");
            var psScript = $"""
                $ToastXml = @"
                < toast >
                    < visual >
                    < binding template = 'ToastGeneric' >
                        < text > Vibecheck Notification </ text >
                        < text >{escapedMessage}</ text >
                    </ binding >
                    </ visual >
                </ toast >
                "@
                $xml = New - Object Windows.Data.Xml.Dom.XmlDocument
                $xml.LoadXml($ToastXml)
                $toast = [Windows.UI.Notifications.ToastNotification]::new($xml)
                $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('Vibecheck')
                $notifier.Show($toast)
                """;

            var psi = new ProcessStartInfo("powershell.exe")
            {
                Arguments = $"-NoProfile -Command \"{psScript}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send PowerShell toast notification: {ex.Message}");
        }
    }
}
