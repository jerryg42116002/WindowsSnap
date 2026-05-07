# Architecture

WindowSnapper is split into small projects with one-way dependencies.

- `WindowSnapper.Core` contains shared models and interfaces only.
- `WindowSnapper.Layouts` contains pure layout logic and depends only on Core.
- `WindowSnapper.Storage` owns configuration persistence and depends on Core and Layouts.
- `WindowSnapper.Win32` owns Windows API integration and depends only on Core.
- `WindowSnapper.Hotkeys` owns hotkey commands and dispatching and depends only on Core.
- `WindowSnapper.Tray` owns system tray integration and depends only on Core.
- `WindowSnapper.App` is the WPF host and composes the modules.
