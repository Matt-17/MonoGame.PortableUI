# MonoGame.PortableUI — Agent Guide

A WPF-inspired retained-tree UI library for MonoGame. Core library targets `net10.0;net10.0-android`.
No XAML — trees are built in C#. Known open issues and deferred work live in `docs/audit.md`.

## Repository layout

- `src/MonoGame.PortableUI` — the library (controls, layout, input, media/brushes, theming core).
- `src/MonoGame.PortableUI.Themes` — theme catalog add-on (`PortableThemes.All`, 37 themes, one file each under `Themes/`).
- `samples/MonoGame.PortableUI.Demo` — DesktopGL demo; `samples/MonoGame.PortableUI.Demo.Android` — Android host.
- `tests/MonoGame.PortableUI.Tests` — MSTest suite (headless, no graphics device needed for most tests).
- `benchmarks/` — BenchmarkDotNet. `docs/` — fonts, release process, historical issue log (`issues.md`), audit (`audit.md`).

## Build, test, run

```bash
dotnet tool restore                                  # MGCB/MGFXC local tools
dotnet test tests/MonoGame.PortableUI.Tests          # fastest verification loop
dotnet build MonoGame.PortableUI.slnx -c Release     # full solution — requires the Android workload!
dotnet run --project samples/MonoGame.PortableUI.Demo
dotnet run --project benchmarks/MonoGame.PortableUI.Benchmarks -c Release -- --filter *Layout*
```

- Building the `.slnx` includes `net10.0-android` inner builds and needs `dotnet workload install android`.
  **Prefer targeting individual projects** when iterating on desktop.
- Demo flags: `--theme <id>`, `--screenshot <dir>` (renders every theme to PNG and exits — the primary
  visual-verification loop), `--screenshot-screen <tab>`; env var `PORTABLEUI_DEMO_THEME`.
- Conventions: `Nullable=enable`, `ImplicitUsings=disable` (write full `using`s), central package versions
  (`Directory.Packages.props`), warnings-as-errors only on CI (`CI=true`).

## Architecture

**Class hierarchy:** `FrameworkElement` (Parent, BackgroundBrush, InvalidateLayout) → `UIElement`
(IsVisible/IsGone) → `Control` (the workhorse: sizing, margin/alignment, input events, theming,
animations, tooltips, context menu). Specializations: `Panel` → `Grid`/`StackPanel`/`SwipePresenter`;
`ContentControl` → `Button` (→ `ToggleButton` → `RadioButton`), `Border`, `CheckBox`, `ScrollViewer`,
`FlyOut`, `Badge`; `TextBlock` → `TextBox`; direct: `ListBox`, `DataGrid`, `Slider`, `ProgressBar`,
`ToggleSwitch`, `TabControl`, `ThemeIsland`. `ContextMenu`/`MenuItem`/`TabItem` are plain objects, not controls.

**Top level:** `ScreenEngine` owns navigation, focus, viewport scaling (`ReferenceSize` letter-boxing),
backdrop-blur and post-FX managers. `ScreenComponent` is the MonoGame `DrawableGameComponent` pumping
Update/Draw. Each `Screen` is a `FrameworkElement` hosting a private root `Grid` (`_mainGrid`).

**Layout contract (two-phase, WPF-like):**
- `MeasureLayout() : Size` — bottom-up desired size. `float.NaN` = Auto, `float.PositiveInfinity` =
  unbounded; test with `SizeEx.IsFixed`. Base `Control` returns fixed size (or 0) + constraints + Margin —
  the correct order is `ApplyConstraints(size) + Margin` (Min/Max exclude margin).
- `UpdateLayout(Rect)` — top-down arrange. Sets `BoundingRect` (margin box), `ClippingRect`
  (= BoundingRect − Margin, the content/hit-test box), `ClientRect`.
- Invalidation: `Control.InvalidateLayout(bool boundsChanged)` bubbles to `Screen`, which marks a dirty
  flag; **one** layout pass runs per frame (start of `Screen.Update`, safety-net flush before `Draw`).
  Explicit `control.UpdateLayout(rect)` is synchronous — tests rely on that. If library code must read
  a fresh `BoundingRect` right after mutating properties, flush via the screen's layout-if-dirty path
  rather than assuming setters laid out synchronously.

