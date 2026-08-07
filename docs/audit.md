# Code Audit — July 2026

Full audit of the library covering bugs, performance, UI/UX consistency, architecture, and tech debt.
Each finding lists severity, location, and status: **fixed** (addressed in the audit-fix pass) or
**deferred** (documented here as future work). Line numbers refer to the state of the tree at audit
time and may drift as fixes land.

**Measured impact of the performance fixes** (BenchmarkDotNet, Ryzen 7 5700X, .NET 10):
`GridLayout500Controls` 410.8 µs → 309.2 µs and 146,336 B → 536 B allocated per pass;
`ScrollListLayout500Controls` 23.3 µs → 17.4 µs, allocation-free. Invalidation batching additionally
coalesces N property changes per frame into one layout pass (asserted by
`AuditFixRegressionTests.Property_changes_coalesce_into_one_layout_pass_per_update`).

## Bugs

| # | Sev | Finding | Location | Status |
|---|-----|---------|----------|--------|
| B1 | med | Clicking an already-checked `RadioButton` toggles it off and back on via `SetGroupChecked`, firing spurious `Checked(false)` → `Checked(true)` events. | `RadioButton.cs`, `ToggleButton.cs:77` | fixed |
| B2 | high | No keyboard activation: focused `Button`/`CheckBox`/`ToggleButton`/`ToggleSwitch` ignore Enter/Space; only TextBox/ListBox/DataGrid/Slider handle keys. | `Button.cs`, `CheckBox.cs`, `ToggleSwitch.cs` | fixed |
| B3 | med | Focus is stolen by *any* mouse-down on *any* control — including right-click and non-interactive `TextBlock`/`Image` — hiding the soft keyboard and blurring an active TextBox. No focusable concept existed. | `Control.cs:772` | fixed (`IsFocusable`) |
| B4 | med | Margin area is hit-testable: all input routing used `BoundingRect` (the margin box) instead of the content box. | `Screen.cs:722/749/1020`, `ListBox.cs:425` | fixed (routes on `ClippingRect`) |
| B5 | med | Touch cannot operate drag interactions: `Slider`, `ScrollViewer` thumb, `DataGrid` header sort/resize were mouse-only. (`ListBox` drag-select stays mouse-only on purpose: on touch, dragging must pan the list; taps select/invoke.) | `Slider.cs:31`, `ScrollViewer.cs:62`, `DataGridHeader.cs:35` | fixed |
| B6 | med | `DataGrid.ApplySort` mutated the caller's `Items` list in place (`Clear`+`AddRange`), silently reordering user data. | `DataGrid.cs:230` | fixed (display-order indirection) |
| B7 | med | Animations/timers of the underlying screen freeze while a FlyOut/ContextMenu is open — only the flyout subtree was ticked. | `Screen.cs:663-680` | fixed |
| B8 | med | Keyboard state was read from the global `Keyboard.GetState()` instead of `IInputSource`, breaking test injection and multi-surface scenarios. | `Screen.cs:881/697` | fixed |
| B9 | low | `Rect.Contains` was exclusive on Left/Top and inclusive on Right/Bottom — a pointer exactly on the left/top edge missed, and adjacent controls could double-hit on shared edges. | `Rect.cs:155` | fixed (inclusive L/T, exclusive R/B) |
| B10 | low | `SnapshotPressedMouseButtons` handed the reused scratch list into `MouseEventArgs`; handlers retaining args saw `Buttons` mutate on later frames. | `Screen.cs:861` | fixed |
| B11 | low | `FlyOut` always anchored *upward* by its own height; `ComboBox` compensated with `bounds.Bottom + targetHeight`. Any other caller got misplaced content. | `FlyOut.cs:41`, `ComboBox.cs:135` | fixed (explicit anchor placement) |
| B12 | high | `RoundedRectRenderer` static texture caches were never disposed, had no `DeviceReset`/`Disposing` handlers, and used `ScreenEngine.Instance!` instead of the drawing device — dangling textures after device reset, NRE risk for surface engines. | `RoundedRectRenderer.cs:11-13` | fixed (mirrors `BrushTextureCache`) |
| B13 | med | Size-keyed mask caches grow without bound: every distinct rounded-rect size allocates and caches a full W×H texture forever (animated/resizing controls leak). Same for `GradientBrush` rounded path. | `RoundedRectRenderer.cs`, `GradientBrush.cs:122` | fixed (cap + clear-on-overflow) |
| B14 | med | `SolidColorBrush.Pixel` used `ScreenEngine.Instance!` — NRE before engine init and wrong device for surface engines. | `SolidColorBrush.cs:17` | fixed |

