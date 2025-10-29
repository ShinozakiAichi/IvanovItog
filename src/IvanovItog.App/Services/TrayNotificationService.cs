using WF = System.Windows.Forms;

namespace IvanovItog.App.Services;

public sealed class TrayNotificationService : IDisposable
{
    private readonly WF.NotifyIcon _notifyIcon;

    public TrayNotificationService()
    {
        _notifyIcon = new WF.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Information,
            Visible = true,
            Text = "IvanovItog"
        };
    }

    public void ShowInfo(string message)
    {
        _notifyIcon.BalloonTipTitle = "IvanovItog";
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = WF.ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(3000);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
