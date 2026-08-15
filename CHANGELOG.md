# Changelog

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
