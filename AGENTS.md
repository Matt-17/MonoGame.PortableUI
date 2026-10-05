# MonoGame.PortableUI — Agent Guide

A WPF-inspired retained-tree UI library for MonoGame. Core library targets `net10.0;net10.0-android`.
No XAML — trees are built in C#. Known open issues and deferred work live in `docs/audit.md`.

## Repository layout

- `src/MonoGame.PortableUI` — the library (controls, layout, input, media/brushes, theming core, `FontStashUIFont` runtime fonts via FontStashSharp — in the core until MonoGame's own font system lands).
- `src/MonoGame.PortableUI.Themes` — theme catalog add-on, NuGet ID `CodeIX.PortableUI.Themes` because the `MonoGame.` prefix is reserved on NuGet (`PortableThemes.All`, 42 themes incl. 5 game UIs, one self-contained file each under `Themes/`; `ThemeBuilder` lives in the core so a theme file can be copied alone — see `Themes/README.md`).
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
- Measure children through `child.Measure()`, not `child.MeasureLayout()`: it caches the result per
  layout pass (a pass starts when a root — parent not a `Control` — is arranged) and
  `InvalidateLayout(true)` drops the cache along the bubble path. Calling `MeasureLayout` directly
  re-measures the whole subtree once per ancestor.
- `UpdateLayout(Rect)` — top-down arrange. Sets `BoundingRect` (margin box), `ClippingRect`
  (= BoundingRect − Margin, the content/hit-test box), `ClientRect`.
- Invalidation: `Control.InvalidateLayout(bool boundsChanged)` bubbles to `Screen`, which marks a dirty
  flag; **one** layout pass runs per frame (start of `Screen.Update`, safety-net flush before `Draw`).
  Explicit `control.UpdateLayout(rect)` is synchronous — tests rely on that. If library code must read
  a fresh `BoundingRect` right after mutating properties, flush via the screen's layout-if-dirty path
  rather than assuming setters laid out synchronously. `InvalidateLayout(false)` (visual-only change)
  does **not** schedule a pass — use it only for state read at draw time.
- Scrolling shifts the arranged content (`OffsetArrangement`) instead of re-arranging it, as long as
  nothing inside invalidated; positions must therefore live only in `BoundingRect`/`ClippingRect`.

**Rendering:** immediate-mode traversal in `Screen.Draw`. Each visible control gets a `RenderContext`
(accumulated transform/opacity/scissor), `GraphicsDevice.ScissorRectangle` is set per control, and
`OnDraw` runs in its own `SpriteBatch.Begin/End` (skipped for controls without an `OnDraw` override,
background, border or shadow); `OnDrawOverlay` gets a second batch only for types that override it.
Translucent rounded fills use a 9-slice of a per-radius mask (`RoundedRectRenderer`), never a
per-size texture. Offscreen passes: backdrop blur (glass brushes), post-FX (CRT/scanline/etc.), and the
letter-box scale target. Render targets are pooled (`RenderTargetHelper`) and recreated on device reset.

**Render on demand:** `ScreenEngineOptions.RenderMode = OnDemand` (the Android demo uses it) draws only
when a frame was requested and otherwise calls `Game.SuppressDraw()` and sleeps out
`IdleUpdateInterval` in one wait that `ScreenEngine.WakeUp()` (input bridge, cross-thread `RequestRedraw`,
`InvokeOnGameThread`) or the next `RequestRedrawAt` cuts short. Requests come from
`Screen.InvalidateLayout` (any property change), input activity (+250 ms grace), running animations,
transitions, `InvokeOnGameThread`, insets/viewport changes. A visual that changes with time **only at
draw time** must ask for its next frame from `OnDraw`: `ScreenEngine.RequestAnimationFrame()` (or
`RequestAnimationFrameAt(time)` like the caret) — otherwise it freezes in OnDemand mode. State flipped by
time in update code calls `Control.RequestRedraw()`. Check `FramesDrawn`/`FramesSkipped`, and on Android
`dumpsys SurfaceFlinger --latency` must show 0 frames on an idle screen.
`UISurface` consumes the same bookkeeping itself (any `RenderMode`): `NeedsRedraw`/`NextRedrawDue` peek,
`DrawIfNeeded`/`DrawIfNeededTo` consume; `DrawTo` renders into a host's atlas tile through a scratch target
shared per device and pixel size (every offscreen pass assumes a full target of the surface's size, so never
draw a surface with a viewport offset). `PostEffectMode` (All/ThemeOnly/None) is the host's switch over theme
and display effects. Surface engines (`IsSurfaceEngine`) keep no offscreen targets between draws: post-FX/island
targets, clip layers and the glass stack backdrop are rented from `RenderTargetPool` (per device and size) and
returned at the end of `DrawStack`; only layer caches stay per engine. `--benchmark-surfaces` measures it.

**Layer cache:** `Control.CacheMode = CacheMode.Bitmap` draws the control's subtree into a render target
and composites one quad while it stays valid (scrolling only moves it; consecutive composites share a
batch). Validity rests on the **visual invalidation contract**: anything read at draw time must, when it
changes, call `InvalidateLayout(false)` (or `RequestRedraw()`, which bubbles the same way) so cached
ancestors go dirty - property setters, `HoverState`/`TouchState`, focus, `Opacity`/`Scale`/`Translation`,
`ScrollViewer.Offset` already do. Draw-time animation (`RequestAnimationFrame[At]`) during a layer
render marks it dirty/refreshes it; layers re-rendered in three consecutive frames (for any reason,
e.g. a list scrolling itself) are drawn live for 30 frames, doubling up to 240 while they keep changing.
Safety net: a press/release, key or text input and `InvokeOnGameThread` re-render every layer once, so
property changes in handlers show even where a setter does not invalidate - per-frame changes from
game code still must call `RequestRedraw()`. Layer render targets must use `PreserveContents`: nested
layers and clip passes switch targets mid-render. `ListBox.ItemCacheMode` caches rows. Global changes bump `ScreenEngine.LayerCacheGeneration`. Subtrees with backdrop (glass) brushes,
post-FX islands or overscroll stretch, and transient scales, are drawn live. Unused caches are freed
after 120 frames; budget three screens of pixels.

**Render quality:** `ScreenEngineOptions.RenderQuality` (Auto = Low in Android battery saver, else High)
resolves to `ScreenEngine.EffectiveRenderQuality` each update. Drawing code reads it through the static
`ScreenEngine.DrawingQuality` / `AnimatesDecorations` (the engine currently drawing). New expensive or
perpetually animated visuals must honour it: skip at Low (blur, post FX, heavy overdraw), freeze below
High (anything that would request a frame forever). Never encode quality in theme files.

**Input:** `Screen.Update` polls `IInputSource` (mouse, touch, keyboard) and diffs against the previous
state. Routing is **bubbling** (depth-first descendants, then self; `args.Handled` stops it); siblings are
visited **topmost first** (reverse `GetDescendants` order = reverse draw order). The only tunneling hook is
`OnPreviewTouchDown`, which lets containers like `ScrollViewer` start a pan under a clickable child; a pan
past the threshold cancels the child's pending click. Hit-testing uses `ClippingRect` — margins are inert. Focus lives in
the owning engine's `FocusedControl` (per engine, so several surfaces/players keep separate focus; resolve it with `ScreenEngine.For(control)`); only controls with
`IsFocusable` take focus on left-mouse-down. Enter/Space activate the focused clickable control.
`ScreenSystem.TotalTime` is the global clock for animations, timers, caret blink, and double-click.
Keyboard: every key (F1-F12, Alt/Ctrl chords) is raised as `KeyDown`/`KeyUp` (`KeyEventArgs.Key`, `IsRepeat`,
`Handled`) on the focused control, bubbling through its parents to `Screen.KeyDown`; only unclaimed keys get the
screen's meaning (debug overlay key, Tab, Escape, arrow navigation, `KeyboardCommand` editing commands via
`KeyPressed`). `ScreenEngineOptions.DebugOverlayKey` (F3, null on surfaces). Text arrives separately as chars.
Arrow keys and the gamepad (`IInputSource.GamePad`: D-pad/stick, A, B, Y, shoulders) are screen-level:
a direction the focused control does not claim via `HandlesDirection` moves focus spatially
(`Screen.MoveFocus`) and scrolls it into view; Escape/B close the open popup or raise
`Screen.BackRequested`. Popups opened from keyboard/gamepad take focus and return it on close.
Navigation: `NavigateToScreen` / `PushOverlay` (screens below stay drawn, frozen) with `ScreenTransition`;
`OnNavigatedTo`/`OnNavigatedFrom` hooks; focus is restored on `NavigateBack`.