## Performance

| # | Sev | Finding | Location | Status |
|---|-----|---------|----------|--------|
| P1 | high | No invalidation batching: `Screen.InvalidateLayout` ignored `boundsChanged` and synchronously re-laid-out the whole tree on **every** property setter; one frame could run many full passes. | `Screen.cs:129` | fixed (dirty flag + one deferred pass per frame) |
| P2 | high | `ListBox.EnsureItemButtons`/`DataGrid.EnsureRows` ran inside `GetDescendants()`, which is called several times per frame (draw + input walks) — O(n) rebuild work each call. | `ListBox.cs:211`, `DataGrid.cs:324` | fixed (items-version gate) |
| P3 | med | Grid track sizing used LINQ (`Take/Skip/Sum`, anonymous types) per child per pass. | `Grid.cs:84-132, 229-258, 378` | fixed (precomputed offset arrays) |
| P4 | med | `StackPanel.MeasureLayout` measured every child **twice** (LINQ `Max` + `Sum`). | `StackPanel.cs:19-28` | fixed (single pass) |
| P5 | med | Two `SpriteBatch.Begin/End` pairs per control per frame (`OnDraw` + `OnDrawOverlay`), even when `OnDrawOverlay` is not overridden. | `Screen.cs:457-488` | fixed (overlay pass skipped when not overridden) |
| P6 | low | Per-frame allocations: `Keyboard.GetPressedKeys()` array + LINQ `Contains`, `GetRenderTargets()` arrays, `Where().ToList()` in `OnMouseEnter`, per-move delegate captures. | `Screen.cs`, `ScreenComponent.cs:65`, `Control.cs:749` | fixed |
| P7 | high | No virtualization in `ListBox`/`DataGrid` (one child control per item, acknowledged in `DataGrid`'s own doc comment). | `ListBox.cs`, `DataGrid.cs:19` | **deferred** — feature-sized work |
| P8 | low | `Control.Screen` walks the parent chain on every access; read frequently (tooltips, capture, theme). | `Control.cs:91` | **deferred** — needs an invalidation story for reparenting |

## UI/UX consistency

| # | Sev | Finding | Location | Status |
|---|-----|---------|----------|--------|
| U1 | med | `ToggleSwitch` and `Badge` hardcode colors and ignore the theme; `DataGrid` has no `ControlStyle` slot, so grids look off-theme in most of the 37 shipped themes. | `ToggleSwitch.cs:28`, `Badge.cs:20`, `PortableTheme.cs:105` | fixed (palette-derived slots; no theme-file edits needed) |
| U2 | med | `CheckBox` box, `Slider` track/fill/thumb border, and `ProgressBar` fill draw square chrome that ignores `CornerRadius`, unlike base `Control`. | `CheckBox.cs:158`, `Slider.cs:138`, `ProgressBar.cs:104` | fixed |
| U3 | med | Constraint-vs-margin order differs: base `Control` does `ApplyConstraints(size) + Margin`, but `Slider`/`ProgressBar`/`ToggleSwitch`/`Image` did `ApplyConstraints(size + Margin)` — Min/Max included the margin for those controls only. | `Slider.cs:132`, `ProgressBar.cs:92`, `ToggleSwitch.cs:67`, `Image.cs` | fixed |
| U4 | low | Missing events: `ProgressBar` fired nothing on `Value` change; `TabControl` had no selection event; `SelectionChanged` args carried no old/new index. | `ProgressBar.cs`, `TabControl.cs` | fixed (additive) |
| U5 | low | `ContextMenu` opens on long-touch for every `ContextMenuType` — verified against the enum docs: intended (`OpenOnLeftClick` is documented as "in addition to long press"; the type governs dismissal and extra mouse triggers, not the touch gesture). | `Control.cs:529` | no change needed |
| U6 | low | Invoke semantics differ by design: `ListBox` invokes on single click, `DataGrid` rows on double click. Flagged, not changed — may be intentional. | `ListBox.cs:289`, `DataGridRow.cs:25` | **deferred** — product decision |
| U7 | low | `Border` exposes `BorderColor`/`BorderWidth` — verified: they are thin aliases delegating to the base `BorderBrush`/`BorderThickness`, kept for API compatibility. Cosmetic only. | `Border.cs` | no change needed |

## Architecture

| # | Sev | Finding | Location | Status |
|---|-----|---------|----------|--------|
| A1 | med | Global mutable statics tie everything to one primary engine: `ScreenEngine.Instance`, static `FocusedControl`, `ScaleFactor`, `ScreenSystem.TotalTime`, RadioButton group registry. Defeats the existing multi-surface design (`CreateSurfaceEngine`) and hurts testability. | `ScreenEngine.cs:14/57` | **deferred** — needs its own design pass |
| A2 | med | `Screen` is a god class (~1350 lines): input routing, drag & drop, tooltips, flyouts, popup clamping, render orchestration, keyboard show/hide. A `Renderer` and `InputRouter` extraction would sharply reduce coupling. | `Screen.cs` | **deferred** — architecture rewrite |
| A3 | med | Platform code in the core assembly via `#if ANDROID` (`AndroidClipboardService`, `TextInput` wiring). The `IClipboardService` abstraction is right; concrete impls belong in platform heads. | `AndroidClipboardService.cs`, `ScreenEngine.cs:29` | **deferred** — packaging change |
| A4 | low | Full sibling-draw batching is prevented by the scissor-per-control design; a real batching renderer is a rewrite. | `Screen.cs:457` | **deferred** |

## Tech debt

| # | Sev | Finding | Location | Status |
|---|-----|---------|----------|--------|
| T1 | med | Three near-identical private `DrawBorder` copies. | `Control.cs:1076`, `CheckBox.cs:193`, `Slider.cs:258` | fixed (shared helper) |
| T2 | med | No shared `ItemsControl`/`Selector` base: `ListBox`/`ComboBox`/`DataGrid` each reimplement Items/SelectedIndex/SelectionChanged/key handling. `Padding` declared separately on `Panel`, `ContentControl`, `TextBox`. | various | **deferred** — breaking API churn |
| T3 | low | Dead code: no-op `ScrollViewer.OnDraw`/`TabControl.OnDraw` overrides, unused `lineCount` param, empty `ContentPresenter`/`TextButton` placeholders. (`Screen.RouteCapturedMouseUp` looked dead but is a test entry point — kept, documented.) | various | fixed (removed; placeholders kept as public API) |
| T4 | low | Orphaned root folders with only stale obj/ output: `MonoGame.PortableUI.Tests/`, `SampleClient.Android/`, `SampleClient.Windows/`. | repo root | fixed (deleted) |
| T5 | low | CI packed only the core package (Themes validated first at release time); installed an unused .NET 8 SDK. | `.github/workflows/ci.yml` | fixed |
| T6 | low | Benchmark named after removed `ThemeRegistry` type. | `PortableUiBenchmarks.cs:90` | fixed |
| T7 | low | Self-flagged hack: `ScreenEngine.CurrentKeyboard`/`RequestKeyboard`/`HideKeyboard` public "for a small hack". | `ScreenEngine.cs:17/174/186` | **deferred** — API surface decision |
