# Caffeinate

A minimal Windows system tray app that prevents your PC from sleeping.

## Usage

Run `Caffeinate.exe` — a circle icon appears in the system tray.

- **Right-click** the icon to open the menu
- **Double-click** the icon to toggle on/off

### Context menu

```
[ ✓ ] Caffeinate          — toggle sleep prevention indefinitely
      Activate for... ►   — activate for a fixed duration
        1 hour
        2 hours
        4 hours
        8 hours
[ ✓ ] Start with Windows  — launch automatically on login
──────────────────────
      Exit
```

### States

| State | Icon | Behavior |
|---|---|---|
| Inactive | Gray circle | Normal sleep settings apply |
| Active (indefinite) | Amber circle | Sleep blocked until manually toggled off |
| Active (timed) | Amber circle | Sleep blocked; tooltip shows time remaining |

### Timed activation

When using **Activate for...**, the tray tooltip shows the remaining time:

- `Caffeinate — Active (1h 45m left)`
- `Caffeinate — Active (23m left)`

When the timer expires, Caffeinate deactivates automatically.

### Last state memory

Caffeinate remembers whether it was active when you last closed it. On the next launch it restores the previous state automatically (indefinite mode).

### Start with Windows

Toggle **Start with Windows** to add or remove Caffeinate from the Windows login startup entries (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`). No installer or admin rights required.

## Requirements

- Windows 10/11
- [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) (x64)

## Build

```bash
dotnet build
```

**Publish single-file exe:**

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

Output: `bin/Release/net9.0-windows/win-x64/publish/Caffeinate.exe`

## How it works

Uses the Windows [`SetThreadExecutionState`](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate) API with `ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED` to block sleep. No polling or timers for the core keep-awake logic — a single API call persists until toggled off or the process exits.

State persistence uses `HKCU\Software\Caffeinate`.

## License

MIT
