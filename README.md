# WindowSnapper

WindowSnapper is a Windows desktop window snapping tool planned for C#,
.NET 8, WPF, Win32 API integration, and JSON-based layout configuration.

The initial repository structure is intentionally minimal. Core behavior will
be added in small, testable layers:

- `WindowSnapper.Core` for shared models and interfaces.
- `WindowSnapper.Layouts` for pure layout calculation.
- `WindowSnapper.Storage` for local configuration files.
- `WindowSnapper.Win32` for Windows API wrappers.
- `WindowSnapper.Hotkeys` for global hotkey handling.
- `WindowSnapper.Tray` for system tray integration.
- `WindowSnapper.App` for the WPF host.

## Build

Install the .NET 8 SDK, then run:

```bash
dotnet restore
dotnet build
dotnet test
```