**App-wide services** (all on `ScreenEngine` unless noted; screens pick changes up on their next update via
version counters, never by rebuilding): `TextScaling` (static, text size factor), `Localizer.Default`
(static, string catalogues; bind with `control.Localize(...)`), `FontManager.DefaultDynamicFont` (UIFont
backend; a theme's `Typography.DynamicFont` overrides it per screen/surface/island), `SafeAreaInsets` (+ `SafeAreaPanel`), `OnScreenKeyboard`, `AccessibilityBridge`, `Toasts`,
`ShowModal`. Platform hooks for Android live in the core's `#if ANDROID` files (`AndroidWindowInsets`,
`AndroidOnScreenKeyboard`, `AndroidTextScaling`, `AndroidAccessibilityBridge`); they are compiled but not
device-tested. There is no iOS head. Platform callbacks must go through `InvokeOnGameThread`.
Desktop window text input reaches exactly one engine per `Game`: the `UISurface` with
`HasKeyboardFocus` (set directly or via `SurfaceFocusManager`), otherwise the regular engine(s);
surface engines never type on their own. A display can be a text-mode screen (`TextGrid`): the
pointer is snapped to character cells after the display curvature is undone (`Screen.TransformPointerPosition`). `SurfaceFocusManager.RouteTextInput` is only for platforms
without window text input.

