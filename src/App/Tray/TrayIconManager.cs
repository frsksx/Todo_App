using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using WinForms = System.Windows.Forms;

namespace WindowsTrayTasks.Tray;

public enum TrayState
{
    Idle,        // gray
    Scheduled,   // blue
    DueSoon,     // amber
    Overdue,     // red
    Paused,      // purple
    Error,       // red with overlay
}

public sealed class TrayIconManager : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private readonly WinForms.NotifyIcon _notifyIcon;
    private readonly WinForms.ContextMenuStrip _menu;
    private TrayState _state = TrayState.Idle;
    private Icon? _currentIcon;

    public event Action? OpenMainRequested;
    public event Action? QuickAddRequested;
    public event Action? OverdueRequested;
    public event Action? SnoozeAllRequested;
    public event Action? PauseToggleRequested;
    public event Action? QuitRequested;

    public TrayIconManager()
    {
        _menu = new WinForms.ContextMenuStrip();
        _menu.Items.Add("Quick Add\tCtrl+Alt+Q", null, (_, _) => QuickAddRequested?.Invoke());
        _menu.Items.Add("Open Tasks\tCtrl+Alt+T", null, (_, _) => OpenMainRequested?.Invoke());
        _menu.Items.Add(new WinForms.ToolStripSeparator());
        _menu.Items.Add("Show Overdue", null, (_, _) => OverdueRequested?.Invoke());
        _menu.Items.Add("Snooze All (5 min)", null, (_, _) => SnoozeAllRequested?.Invoke());
        _menu.Items.Add("Pause / Resume Reminders", null, (_, _) => PauseToggleRequested?.Invoke());
        _menu.Items.Add(new WinForms.ToolStripSeparator());
        _menu.Items.Add("Quit", null, (_, _) => QuitRequested?.Invoke());

        _notifyIcon = new WinForms.NotifyIcon
        {
            Visible = true,
            ContextMenuStrip = _menu,
            Text = "Tray Tasks",
        };
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == WinForms.MouseButtons.Left) OpenMainRequested?.Invoke();
        };

        SetState(TrayState.Idle, "No reminders");
    }

    public void SetState(TrayState state, string tooltip)
    {
        _state = state;
        var icon = BuildIcon(state);
        var old = _currentIcon;
        _notifyIcon.Icon = icon;
        _currentIcon = icon;
        if (old is not null)
        {
            var handle = old.Handle;
            old.Dispose();
            DestroyIcon(handle);
        }
        // NotifyIcon.Text is capped at 127 chars
        _notifyIcon.Text = tooltip.Length > 127 ? tooltip[..127] : tooltip;
    }

    public TrayState State => _state;

    public void ShowBalloon(string title, string message)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = WinForms.ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(5000);
    }

    private static Icon BuildIcon(TrayState state)
    {
        var (fill, accent) = state switch
        {
            TrayState.Idle => (Color.FromArgb(140, 140, 140), Color.FromArgb(200, 200, 200)),
            TrayState.Scheduled => (Color.FromArgb(60, 120, 220), Color.FromArgb(140, 180, 240)),
            TrayState.DueSoon => (Color.FromArgb(240, 170, 30), Color.FromArgb(255, 210, 100)),
            TrayState.Overdue => (Color.FromArgb(220, 60, 60), Color.FromArgb(250, 130, 130)),
            TrayState.Paused => (Color.FromArgb(140, 90, 200), Color.FromArgb(190, 150, 230)),
            TrayState.Error => (Color.FromArgb(220, 60, 60), Color.FromArgb(255, 220, 60)),
            _ => (Color.Gray, Color.LightGray),
        };

        const int size = 32;
        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using var bg = new SolidBrush(fill);
            g.FillEllipse(bg, 2, 2, size - 4, size - 4);

            using var ring = new Pen(accent, 2);
            g.DrawEllipse(ring, 3, 3, size - 6, size - 6);

            // little inner shape distinguishes states by silhouette as well as color
            switch (state)
            {
                case TrayState.Idle:
                    using (var dot = new SolidBrush(accent))
                        g.FillEllipse(dot, 13, 13, 6, 6);
                    break;
                case TrayState.Scheduled:
                case TrayState.DueSoon:
                case TrayState.Overdue:
                    using (var pen = new Pen(Color.White, 2.4f))
                    {
                        // clock hands
                        g.DrawLine(pen, 16, 16, 16, 9);
                        g.DrawLine(pen, 16, 16, 21, 18);
                    }
                    break;
                case TrayState.Paused:
                    using (var pen = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(pen, 11, 10, 3, 12);
                        g.FillRectangle(pen, 18, 10, 3, 12);
                    }
                    break;
                case TrayState.Error:
                    using (var pen = new Pen(Color.White, 2.6f))
                    {
                        g.DrawLine(pen, 11, 11, 21, 21);
                        g.DrawLine(pen, 21, 11, 11, 21);
                    }
                    break;
            }
        }
        var hIcon = bmp.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        if (_currentIcon is not null)
        {
            var h = _currentIcon.Handle;
            _currentIcon.Dispose();
            DestroyIcon(h);
        }
    }
}