**Rendering:** immediate-mode traversal in `Screen.Draw`. Each visible control gets a `RenderContext`
(accumulated transform/opacity/scissor), `GraphicsDevice.ScissorRectangle` is set per control, and
`OnDraw` runs in its own `SpriteBatch.Begin/End`; `OnDrawOverlay` gets a second batch only for types
that override it. Offscreen passes: backdrop blur (glass brushes), post-FX (CRT/scanline/etc.), and the
letter-box scale target. Render targets are pooled (`RenderTargetHelper`) and recreated on device reset.

**Input:** `Screen.Update` polls `IInputSource` (mouse, touch, keyboard) and diffs against the previous
state. Routing is **bubbling only** (depth-first descendants, then self; `args.Handled` stops it); there
is no tunneling. Hit-testing uses `ClippingRect` — margins are inert. Focus lives in
`ScreenEngine.FocusedControl` (currently a process-global static — see audit A1); only controls with
`IsFocusable` take focus on left-mouse-down. Enter/Space activate the focused clickable control.
`ScreenSystem.TotalTime` is the global clock for animations, timers, caret blink, and double-click.

## Implementing a control

Override as needed: `MeasureLayout`/`UpdateLayout` (custom layout), `OnDraw`/`OnDrawOverlay` (visuals),
`GetDescendants` (children), `GetThemeStyle`/`GetThemeBackgroundBrush`/`OnThemeChanged` (theming),
`GetVisualState`/`ChangeVisualState` (state visuals), `CapturesInputBeforeDescendants` (claim input
before children, e.g. scrollbar), `ClipsDescendants`. Wire behavior to events (`Click`, `MouseDown`,
`TouchDown`, `KeyPressed`, …) in the constructor. Interactive drag behaviors must wire **both** mouse
and touch events.

**Theming pattern:** visual properties resolve live from the current theme's `ControlStyle` slot unless
explicitly set by the user. The constructor seeds snapshots from `PortableTheme.ResolveCurrent()`;
`OnThemeChanged(old, new)` re-seeds only values still reference-equal to the old theme's (so user
overrides survive theme switches). New themed controls get a `ControlStyle` slot in
`PortableTheme.FromPalette` with **palette-derived defaults** — never edit the 37 theme files for a new
slot. Theme resolution is cached per global `ThemeVersion`.

**Item controls** (`ListBox`, `DataGrid`, `TabControl`) materialize one child per item. The full
item→child sync runs in the layout pass (`MeasureLayout`/`UpdateLayout`); `GetDescendants()` (called
several times per frame) only rebuilds on a count mismatch. After editing items **in place** call
`Refresh()` — adds/removes are picked up on the next layout pass. `DataGrid` sorting reorders a
display-order index list (`DisplayedItems`), never the caller's `Items`. There is no virtualization
yet (audit P7).

## Pitfalls

- **Premultiplied alpha everywhere.** Brushes/masks draw with premultiplied colors
  (`Color * alpha`, not `new Color(r,g,b,a)`); `RoundedRectRenderer` expects premultiplied input.
- **Hot paths must not allocate.** No LINQ in `MeasureLayout`/`UpdateLayout`/`OnDraw`/per-frame update
  code; reuse buffers (see pressed-keys/render-target caching in `Screen`/`ScreenComponent`).
- **GPU resources need device-lifetime handling.** Any static `Texture2D` cache must register
  `DeviceReset`/`Disposing` cleanup and bound growth — follow `BrushTextureCache`.
- **Don't call `ScreenEngine.Instance` from rendering code** — derive the `GraphicsDevice` from the
  `SpriteBatch` at hand; surface engines are not `Instance`.
- **Android host:** pin `PreferredBackBufferWidth/Height` to `DisplayMetrics` — a mismatched fixed
  back-buffer breaks scissor-based text clipping. Screenshot via `adb shell screenrecord` (screencap
  doesn't capture the GL surface).
- **`Rect.Contains`** is inclusive on Left/Top, exclusive on Right/Bottom.
- **Verification loop:** run the test suite, then the demo `--screenshot` sweep and diff PNGs against a
  baseline before/after visual changes; run `*Layout*` benchmarks for layout-path changes.
