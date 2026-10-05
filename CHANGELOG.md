# Changelog

## Unreleased

- **Fix (#91): readable selected text.** Under an opaque selection highlight a `TextBox` drew selected text in its normal colour (dark text on a navy Windows 3.1 highlight, ink on the Game Boy theme's dark green). It now inverts: selected glyphs take the box's background colour (a solid fill or a frame/bevel face) when that reads on the highlight, otherwise black or white. Translucent highlights (most themes) keep the text colour. New theme slot `TextBoxSelectionTextColor` and `TextBox.SelectionTextColor` set it explicitly. Glyphs stay pixel-identical in position to an undivided line.

- **Distant screens render smaller (#99):** `UISurface.DrawTo`/`DrawIfNeededTo` render a tile smaller than the surface at a lower resolution instead of rendering full size and shrinking the copy: the smallest step of `UISurface.DrawToResolutionSteps` (1, ¾, ½, ⅜, ¼, ³⁄₁₆, ⅛ of the pixel size) that still covers the tile. Draw-only: no relayout, pointer mapping unchanged, layer caches re-render at the new scale. The copy shrinks by at most a third, so distant text no longer shimmers; the few steps keep scratch targets and glyph sizes bounded. `LastDrawResolution` reports the step used. A full-size tile stays pixel-identical to `Draw`.
- **No GPU memory per surface (#100):** surface engines rent their post-FX UI and effect-island targets, clip-shape layers and the nested-glass backdrop from a pool shared per device and size for the duration of a draw, instead of keeping their own; the main engine keeps its own. With display effects on, a 640×400 surface held a 1000 KB render target each; now 100 surfaces share one.
- **Surface benchmark:** demo flag `--benchmark-surfaces out.csv [--surface-count 100]` hosts N text-mode editor stations drawn into an atlas with `DrawIfNeededTo`. Measured on a desktop GPU (100 surfaces, 640×400): 0 GPU resources and 21.5 KB managed memory per surface (12.5 KB when surfaces share one theme instance - create a theme once per look, not per surface), ~1 µs idle update per surface, 12 of 100 surfaces redrawn per idle frame (their blinking carets), ~8 ms to redraw all 100 flat and ~16 ms with display effects, nothing retained after `Dispose`. Redrawing is CPU-bound there, so half-size tiles mainly save GPU fill and atlas space.

## 0.3.0-alpha.6

Hosting many in-world screens: surfaces draw only when they changed, can render flat for a host's own display shader, and can draw straight into an atlas tile.

- **On-demand surfaces (#93):** `UISurface.NeedsRedraw` (peek, consumes nothing), `NextRedrawDue` (on the `ScreenSystem.TotalTime` clock: `Zero` = now, `MaxValue` = idle, otherwise e.g. the next caret flip), `DrawIfNeeded(gameTime)` (draws only when needed, the target keeps the last picture), `Invalidate()`, `FramesDrawn`/`FramesSkipped`. Changes made while a surface is not drawn stay pending: skipping draws never loses one. Device resets (and, on Android, resuming) invalidate the frame; on desktop, window activation does not redraw anything. Give every surface its own `InputSource` (`NullInputSource.Instance` while unfocused): the default is the real mouse and keyboard, and their activity keeps a surface drawing for 250 ms. Update all surfaces with the same monotonic `GameTime`: `ScreenSystem.TotalTime` is one clock.
- **Host post-effect switch (#97):** `ScreenEngineOptions.PostEffectMode` / `UISurface.PostEffectMode`: `All` (default), `ThemeOnly` (theme look without the display stage), `None` (flat: no extra render target, no full-screen pass, no effect islands, pointer not mapped through a barrel). It wins over the theme's and the display's effects, so a host whose 3D display shader does curvature, scanlines and glass renders station UIs flat and maps the pointer through its own curve.
- **Draw into an atlas tile (#94):** `UISurface.DrawTo(target, destination, gameTime)` / `DrawIfNeededTo(...)` render the surface into a rectangle of a host's render target (which must use `PreserveContents`), replacing the tile's contents. The surface then owns no target of its own: it renders into a scratch target shared by all surfaces of the same pixel size and copies it into the tile, scaled to the tile's size. Pixel-identical to `Draw` at 1:1. Scaling down saves atlas space, not rendering: the UI is rendered at full size and copied with linear filtering without mipmaps, so text shimmers below half size. For distant screens lower `LayoutScale` (one relayout per change) to render at about the tile's size. A host that writes into a tile itself calls `Invalidate()`.
- **Cheaper surfaces (#95):** surface engines create no `ScreenComponent` and subscribe to the window's text input only while they own the keyboard; surfaces share their `SpriteBatch` per device. `UISurface.PostProcessManager` is obsolete (it was never used and is now always null).
- **Fix:** radio groups held their buttons strongly, keeping every discarded screen or surface with a `RadioButton` alive; members are now weak.
- **Fix:** creating any engine (each `UISurface` creates one) reset the static `ScreenEngine.ScaleFactor` to 1.

## 0.3.0-alpha.5

- **Vertical scanlines:** `ScanlinePostEffect.Orientation = ScanlineOrientation.Vertical` draws the lines top to bottom, for displays whose tube is mounted on its side (3:4 portrait arcade monitors). Works in the PostFx shader and the shader-free overlay path; default stays horizontal.

## 0.3.0-alpha.4

- **Text-mode displays (#87):** `TextGrid` (`ScreenEngineOptions.TextGrid`, `UISurface.TextGrid`, e.g. `TextGrid.Dos` 80×25, `TextGrid.C64` 40×25) makes a display a character-cell screen: the pointer snaps to the centre of its cell for drawing and input — after the display's curvature is undone, so clicks near a curved edge hit the visible cell. Pointer capture keeps fractional motion, so slow moves still cross cells. Per engine/surface. Cell size = layout size / grid (recommended 640×400 or 720×400 for 80×25, 320×200 for 40×25, stretched to 4:3).
- **Text-mode pointer:** `CursorStyle.TextCell()` inverts the cell under the pointer (the DOS mouse), `CursorStyle.TextBlock(color)` fills it; default for the terminal-era themes (DOS, Norton, Terminal), phosphor-coloured blocks for Amber/Phosphor, a light-blue block for C64. `ScreenEngineOptions.TextCellCursorRenderer` lets a host with its own character buffer draw the exact attribute-swapped cell.
- **Text-mode caret:** `CaretStyle.TextMode` — a two-scanline underline in insert mode, a full block in overwrite mode (Insert key toggles `TextBox.IsOverwriteMode`), blinking at ~3.75 Hz (`TextBoxCaretBlinkInterval`); separate from the pointer. Used by the text-mode themes.
- The world-space demo monitor runs as an 80×25 (C64: 40×25) text-mode screen when it shows a text-mode theme.

## 0.3.0-alpha.3

Themes, glass and in-world screens: 42 themes (five new game UIs), button variants and chrome brushes, a real liquid glass shader with nested glass for dialogs, themed software cursors, display effects and pointer capture for in-world monitors, single-owner text input for surfaces. The theme catalog is now published as `CodeIX.PortableUI.Themes`; FontStashSharp runtime fonts are part of the core.

- **Curvature belongs to the display (#74, #75):** `CrtBarrelPostEffect` only applies as a display effect (`ScreenEngineOptions.PostEffects` / `UISurface.PostEffects`); in a theme's or ThemeIsland's effect list it is ignored. One source for the curve means the screen, `UISurface.IsPointOnDisplay`, pointer capture and Low render quality can no longer disagree about it.
- **Fix (#63):** window text input reaches exactly one engine per game — the `UISurface` with `HasKeyboardFocus` (now settable; set directly or via `SurfaceFocusManager`), otherwise the main UI. Previously every surface engine typed every character into its own focused field, and `SurfaceFocusManager.RouteTextInput` typed it a second time (now a no-op on desktop). A disposed surface hands the keyboard back.
- **Packages:** the theme catalog is published as **`CodeIX.PortableUI.Themes`** (namespace unchanged: `MonoGame.PortableUI.Themes`) — the `MonoGame.` prefix is reserved on NuGet, so the `MonoGame.PortableUI.Themes` and `MonoGame.PortableUI.FontStashSharp` IDs of 0.3.0-alpha.2 were never published. `FontStashUIFont` moved into the core (namespace `MonoGame.PortableUI.Text`; the core now depends on the pure-managed FontStashSharp.MonoGame) until MonoGame's own font system can back `UIFont`. The release workflow now fails on a missing or unpublished package instead of reporting success.

- **Render on demand:** `ScreenEngineOptions.RenderMode = RenderMode.OnDemand` draws only when the UI changed (input, property changes, animations, transitions, caret blink, spinners) and idles the game loop otherwise. On a Pixel 5 the idle Android demo went from 60 fps and ~50 % of a core to 0 fps and ~8 %. Custom time-driven visuals call `ScreenEngine.RequestAnimationFrame()` while drawing; `Control.RequestRedraw()`/`ScreenEngine.RequestRedraw()` cover other changes. MonoGame upgraded to 3.8.5.1.
- **Clip shapes:** inside clips (corner-radius clipping, e.g. a rounded ListBox) render into control-sized offscreen layers instead of full-screen ones: about a third of the GPU fill and memory for the demo's list, pixel-identical output (all 43 theme screenshots unchanged).
- **Fix (Android):** the status bar no longer disappears after returning to the app. MonoGame applied its (wrong) full-screen default as `FLAG_FULLSCREEN` on every resume; `AndroidSurfaceSize.Follow` now keeps the window's real state. The demo re-applies its own immersive mode after a resume.
- **Android demo at display rate:** the game loop follows vsync (`IsFixedTimeStep = false`) instead of a fixed 60 Hz step: scrolling runs at 90 fps on a Pixel 5 with every frame exactly one refresh apart (60 fps on a 90 Hz panel judders), at ~24 % instead of ~30 % CPU; idle stays at 0 frames.
- **Layer cache follow-up:** `ListBox.ItemCacheMode` keeps rows in textures (list scrolling re-renders only rows coming into view; ~17 % less draw time in the demo). Layers that change every frame are drawn live with a growing back-off. A press, release, key or text input re-renders all layers once, so handler-made changes always show; `BackgroundBrush`, `BorderBrush`, `BorderThickness`, `CornerRadius`, `Shadow` and bevel colours now invalidate. **Fix:** cached layers inside clip passes or other layers went black on Android (render targets now preserve their contents).
- **Layer cache:** `Control.CacheMode = CacheMode.Bitmap` keeps a control's subtree in a texture while nothing inside changes, so scrolling moves textures instead of redrawing text and chrome. Visual state changes (hover, pressed, focus, opacity/scale/translation, scroll offset) now invalidate like property changes, which also closes redraw gaps in on-demand rendering. In the Android demo (page blocks cached) scrolling draws in ~1.9 ms instead of ~3.0 ms per frame with 7 instead of 39 batches, ~20 % less CPU on a Pixel 5.
- **Fix:** after resuming on Android the on-demand loop draws ten frames instead of one, so the dimmed task snapshot no longer stays on screen until the first touch.
- **Android APK size:** the demo's Release APK went from 12.8 MB to 4.4 MB (arm64 only, no native HTTP handler, trimmed FontStashSharp BMFont loader, no OpenAL/zlib/crypto native libraries); profiled AOT stays on for a ~45 % faster cold start. Trade-offs per measure in `docs/android-size.md`.
- **Android idle cost:** `AndroidInputBridge` delivers touch and keys thread-safely (MonoGame's `TouchPanel`/`Keyboard` are not) and wakes an idle on-demand loop immediately, so the game loop can run off the UI thread (`RenderOnUIThread = false`) with a long `IdleUpdateInterval`. Android hooks (`AndroidWindowInsets`, accessibility bridge) are safe to attach from the game thread. The Android demo idles at ~0.5 % CPU on a Pixel 5 (was ~7 %), taps, typing and scrolling unchanged. `ScreenEngine.WakeUp()` ends an idle wait; cross-thread `RequestRedraw`/`InvokeOnGameThread` and scheduled frames do so automatically.
- **Render quality:** `ScreenEngineOptions.RenderQuality` (Auto/High/Balanced/Low, switchable at runtime) is a device and battery policy on top of any theme. Balanced stops never-resting decorations (glass sweeps, film-grain noise) so idle screens need no frames; Low also skips backdrop blur (glass falls back to its tint), theme post effects (display effects stay), uses 3 shadow layers instead of up to 10 and caps drawing at 30 fps. Auto picks Low in Android battery saver. `MaxFrameRate` caps drawing explicitly. On a Pixel 5, scrolling the Android demo costs ~16 % CPU at Low vs ~40 % at High.
- **Button variants:** `Button.Variant` (Primary/Secondary/Danger) styled by the new `PortableTheme.PrimaryButton`/`SecondaryButton`/`DangerButton` slots, so call-to-action buttons follow the theme instead of a flat fill.
- **Chrome brushes:** `ChamferBrush` (cut corners, gradients, accent bar), `FrameBrush` (concentric rings, notched pixel corners), chained shadows (`ShadowStyle.Also`), bevel lines scaled under `LayoutScale` (`BrushContext.Scale`).
- **Display effects:** `ScreenEngineOptions.PostEffects` / `UISurface.PostEffects` hold effects of the screen the UI is shown on (e.g. an in-world CRT's curvature and scanlines). They run after the theme's effects and survive theme changes; pointer input follows the curvature. Amber and Phosphor no longer curve the flat screen; the world-space demo monitor is a CRT by default and switches to LCD. `IsPointOnDisplay` tells hosts whether a point is on the (curved) picture; the demo shows the in-game pointer only there and the system pointer everywhere else.
- **Liquid glass, for real:** `LiquidGlassBrush` refracts the sharp backdrop with a new `LiquidGlass` shader — lens-like displacement at the rounded rim, slight chromatic fringe, specular top-left rim, almost clear body (`Refraction`, `Bezel`, `Chroma`, `Frost`, `Highlight`); falls back to frosted acrylic without shader support. `BackdropSource.TryGetSharp` exposes the unblurred backdrop.
- **Fixes:** hover follows recycled list rows after scrolling (enter by hover state, not edge crossing; re-evaluated when content moves under a resting pointer); list frames keep their bottom line on half-pixel positions.
- **Performance:** `PatternBrush` pre-repeats its tile to ~128 px and snaps to whole pixels — a full-screen pinstripe went from ~450k sprites (34 ms/frame) to ~60 (Aqua 2.6 ms, Mac 1-bit 15 → 3.4 ms, Phantom/Tactical 5 → 2–2.7 ms), pixel-identical. Demo: `--benchmark-themes out.csv` measures frame time and draw calls per theme.
- **Nested glass:** when an overlay (modal, sheet) contains glass, the engine renders the screens below into an offscreen picture and hands it to the overlay as its backdrop, so dialog glass blurs/refracts the real UI underneath. New theme slots `ModalBackgroundBrush` and `ModalShadow`; glass dialogs keep their rounded shape (no clip layer).
- **Animated wallpapers:** the glass themes' backgrounds drift slowly (`Animated`, `Speed`), requesting frames in on-demand mode.
- **Glass themes redesigned:** Liquid Glass (dark, vivid orb wallpaper, refracting pill buttons and rounded cards) and Frosted Glass (light, milky frost over a pastel wallpaper, filled system-style buttons, segmented tabs). The demo dropped its glass-only panel special case; cards take corners and shadows from the theme.
- **Pointer capture for in-world screens:** `SurfacePointerCapture` — click in, Escape out: relative mouse motion drives a pointer that belongs to the surface (unaffected by camera/monitor movement), confined to the visible picture incl. the CRT curve; the release key is swallowed. `WorldSurfaceMapper.MapSurfaceToScreen` puts the system pointer back where the in-world one was. The world-space demo starts in capture mode and can switch to the free hover pointer.
- **Themed software cursor:** `PortableTheme.Cursor` (`CursorStyle.ModernArrow` fine and anti-aliased, `Arrow` classic 1-bit, `PixelArrow` 8-bit) drawn when `ScreenEngineOptions.ShowSoftwareCursor` / `UISurface.ShowSoftwareCursor` is on. It is part of the UI, so display effects such as CRT curvature bend it while it stays under the physical pointer. Defaults follow the theme era (pixel for retro, 1-bit for classic desktops/terminals, fine for modern); several themes bring their own colors. Demo: `--screenshot-cursor x,y`.
- **Copyable themes:** `ThemeBuilder` moved into the core library, so each theme file depends only on MonoGame.PortableUI and can be copied alone (see `Themes/README.md`).
- **Themes:** Windows 95, XP Luna, Aero, Aqua, E-Ink, Mac 1-bit, Game Boy and Neumorphism reworked to match their originals; five new game UI themes (Sci-Fi HUD, Dark Fantasy RPG, Cozy Casual, Tactical Ops, Phantom Strike) with four new OFL fonts — 42 themes in total.
- **Fix:** `TextBox` draws its theme's border, corner radius and shadows, not only the fill.
- **RadioButton** is a classic radio (round ring with a dot, label beside it) styled from the theme's check box values instead of a button face; use `ToggleButton` for a segmented look. Same-named radio groups on different screens/surfaces no longer affect each other.
- **Focus ring only for keyboard/gamepad** (like CSS `:focus-visible`): clicking or tapping no longer leaves an outline; text boxes keep theirs. Tab now switches to keyboard navigation mode. The default ring is subtler: thin, slightly translucent and offset a little outside the control (focus rings are no longer clipped). Radio labels sit centred on the ring. Demo: `--screenshot-tabs n`.
- **Fixes:** the scrim tap that closes a modal no longer clicks the control below it; frosted/liquid glass fill their rounded corners; hover overlays and shadows keep cut-corner shapes.
- **Demo:** controls follow the theme instead of demo-set colors; `--screenshot-themes id,id` renders a subset.

## 0.3.0-alpha.2

Android-ready (dp layout, edge to edge, safe areas, soft keyboard, TalkBack bridge, touch scrolling with fling and overscroll), virtualized lists and grids, per-engine focus, modals, toasts, menus, localization, text scaling and an optional FontStashSharp package.

### Known limitations
- Glass (backdrop) blur radii are not scaled by `LayoutScale` yet, so blurred surfaces look slightly sharper on high-density screens.
- The Android soft keyboard ignores `TextInputPurpose` (MonoGame owns the input connection); TalkBack is not device-tested; there is no iOS head.
- Virtualized lists use one row height for all rows (`IsVirtualizing = false` for variable heights).

### Breaking changes
- `ScreenEngine.FocusedControl` is an instance property: every engine (window, `UISurface`, player) has its own focus. Use `engine.FocusedControl` or `ScreenEngine.For(control)`.
- Removed `CornerStyle` (enum, `StateStyle.CornerStyle`, `LiquidGlassBrush.CornerStyle`): squircle/cut corners were never rendered.
- Removed `Control.BackdropMode` and the `BackdropMode` enum: `GrabPass` was not implemented.
- Removed `Typography.HeadingScale`; use `Typography.HeadingSize` with the new `TextBlock.IsHeading`.
- `IInputSource` gained `GamePad` (default interface member, existing implementations keep compiling).
- Arrow keys / D-pad directions a focused control does not use itself (`Control.HandlesDirection`) now move focus spatially; `Slider` keeps only Left/Right.
- `ListBox` calls `ToString()` only for new or replaced items; call `Refresh()` after editing items in place (as documented).

### Added
- Gamepad support and spatial focus navigation: D-pad/left stick/arrow keys move focus to the nearest control, A activates, B/Escape close the open popup or raise `Screen.BackRequested`, shoulders step through tab stops, Y / context-menu key / Shift+F10 open a context menu. Focused controls scroll into view.
- Keyboard and gamepad control of popups: menus and the ComboBox dropdown take focus when opened from the keyboard, Enter activates, focus returns to the opener on close; `ListBox`/`DataGrid` support PageUp/PageDown/Home/End.
- `ScreenEngine.PushOverlay` (screens below stay visible, frozen), `ScreenTransition` (Fade, SlideFromRight, SlideFromBottom) for push/pop, `Screen.OnNavigatedTo`/`OnNavigatedFrom` hooks and events, focus restored when navigating back.
- `WrapPanel`, `UniformGrid` and `StackPanel.Spacing`.
- `TextBox`: undo/redo (Ctrl+Z, Ctrl+Y/Ctrl+Shift+Z), word navigation and deletion (Ctrl+arrows, Ctrl+Backspace/Delete), word wrap for multiline boxes (`TextWrapping.Wrap`).
- `PathGeometry` (lines, curves, arcs, rounded rects, ellipses) with a CPU rasterizer and the `PathShape` control.
- Non-rectangular clipping: `Control.Clip` with `RoundedRectClip`/`PathClip`, inside or outside, nestable; `Control.ClipToCornerRadius`.
- Modal layer: `ScreenEngine.ShowModal(content, options)` with scrim, focus capture, Back/Escape handling, centred panel or bottom sheet.
- Toasts: `ScreenEngine.Toasts.Show(...)` with a queue, durations, overflow policy and edge placement; overlays, modals and toasts now also draw on a `UISurface`.
- `ScrollViewer.ScrollDirections` for two-axis scrolling; horizontal wheel and Shift+wheel (`IInputSource.HorizontalScrollWheelValue`).
- Menus: separators, icons, shortcut text, disabled and checkable items, drill-down submenus; `FlyOutPlacement.Right/Left/Auto` with flipping at the screen edge.
- `ListBox.SelectionMode` (Single, Multiple, Extended with Ctrl/Shift), `SelectedIndices`/`SelectedItems`, and `ItemTemplate` for `ListBox` and `ComboBox`.
- `TextScaling`: one app-wide text size factor from a fixed value, the app's setting or the OS font scale (Android hook `AndroidTextScaling`), clamped; layout is measured at the scaled size.
- Localization: `Localizer` with JSON catalogues, region → language → fallback lookup, culture-aware `Format`, visible placeholders for missing keys and a live language switch (`control.Localize(...)`, `LocalizedText(key)`).
- Safe areas: `ScreenEngine.SafeAreaInsets`/`KeyboardInset`/`SafeAreaChanged`, the `SafeAreaPanel` control and `AndroidWindowInsets` for cutouts, system bars and the IME.
- On-screen keyboard bridge: `IOnScreenKeyboard` with `TextInputPurpose` (`TextBox.InputPurpose`), `AndroidOnScreenKeyboard`, and `DelegateOnScreenKeyboard` as an SDK-free hook for Steam's keyboard; the focused field scrolls above the keyboard.
- Font backends: `UIFont` abstraction (`TextBlock.DynamicFont`, `FontManager.DefaultDynamicFont`, `SpriteFontUIFont`) and the new optional package **MonoGame.PortableUI.FontStashSharp** (`FontStashUIFont`) for runtime-rasterized TTF/OTF text at any size and with any character.
- Accessibility: platform-neutral `AccessibilityTree` (roles, labels, values, states, actions, reading order from the focus order, `Control.Accessibility` overrides, live regions), `ScreenEngine.AccessibilityBridge` that only works while a screen reader is active, and `AndroidAccessibilityBridge` for TalkBack.
- `ScreenEngine.InvokeOnGameThread` for platform callbacks.
- Virtualized `ListBox` and `DataGrid` (`IsVirtualizing`, on by default): only the rows in view get controls, recycled while scrolling; `ListBox.ScrollIndexIntoView`.
- `ScreenEngineOptions.LayoutScale`: lay out in density-independent units and draw at native resolution (Android: the display density); rows and single-line text boxes grow to fit scaled text.
- `ScreenEngine.HandleKeyCommand` and IME text routing in `AndroidOnScreenKeyboard`.
- Touch scrolling: touch capture (the viewer that took a drag keeps it until the finger lifts), touch slop and axis-locked hand-off between nested viewers, animated fling from the finger's velocity and an animated rubber-band spring (`FlingDeceleration`, `RubberBandStiffness`).
- `OverscrollEffect.Stretch` (Android 12+ stretch) besides the rubber band, per viewer or via `ScreenEngineOptions.OverscrollEffect`; `ScrollDirections.None`.
- `ScreenEngineOptions.HoverOnTouch` (off by default): touch no longer leaves controls looking hovered.
- Touch press feedback like Android: a finger only looks pressed after resting 100 ms (a drag that becomes a scroll never lights the row up); quick taps still flash briefly.
- `AndroidSurfaceSize.Follow`: keeps the back buffer equal to the game view, which makes edge-to-edge windows work (verified on Android 14 and 16).
- `ControlStyle.TransitionDuration` drives the button press animation (zero = no animation); `StateStyle.FocusVisualKind` of the Focused state selects the focus visual; `TextBlock.IsHeading` uses `Typography.HeadingSize`.

### Changed
- Default `TextBox` padding is 10/4 (was 4).
- Fixed: nested scroll viewers drifted (the inner one re-applied the outer scroll), which emptied lists after scrolling the page; unchanged Min/Max sizes no longer trigger a layout pass every frame.
- Data grids whose columns fit no longer scroll sideways.
- Fixed: taps were lost because the touch state was read twice per frame (introduced with keyboard/gamepad popup control).
- Large layout/render performance work: per-pass measure cache, scroll shifting instead of re-layout, allocation-free visual-child walks, 9-sliced translucent rounded fills, cached text metrics (Grid layout of 500 controls 414 µs → 26 µs).
- Many control fixes (fonts without a default character, theme fonts, hit-test order, touch panning in lists, Min/Max constraints, focus on hidden/removed controls, nested scrolling, navigation layout, popup layout, theme switching, RadioButton groups, selection after item removal, DataGrid template cells, TabControl clipping).

## 0.3.0-alpha.1

- Solution, library and packages moved to **.NET 10**. The NuGet packages now ship `net10.0` and `net10.0-android36.0` assemblies instead of `net8.0`.
- **Android support**: `MonoGame.PortableUI` and `MonoGame.PortableUI.Themes` multi-target `net10.0-android`; `AndroidClipboardService` wires the clipboard to the platform, and `ScreenEngine`/`FontManager`/`BackdropManager`/`PostProcessManager` reset their static state so the UI survives an activity restart. `samples/MonoGame.PortableUI.Demo.Android` is a minimal host (activity + manifest) running the same controls on device/emulator. Android hosts must pin `PreferredBackBufferWidth/Height` to the real display size — MonoGame's density-scaled default diverges from the GL surface and clips text in content-tight controls (buttons, list rows); see `AndroidDemoGame` and the README.
- **`DataGrid`**: columns (`DataGridColumn`), click-to-sort with triangle sort glyphs, row selection (`SelectedIndex`/`SelectedItem`, `SelectionChanged`, `RowInvoked`), alternating row brushes, optional grid lines and column headers, and a horizontal scrollbar for wide grids. Covered by regression tests and shown in the demo.
- **`Badge`**: count/dot pill (`Count`, `Dot`, `ShowZero`, `BadgeColor`, `TextColor`).
- **`ToggleSwitch`**: animated sliding knob (`IsOn`, `Toggled`, `SlideSeconds`, `KnobInset`, separate on/off track and knob brushes).
- **`SwipePresenter`**: panel that animates content swaps in a direction (`Swipe(content, direction)`, `SetContent`, `Duration`, `Easing`).
- **`ShimmerGlassBrush`**: rounded glass fill with an animated diagonal sweep (`SweepColor`, `SweepSpeed`, `SweepStrength`, `BandWidthFraction`, `SweepSkew`). The streak is a soft-faded band sheared across the surface, clipped to the brush bounds and free of scanline seams.
- `FrostedGlassBrush` supports rounded corners; backdrop blur is triggered from the screen's own background brush.
- `TextBlock` gained `TextWrapping` and `TextTrimming` (word wrap and ellipsis) plus a text shadow (`ShadowColor`, `ShadowOffset`, `ShadowBlur`).
- Rounded borders can be drawn with a diagonal bevel via `Control.BorderBevelLight`/`BorderBevelDark`.
- Image fixes: `ImageBrush` in `UniformToFill` clips its overflow instead of bleeding past the control, image brushes follow the control's corner radius, and the disabled-state overlay is clipped to that radius too. Image controls and image-brush backgrounds now sample linearly, so scaled images are smooth instead of blocky.
- Fixed seams between the slices of translucent rounded surfaces.
- Gradient brushes share one ordered-stop evaluation and caching path (`LinearGradientBrush`, `RadialGradientBrush`).
- Broad control fixes across `Grid`, `StackPanel`, `TabControl`, `ComboBox`, `ListBox`, `RadioButton`, `ProgressIndicator`, `TextBox`, `ToggleButton`, `ScrollViewer`, `Screen` and the rounded-rect/border renderers, with new regression suites covering them.
- `Screen.ExternalBackdrop` / `UISurface.ExternalBackdrop`: a host game can feed its rendered frame as the backdrop that glass brushes blur — frosted glass now works over live game scenes, and `BackgroundBrush` becomes optional when an external backdrop is supplied.
- `TextBlock.FontOverride`: assign a specific SpriteFont (size/weight) per block; wins over theme/default resolution and survives theme switches.
- Theme catalog with 37 built-in themes, including a `default` entry that shows the library's untouched styling when no theme is applied.
- Themes moved into their own add-on package **MonoGame.PortableUI.Themes** (`PortableThemes.All`/`Resolve`; replaces the core `ThemeRegistry`): one self-contained file per theme so a single theme can be copied into a project and customized; the core library works completely without the package. `PortableTheme.FromPalette(palette)` (in core) builds a full theme from the 19 palette slots. The demo now defines only `DemoTheme.cs`, a commented template theme shown next to `default` in the picker.
- The Themes package ships its fonts as NuGet content files (`ThemeContent/Fonts` with TTFs, spritefont descriptors and licenses, plus a ready-to-paste `themes-fonts.mgcb-snippet.txt`); `FontManager.TryGetFont`/`GetFontOrDefault` fall back to the default font with a one-time warning when a theme font is not built, instead of throwing.
- Fixed straight-alpha colors being drawn under SpriteBatch's premultiplied AlphaBlend: all brushes now premultiply, which fixes over-bright translucent surfaces (frosted glass, gradients, hover overlays, AA corners).
- Real backdrop blur (R8): `BackdropManager` renders the screen background into a scene target and blurs it with a shader-free bilinear down/upsample chain; `FrostedGlassBrush`/`AcrylicBrush`/`LiquidGlassBrush` sample the blurred backdrop in screen space.
- Real post-process chain (R9): scanlines, dot-matrix, vignette, film grain, CRT barrel (distortion mesh) and bloom now actually render (shader-free); used by phosphor, amber and cyberpunk.
- Shadows: `ShadowStyle` rendering follows rounded corners, uses normalized layer alpha, and is no longer clipped away by the control scissor; themes define `ButtonShadow`/`PanelShadow` (material, neumorphic, brutalist, studio, glass, and more).
- Demo: switching themes now rebuilds the screen so the selection actually applies everywhere (selected tab is preserved); `--screenshot` renders the real `MainScreen` per theme via `UISurface` instead of a mock, and `--screenshot-screen <tab>` selects a tab (default: the Controls page).
- Layout fixes: `Grid` now measures its content (Auto/star tracks) instead of reporting zero size; `TextBlock` measurement includes its margins (labels no longer collapse and clip); `Panel` gained `Padding`, honored by `StackPanel` and `Grid`.
- Clipping is now opt-in per control (`ClipsDescendants`, enabled for `ScrollViewer`), so drop shadows are no longer cut off at panel edges.
- `ShadowStyle.Opacity` scales overall shadow strength; the demo FX tab exposes blur, X/Y offset (negative supported) and opacity sliders with live numeric labels.
- `ComboBox` draws a themeable dropdown triangle (`PortableTheme.ComboBoxGlyphColor`, `ComboBox.GlyphColor`/`GlyphSize`) and reserves padding so text can't overlap it.
- `Image.Stretch` defaults to `Uniform` (WPF-like) and the image is centered within its bounds; oversized icons no longer disappear into a clipped corner.
- T2 style system: controls resolve `ControlStyle`/`StateStyle` at use time (background, border, corner radius, shadow per visual state); explicit assignments always win; internal chrome buttons opt out via `UseThemeStyle`.
- T3 live theme switching: changing `Options.Theme` (or a `ThemeIsland`) restyles the existing tree — controls re-seed constructor snapshots while preserving user overrides; fonts switch per theme.
- R3 real shaders: `dotnet-mgfxc` tool manifest entry; compiled `Blur` (separable Gaussian) and `PostFx` (single-pass scanlines/dot-matrix/vignette/grain) effects with automatic shader-free fallback.
- Post effects are now ThemeIsland-scoped: an island whose theme has enabled post effects renders its subtree distorted/composed within its own rect (e.g. a CRT monitor inside another screen), and mouse/touch positions are inverse-mapped through the barrel (screen-level and per-island) so CRT themes are fully usable.
- Era chrome wave: XP Luna glossy buttons with orange hover ring, Win95/Amiga/BeOS/NeXT bevels (`BevelBrush`), Turbo-Vision DOS/Norton dialogs, Aqua gel + pinstripes (`PatternBrush`), LCARS pills, per-theme corner radii and 1px frames across the catalog.
- Contrast audit test (WCAG relative luminance) over all 37 theme palettes; muted text auto-adjusts toward readability; DOS/C64/solarized/neumorphic palette fixes.
- `IsHitTestVisible` on controls (gallery previews are no longer clickable); Grid star-span measurement fix; `ProgressBar.IsIndeterminate` marquee; `ContextMenuTypes.OpenOnLeftClick` + `Control.OpenContextMenu()`; `PortableTheme.ButtonMargin`.
- Drag & drop (WPF-style): `Control.AllowDrop` + `DragEnter`/`DragOver`/`DragLeave`/`Drop`, `DragDrop.DoDragDrop`/`Control.BeginDrag` returning a `DragOperation` (payload, allowed effects, `DragMoved`/`Completed`/`Canceled`), optional ghost visual following the pointer, Esc/right-click cancel, mouse + touch; demo gets a Kanban "Drag & drop" tab.
- World space demo (replaces "Adventure room"): the DOS `UISurface` renders on a swaying perspective 3D quad; the mouse is raycast onto the quad (`WorldSurfaceMapper`) into the surface's `VirtualInputSource`, so the inner screen is fully clickable, with keyboard text routed via `SurfaceFocusManager`. `UISurface.Draw` now restores previously bound render targets.
- Fonts: all demo fonts are now bundled open-licensed TTFs (Selawik, Roboto, Orbitron, VT323 added — no more silent Segoe UI/Consolas aliases); per-font attribution in `docs/FONTS.md`, referenced from README and LICENSE. Fixed collapsed/double-wide space glyphs in retro fonts (`UseKerning` was off in their spritefonts).
- Fixed the screen going black while scrolling the gallery: island post-FX now renders the whole UI into a preserved target before switching render targets mid-frame.
- Demo Controls page: read-only TextBox, left-click context-menu button, live animated determinate progress (with % readout) and an indeterminate marquee bar.
- Keyboard fixes: command keys (Backspace/Delete/arrows/…) now auto-repeat with a typematic profile (500 ms initial delay, then 45 ms repeats); a screen only processes keys/text for focused controls it owns, fixing doubled characters and double-deletes when a UISurface screen updates alongside its host.
- World space demo shows the currently selected theme on the monitor (theme-default styling + theme name in the title) instead of a hard-coded DOS look.
- Focus visuals follow rounded corners (rounded focus ring instead of a rectangle on rounded buttons).
- FlyOuts (dropdowns, context menus) clip their content again; tab headers distribute the strip proportionally to their measured label widths so long headers aren't cut; Press Start 2P now builds at 11 px so the wide pixel themes stay legible with real space glyphs; theme presets cache their created theme, removing the hitch when switching themes.
- ListBox: items are inset by the frame thickness so the themed border stays visible, and hovering the selected item keeps its selected look instead of washing it out.
- Rounded buttons keep their corners on hover/pressed: backdrop brushes (frosted glass/acrylic) expose a solid stand-in used for rounded state overlays.
- DOS/Norton input fields are blue with light text (no more gray-on-gray); the world-space prompt/status use the contrast-audited Text-on-Background pair so they are readable in every theme.
- Fixed doubled characters in the world-space demo (the surface engine already routes `Window.TextInput`; the demo's extra hook was removed).
- TextBox: lines that only partially fit the padded text rect are drawn (scissor-clipped) instead of skipped — text no longer disappears when a TextBox is slightly shorter than the font's line height.
- DOS/Norton ComboBoxes are blue with yellow text and glyph (classic pick-list look); ComboBox honors a style-slot text color distinct from ButtonTextColor.
- `--screenshot-screen worldspace` renders the world-space demo per theme for visual verification.

## 0.2.0-alpha.2

- Added minimal public `TileBrush` and `NineTileBrush` texture brushes.
- Fixed `Grid` Auto sizing for children spanning Auto rows or columns.
- Reduced draw/layout churn in scissor rendering, visual-tree flattening and TextBox text measurement.
- Removed the unused `ContentPresenter` and `Button.Template` alpha placeholders.
- Polished the demo surface for input controls, disabled/focus states, tooltips and button press animation.

## 0.2.0-alpha.1

- Modernized the project for .NET 8 and MonoGame 3.8.4.1.
- Replaced legacy PCL/Xamarin projects with a DesktopGL demo.
- Added regression coverage for the historical GitHub issue backlog.
