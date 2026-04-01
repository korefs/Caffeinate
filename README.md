# Caffeinate

A minimal Windows system tray app that prevents your PC from sleeping.

## Usage

Run `Caffeinate.exe` -> a circle icon appears in the system tray.

- **Right-click** the icon to open the menu
- Click **Caffeinate** to toggle sleep prevention on/off
- **Double-click** the icon to toggle as well

| State | Icon | Behavior |
|---|---|---|
| Inactive | Gray circle | Normal sleep settings apply |
| Active | Amber circle | Display and system sleep are blocked |

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

Uses the Windows [`SetThreadExecutionState`](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate) API with `ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED` to block sleep. No polling or timers — a single API call persists until toggled off or the process exits.

## License

MIT
