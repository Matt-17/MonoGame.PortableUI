# MonoGame.PortableUI TODO

Feature requests noted from consuming this library in a downstream project ("The Terra Contracts", `D:\Development\Games\The Terra Contracts`, sibling repo). Filed here so they aren't only sitting in that other repo's chat history.

## 1. Android platform support — DONE

The library now multi-targets `net10.0` (MonoGame.Framework.DesktopGL) and `net10.0-android`
(MonoGame.Framework.Android). Delivered:

- `Directory.Build.props` moved to `net10.0`; `src/MonoGame.PortableUI` and
  `src/MonoGame.PortableUI.Themes` set `<TargetFrameworks>net10.0;net10.0-android</TargetFrameworks>`
  (each clears the inherited singular `TargetFramework` first, otherwise the SDK skips the
  multi-targeting fan-out). MonoGame package references are TFM-conditional; `MonoGame.Framework.Android`
  version added to `Directory.Packages.props`.
- `AndroidClipboardService : IClipboardService` (guarded `#if ANDROID`, via `ClipboardManager`).
- Lifecycle/static-state hardening: `FontManager.Reset()` (auto-invoked when a new `Game` loads fonts),
  `ScreenEngine.Initialize` clears stale static focus, and `Backdrop`/`PostProcess` render-target managers
  are recreated when the `GraphicsDevice` changes. `Game.Window.TextInput` wiring is guarded off Android
  (desktop-only backend); `ScreenEngine.HandleTextInput(char)` is the Android-side entry point.
- `samples/MonoGame.PortableUI.Demo.Android` — minimal activity + manifest host, added to the `.slnx`.
  Verified building a signed APK and running on an emulator (touch input works).

### Known follow-up: Android native-density text rendering in nested/button content

On the Android GL backend at native display density (targetSdk 24+, e.g. an xxhdpi emulator), text that is
rendered **inside a `Button`** (the `Button` -> child `TextBlock` path, which also backs `ListBox` item rows)
is clipped to a too-narrow region and the item rows measure too tall. Standalone `TextBlock`/`TextBox` and
all non-text chrome render correctly, and the same code renders correctly on DesktopGL and under the legacy
pre-24 Android compatibility-scaling path. Likely a scissor/measure interaction specific to deeply-nested
clipped content at high DPI. Does not block the port (app builds/runs/renders/handles touch); worth a
dedicated fix before shipping button-heavy Android UI. The new `DataGrid` renders its cells as `TextBlock`s
(not `Button`s), so it is not affected by this path.

## 2. Data grid / table control — DONE

`DataGrid` + `DataGridColumn` (`src/MonoGame.PortableUI/Controls/`). Full-featured:
Auto/Absolute/star column widths (same semantics as `Grid`), click-to-sort headers (stable sort, custom
`SortKey` or text fallback), row selection with keyboard navigation and `BringIntoView`, draggable column
resize splitters (clamped to `MinWidth`), and per-column cell templates. Composed like `ListBox`
(a non-scrolling header + a `ScrollViewer` of materialized row controls). Covered by
`tests/MonoGame.PortableUI.Tests/DataGridRegressionTests.cs` and demonstrated on the desktop demo's
"Data grid" tab.

## 3. .NET 10 — DONE

The whole solution moved from `net8.0` to `net10.0` together with the Android TFM work (bundled per the
original note so the TFM decisions happened once).