## Implementing a control

Override as needed: `MeasureLayout`/`UpdateLayout` (custom layout), `OnDraw`/`OnDrawOverlay` (visuals),
`GetDescendants` **and** `VisualChildCount`/`GetVisualChild(i)` (children — the per-frame walks use the
indexed pair; a control overriding only `GetDescendants` falls back to a materialized list), `GetThemeStyle`/`GetThemeBackgroundBrush`/`OnThemeChanged` (theming),
`GetVisualState`/`ChangeVisualState` (state visuals), `CapturesInputBeforeDescendants` (claim input
before children, e.g. scrollbar), `ClipsDescendants`. Wire behavior to events (`Click`, `MouseDown`,
`TouchDown`, `KeyPressed`, …) in the constructor. Interactive drag behaviors must wire **both** mouse
and touch events.

**Theming pattern:** visual properties resolve live from the current theme's `ControlStyle` slot unless
explicitly set by the user. The constructor seeds snapshots from `PortableTheme.ResolveCurrent()`;
`OnThemeChanged(old, new)` re-seeds only values still reference-equal to the old theme's (so user
overrides survive theme switches). New themed controls get a `ControlStyle` slot in
`PortableTheme.FromPalette` with **palette-derived defaults** — never edit the 42 theme files for a new
slot. Theme resolution is cached per global `ThemeVersion`.

**Buttons and chrome brushes:** `Button.Variant` (Primary/Secondary/Danger) resolves the theme's
`PrimaryButton`/`SecondaryButton`/`DangerButton` slots — use variants instead of setting
`BackgroundBrush`, or the theme's look is lost. Theme-level shapes: `BevelBrush` (Win9x),
`FrameBrush` (concentric rings, pixel notches), `ChamferBrush` (cut corners, accent bar),
`ShadowStyle.Also` (chained shadows, e.g. neumorphism). Brushes get `BrushContext.Scale` for HiDPI line widths.

