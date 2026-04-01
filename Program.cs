using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

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
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly Icon _iconOn;
    private readonly Icon _iconOff;
    private readonly IntPtr _iconOnHandle;
    private readonly IntPtr _iconOffHandle;
    private bool _isActive;

    public CaffeinateContext()
    {
        (_iconOff, _iconOffHandle) = CreateCircleIcon(Color.FromArgb(130, 130, 130));
        (_iconOn, _iconOnHandle)   = CreateCircleIcon(Color.FromArgb(255, 160, 0));

        _toggleItem = new ToolStripMenuItem("Caffeinate") { CheckOnClick = false };
        _toggleItem.Click += (_, _) => Toggle();

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => Exit();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_toggleItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _trayIcon = new NotifyIcon
        {
            Text = "Caffeinate — Inactive",
            Icon = _iconOff,
            ContextMenuStrip = menu,
            Visible = true
        };

        _trayIcon.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) Toggle();
        };
    }

    private void Toggle()
    {
        _isActive = !_isActive;

        NativeMethods.SetThreadExecutionState(_isActive
            ? NativeMethods.EXECUTION_STATE.ES_CONTINUOUS | NativeMethods.EXECUTION_STATE.ES_SYSTEM_REQUIRED | NativeMethods.EXECUTION_STATE.ES_DISPLAY_REQUIRED
            : NativeMethods.EXECUTION_STATE.ES_CONTINUOUS);

        _toggleItem.Checked = _isActive;
        _trayIcon.Icon      = _isActive ? _iconOn : _iconOff;
        _trayIcon.Text      = _isActive ? "Caffeinate — Active" : "Caffeinate — Inactive";
    }

    private void Exit()
    {
        _trayIcon.Visible = false;
        if (_isActive)
            NativeMethods.SetThreadExecutionState(NativeMethods.EXECUTION_STATE.ES_CONTINUOUS);
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
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
        ES_CONTINUOUS        = 0x80000000,
        ES_SYSTEM_REQUIRED   = 0x00000001,
        ES_DISPLAY_REQUIRED  = 0x00000002,
    }

    [DllImport("kernel32.dll", SetLastError = false)]
    internal static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

    [DllImport("user32.dll", SetLastError = false)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(IntPtr hIcon);
}
