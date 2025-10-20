using System.Diagnostics;
using System.Runtime.InteropServices;
using Vibecheck.Utility;

namespace Vibecheck.Engine;

/// <summary>
/// Sends notifications to the current Windows device.
/// </summary>
public sealed class WindowsNotificationProvider : INotificationProvider
{
    internal WindowsNotificationProvider() { }

    /// <summary>
    /// Sends a toast notification via PowerShell on Windows.
    /// </summary>
    /// <param name="message">The notification message.</param>
    public void SendNotification(string message)
    {
        using var activity = Tracing.Start();

        // TODO: This doesn't work at all. I need to think of other ways to do this.
        // https://github.com/Ceebox/Vibecheck/issues/17

        // Yeah, this is HACKY!
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        try
        {
            // Escape quotes, or we will have an issue
            var escapedMessage = message.Replace("\"", "`\"");

            // I have been having issues with triple quotes breaking this
            var psScript = $@"
$ToastXml = @'
<toast>
  <visual>
    <binding template='ToastGeneric'>
      <text>Vibecheck Notification</text>
      <text>{escapedMessage}</text>
    </binding>
  </visual>
</toast>
'@
$xml = New-Object Windows.Data.Xml.Dom.XmlDocument
$xml.LoadXml($ToastXml)
$toast = [Windows.UI.Notifications.ToastNotification]::new($xml)
$notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('Vibecheck')
$notifier.Show($toast)
";

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
            activity.AddError(ex);
        }
    }
}