**Item controls**: `TabControl` materializes one child per item; `ListBox` and `DataGrid` virtualize by
default (`IsVirtualizing`) — the internal `VirtualItemsPanel` realizes only the rows in view and recycles
them on scroll (it overrides `OffsetArrangement`, which scrolling calls instead of a layout pass), with
one shared row height. Rows carry their item/display index in `Tag`/`Index`; never assume row position
= item index. The full item→child sync runs in the layout pass (`MeasureLayout`/`UpdateLayout`); `GetDescendants()` (called
several times per frame) only rebuilds on a count mismatch. After editing items **in place** call
`Refresh()` — adds/removes are picked up on the next layout pass. `DataGrid` sorting reorders a
display-order index list (`DisplayedItems`), never the caller's `Items`.

## Pitfalls

- **Premultiplied alpha everywhere.** Brushes/masks draw with premultiplied colors
  (`Color * alpha`, not `new Color(r,g,b,a)`); `RoundedRectRenderer` expects premultiplied input.
- **Hot paths must not allocate.** No LINQ in `MeasureLayout`/`UpdateLayout`/`OnDraw`/per-frame update
  code; reuse buffers (see pressed-keys/render-target caching in `Screen`/`ScreenComponent`).
- **GPU resources need device-lifetime handling.** Any static `Texture2D` cache must register
  `DeviceReset`/`Disposing` cleanup and bound growth — follow `BrushTextureCache`.
- **Don't call `ScreenEngine.Instance` from rendering code** — derive the `GraphicsDevice` from the
  `SpriteBatch` at hand; surface engines are not `Instance`.
- **Android host:** the back buffer must equal the game view's real size — call
  `AndroidSurfaceSize.Follow(view, graphics, engine)` in `Initialize`. MonoGame letter-boxes a
  differently shaped preferred size into the view with a negative viewport offset, so drawing shifts
  while scissor rects do not and text disappears (this is what broke edge-to-edge). Edge to edge works
  with it (`SetDecorFitsSystemWindows(false)`, cutout `ShortEdges`, transparent bars) plus a
  `SafeAreaPanel`; use `ScreenEngineOptions.LayoutScale = density` for dp layout. Screenshot via
  `adb shell screenrecord` (screencap doesn't capture the GL surface).
- **`Rect.Contains`** is inclusive on Left/Top, exclusive on Right/Bottom.
- **Android game loop off the UI thread:** with MonoGame's default `RenderOnUIThread = true` every loop
  iteration is marshalled to the UI thread (~1.8 ms CPU each) and an idle sleep blocks the UI looper, so
  idle costs ~7 % CPU. The demo sets `RenderOnUIThread = false` + `AndroidInputBridge.Attach(view,
  engine)` + a 250 ms idle interval: ~0.5 %. In that mode MonoGame's `TouchPanel`/`Keyboard` are unsafe
  (unsynchronized, touch listener set from the wrong thread) - PortableUI reads the bridge instead - and
  any view access from game code must go through `AndroidUiThread.Run`/`view.Post`.
- **Status bar after resume:** MonoGame's Android presentation parameters report `IsFullScreen = true`
  even for windowed games and `AndroidGameActivity.OnResume` turns that into `FLAG_FULLSCREEN`, which it
  never clears. `AndroidSurfaceSize.Follow` writes the window's real state into the parameters on pause
  (clearing the flag afterwards would make Android re-show bars over an app's immersive mode). Apps
  with their own immersive mode re-apply it in `OnResume`, posted (see the demo's `MainActivity`).
- **Frame pacing:** a fixed time step (MonoGame's default, 60 Hz) on a 90/120 Hz panel shows some
  refreshes twice - visible judder while scrolling. App-style hosts set `IsFixedTimeStep = false` with
  `SynchronizeWithVerticalRetrace = true` so frames follow vsync (the Android demo: 90 fps on a Pixel 5,
  every frame one refresh apart, and less CPU than the fixed step's 1 ms sleep spin). Animations and
  fling already use real elapsed time. Check pacing with `dumpsys SurfaceFlinger --latency` intervals.
- **Verification loop:** run the test suite, then the demo `--screenshot` sweep and diff PNGs against a
  baseline before/after visual changes; run `*Layout*` benchmarks for layout-path changes.
