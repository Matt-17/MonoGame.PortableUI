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

### Resolved: Android text clipping in content-tight controls (back buffer vs. scissor space)

Earlier symptom: text inside a `Button` (and `ListBox` item rows) was clipped to a narrow sliver with rows
looking too tall, while standalone `TextBlock`/`TextBox` and `DataGrid` cells on the same screen rendered
fine. Root cause was **not** layout — an on-device dump proved every `BoundingRect`/`MeasureString` was
correct. MonoGame's Android back buffer defaults to a density-scaled size *smaller* than the GL surface it
renders into, so `SpriteBatch` draws stretched to the surface while `GraphicsDevice.ScissorRectangle` is
applied in the smaller back-buffer space; the mismatch grows with distance from the origin, clipping
content-tight scissor rects (button/list text sized to the text) while leaving stretched cells and full-width
labels intact. Fixed by pinning `PreferredBackBufferWidth/Height` to the real `DisplayMetrics` in the host
game (`samples/MonoGame.PortableUI.Demo.Android/AndroidDemoGame.cs`). **Any Android host of this library must
do the same.**

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
