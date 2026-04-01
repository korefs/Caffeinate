using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Win32;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new CaffeinateContext());
    }
}

sealed class CaffeinateContext : ApplicationContext
{
    private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppRegistryKey     = @"Software\Caffeinate";
    private const string AppName            = "Caffeinate";

    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly Icon _iconOn;
    private readonly Icon _iconOff;
    private readonly IntPtr _iconOnHandle;
    private readonly IntPtr _iconOffHandle;
    private bool _isActive;
    private System.Windows.Forms.Timer? _timer;
    private DateTime _expiresAt;

    public CaffeinateContext()
    {
        (_iconOff, _iconOffHandle) = CreateCircleIcon(Color.FromArgb(130, 130, 130));
        (_iconOn,  _iconOnHandle)  = CreateCircleIcon(Color.FromArgb(255, 160, 0));

        _toggleItem       = new ToolStripMenuItem("Caffeinate") { CheckOnClick = false };
        _toggleItem.Click += (_, _) => Toggle();

        var timedMenu = new ToolStripMenuItem("Activate for...");
        foreach (var hours in new[] { 1, 2, 4, 8 })
        {
            var h    = hours;
            var item = new ToolStripMenuItem($"{h} hour{(h > 1 ? "s" : "")}");
            item.Click += (_, _) => ActivateFor(h);
            timedMenu.DropDownItems.Add(item);
        }

        _startupItem       = new ToolStripMenuItem("Start with Windows") { CheckOnClick = false, Checked = IsStartupEnabled() };
        _startupItem.Click += (_, _) => ToggleStartup();

        var exitItem  = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => Exit();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_toggleItem);
        menu.Items.Add(timedMenu);
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _trayIcon = new NotifyIcon
        {
            Text             = "Caffeinate — Inactive",
            Icon             = _iconOff,
            ContextMenuStrip = menu,
            Visible          = true
        };

        _trayIcon.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) Toggle();
        };

        if (GetLastActiveState())
            Toggle();
    }

    private void Toggle()
    {
        CancelTimer();
        _isActive = !_isActive;
        ApplyExecutionState();
        _toggleItem.Checked = _isActive;
        _trayIcon.Icon      = _isActive ? _iconOn : _iconOff;
        UpdateTrayText();
    }

    private void ActivateFor(int hours)
    {
        CancelTimer();

        if (!_isActive)
        {
            _isActive           = true;
            _toggleItem.Checked = true;
            _trayIcon.Icon      = _iconOn;
            ApplyExecutionState();
        }

        _expiresAt   = DateTime.Now.AddHours(hours);
        _timer       = new System.Windows.Forms.Timer { Interval = 30_000 };
        _timer.Tick += OnTimerTick;
        _timer.Start();
        UpdateTrayText();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (DateTime.Now >= _expiresAt)
        {
            CancelTimer();
            _isActive           = false;
            _toggleItem.Checked = false;
            _trayIcon.Icon      = _iconOff;
            ApplyExecutionState();
        }

        UpdateTrayText();
    }

    private void CancelTimer()
    {
        if (_timer is null) return;
        _timer.Stop();
        _timer.Dispose();
        _timer = null;
    }

    private void ApplyExecutionState()
    {
        NativeMethods.SetThreadExecutionState(_isActive
            ? NativeMethods.EXECUTION_STATE.ES_CONTINUOUS | NativeMethods.EXECUTION_STATE.ES_SYSTEM_REQUIRED | NativeMethods.EXECUTION_STATE.ES_DISPLAY_REQUIRED
            : NativeMethods.EXECUTION_STATE.ES_CONTINUOUS);
    }

    private void UpdateTrayText()
    {
        if (!_isActive)
        {
            _trayIcon.Text = "Caffeinate — Inactive";
            return;
        }

        if (_timer is not null)
        {
            var remaining = _expiresAt - DateTime.Now;
            var text = remaining.TotalMinutes >= 60
                ? $"Caffeinate — Active ({(int)remaining.TotalHours}h {remaining.Minutes:D2}m left)"
                : $"Caffeinate — Active ({(int)remaining.TotalMinutes}m left)";
            _trayIcon.Text = text.Length > 63 ? text[..63] : text;
        }
        else
        {
            _trayIcon.Text = "Caffeinate — Active";
        }
    }

    private static bool GetLastActiveState()
    {
        using var key = Registry.CurrentUser.OpenSubKey(AppRegistryKey);
        return key?.GetValue("WasActive") is int val && val == 1;
    }

    private static void SaveActiveState(bool isActive)
    {
        using var key = Registry.CurrentUser.CreateSubKey(AppRegistryKey);
        key.SetValue("WasActive", isActive ? 1 : 0, RegistryValueKind.DWord);
    }

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: false);
        return key?.GetValue(AppName) is string val && val == Application.ExecutablePath;
    }

    private void ToggleStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: true);
        if (key is null) return;

        if (_startupItem.Checked)
            key.DeleteValue(AppName, throwOnMissingValue: false);
        else
            key.SetValue(AppName, Application.ExecutablePath);

        _startupItem.Checked = !_startupItem.Checked;
    }

    private void Exit()
    {
        SaveActiveState(_isActive);
        CancelTimer();
        _trayIcon.Visible = false;
        if (_isActive)
            NativeMethods.SetThreadExecutionState(NativeMethods.EXECUTION_STATE.ES_CONTINUOUS);
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CancelTimer();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _iconOn.Dispose();
            _iconOff.Dispose();
        }
        if (_iconOnHandle  != IntPtr.Zero) NativeMethods.DestroyIcon(_iconOnHandle);
        if (_iconOffHandle != IntPtr.Zero) NativeMethods.DestroyIcon(_iconOffHandle);
        base.Dispose(disposing);
    }

    private static (Icon icon, IntPtr handle) CreateCircleIcon(Color color)
    {
        using var bmp = new Bitmap(16, 16);
        using var g   = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, 14, 14);
        var handle = bmp.GetHicon();
        return (Icon.FromHandle(handle), handle);
    }
}

static class NativeMethods
{
    [Flags]
    internal enum EXECUTION_STATE : uint
    {
        ES_CONTINUOUS       = 0x80000000,
        ES_SYSTEM_REQUIRED  = 0x00000001,
        ES_DISPLAY_REQUIRED = 0x00000002,
    }

    [DllImport("kernel32.dll", SetLastError = false)]
    internal static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

    [DllImport("user32.dll", SetLastError = false)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(IntPtr hIcon);
}
