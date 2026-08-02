# Changelog

## 0.3.0-alpha.1 (unreleased)

- Theme catalog with 37 built-in themes (`ThemeRegistry`), including a `default` entry that shows the library's untouched styling when no theme is applied.
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
