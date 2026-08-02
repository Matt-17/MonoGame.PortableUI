# Theme-Plan — Glorious Theming Roadmap for MonoGame.PortableUI

Goal: turn the current 8 "okay-ish" themes into a gallery of **30+ stunning themes** spanning C64 → DOS Turbo Vision → Win95 → Aqua/Luna/Aero → Metro/Material → Frosted/Liquid Glass. This requires new rendering features (rounded corners, shadows, real blur, gradients, textures), a real style system with live theme switching, perf fixes, and a much better demo.

Legend: `R#` rendering, `T#` theming architecture, `C#` controls/bugs, `D#` demo, `W#` world-space, `Q#` tests. **All design decisions are resolved** — marked "DECIDED" (explicitly chosen) or "DEFAULT" (resolved to the recommendation/sensible default; veto by editing). Remaining `- [ ]` checkboxes in §0 are work items, not open questions.

---

## §0.a Implementation audit (2026-07-02)

A code audit found several checked-off items were stubs. Current, verified state:

| Item | Actual state |
|---|---|
| R3 shader infra | **Implemented (2026-07-03).** `dotnet-mgfxc` added to the tool manifest; `Blur.fx` (separable Gaussian, quarter-res, used by backdrop blur + bloom) and `PostFx.fx` (single-pass scanlines/dot-matrix/vignette/grain) compiled to embedded `.ogl.mgfxo` and loaded via `EffectCache`; `RenderCapabilities.*Available` is now true on GL devices. Shader-free chains remain as automatic fallback. Barrel stays a distortion mesh (works with both paths). |
| R4 rounded corners | CPU corner-mask fallback only — works for solid fills; gradient/texture fills are not corner-clipped. |
| R7 shadows | **Fixed in this pass:** rounded, normalized-alpha layered shadows; control scissor widened so shadows aren't clipped; `PortableTheme.ButtonShadow`/`PanelShadow` added and populated for the themes that specify them. SDF path still open (needs R3). |
| R8 backdrop blur | **Implemented shader-free in this pass:** `BackdropManager` bilinear down/upsample chain + `BackdropSource`; glass brushes sample the blurred screen background. Layered mode only (blurs the screen background, not sibling controls); GrabPass and rounded clipping of the glass region remain open. |
| R9 post-FX | **Implemented shader-free in this pass:** scanlines, dot-matrix, vignette, grain (overlay textures), CRT barrel (distortion mesh), bloom (blur chain, additive, no bright-pass). |
| Premultiplied alpha | **Root-cause fix in this pass** (was not in the plan): the whole library drew straight-alpha colors under premultiplied AlphaBlend, over-brightening every translucent draw — the main reason glass/gradient themes looked wrong. All brushes and generated textures now premultiply. |
| T2 style system | **Implemented (2026-07-03):** controls resolve `ControlStyle`/`StateStyle` at use time — `Control.BackgroundBrush/BorderBrush/BorderThickness/CornerRadius/Shadow` are override-or-style getters (explicit assignments win); Button/ToggleButton/ComboBox/TextBox/ListBox consume their theme slots; internal chrome buttons opt out via `UseThemeStyle=false`; flat props remain the fallback for style-less themes. |
| T3 live switching | **Implemented (2026-07-03):** `Screen.Update` runs a `RefreshThemeResources` pass when `ThemeVersion` changes; controls re-seed ctor snapshots in `OnThemeChanged(old,new)` — values still equal to the old theme's are replaced, user overrides preserved (reference equality; theme brushes are shared instances); `TextBlock` re-resolves the theme font and re-measures. |
| Wave 1–3 theme CRs | The 37 registry themes are palette-driven (plus per-theme shadows/post-FX); era-specific chrome (Win95 bevels, Aqua pinstripes/gel, DOS box-drawing frames, LCARS pill corners, dithers) is **not** implemented. |
| D4 screenshots | **Fixed in this pass:** `--screenshot` renders the real `MainScreen` per theme through `UISurface` (previously a hand-drawn mock); `--screenshot-screen <tab>` selects a tab. `docs/themes/*.png` regenerated. |
| New | `default` theme id added (library + demo): selecting it shows `PortableTheme.CreateDefault()` — the UI's styling when no theme is applied. |

---

## §0 Master TODO

### Phase P0 — Quick wins (no design decisions, do first)
- [x] R1.1 Cache `RasterizerState` statically in `Screen.cs` (currently allocated per `DrawControlTree` call, never disposed)
- [x] R1.2 Pool/reuse the flattened visual-tree list used every input tick (`VisualTreeHelper.GetVisualTreeAsList`)
- [x] R1.3 Brush texture cache keyed by parameters (gradients/frost recreated per property change, never shared)
- [x] C3.1 Fix Grid auto-size ignoring `span > 1` children (`Grid.cs` "// Ignore now")
- [x] C3.2 Fix TextBox O(n²) substring measuring (incremental width cache)
- [x] T6.1 Lazy font loading in `FontManager` (stop eager-probing 31 sizes × 4 styles per family)
- [x] R6.0 Release-hardening: replace `TileBrush`/`NineTileBrush` stubs with **minimal** production implementations (tiled fill + stretched 9-slice; explicitly *not* Brush API v2 — no BrushContext/radius/caching yet, that lands with R5/R6)

### Phase R — Rendering foundation
- [x] R2 Batching strategy (see Decision R2)
- [x] R3 Shader infrastructure — `dotnet-mgfxc` in the tool manifest; real compiled effects embedded (`Blur` = separable Gaussian for backdrop/bloom, `PostFx` = single-pass scanlines/dot-matrix/vignette/grain, `Primitives` = sprite tint pass-through for the future SDF path); shader-free fallbacks remain when effects fail to load
- [x] R4 Rounded corners (CPU corner-mask path; SDF shader variant blocked on R3)
- [x] R5 Brush API v2 (`BrushContext`: rect, radius, opacity, time, pointer pos, device)
- [x] R6.1 `LinearGradientBrush` v2: multi-stop + arbitrary angle
- [x] R6.2 `RadialGradientBrush` (circular gradients)
- [x] R6.3 `ImageBrush` (background images: stretch/tile/source-rect/tint)
- [x] R6.4 Implement `TileBrush` (currently `NotImplementedException`)
- [x] R6.5 Implement `NineTileBrush` (9-slice; currently `NotImplementedException`)
- [x] R7 Shadows/elevation (`ShadowStyle`; rounded layered CPU shadows; SDF variant blocked on R3)
- [x] R8 Real backdrop blur pipeline (RT → bilinear down/upsample chain → glass brushes sample it; shader-free)
- [ ] R8.5 Liquid-glass extras — **partial: specular sweep implemented; R3 shaders now available so refraction/squircle-clip/saturation can be built, still open**
- [x] R9 Per-theme post-process chain (scanlines, CRT barrel mesh, vignette, grain, dot-matrix, bloom; shader-free)
- [x] R10 `Theme.PixelSnapping` policy (retro = crisp, modern = AA)

### Phase T — Theming architecture
- [x] T1 Promote `DemoThemePalette` → library `ThemePalette`
- [x] T2 Style system — controls resolve `ControlStyle`/`StateStyle` at use time (background, border, corner radius, shadow per state; explicit assignments win; flat props remain the fallback for style-less themes)
- [x] T3 Live theme switching — `Screen.Update` runs a `RefreshThemeResources` pass keyed on `ThemeVersion`; controls re-seed ctor snapshots in `OnThemeChanged(old,new)` (reference equality preserves user overrides); `TextBlock` re-resolves the theme font
- [x] T3.5 **Theme Islands**: `ThemeIsland` container control for per-subtree themes (nearest-island resolution, overlay/generated-child inheritance, local overrides survive)
- [x] T4 State transitions: `ColorAnimationTween`, brush crossfade, more easings (Quad/Expo/Back/Elastic/Bounce)
- [x] T5 Theme registry in library (see Decision T5 for packaging)
- [x] T6 Typography per theme + new OFL fonts in mgcb (Selawik, Roboto, VT323, Orbitron, …)
- [x] T7 Theme metadata (era, dark/light, preview swatches, `ReducedMotion`)

### Phase C — Controls & bug fixes
- [x] C1 `BorderBrush`/`BorderThickness`/`CornerRadius` styleable on all chrome controls (not just `Border`)
- [x] C2.1 New control: `Slider`
- [x] C2.2 New control: `ProgressBar` (determinate; `ProgressIndicator` stays for indeterminate)
- ~~C2.3 Optional `Expander`/`Hyperlink`~~ — skipped (neither checked; not needed for the gallery, can be added later)
- [x] C3.3 Keyboard input via `Window.TextInput` (replaces hard-coded US layout)
- [x] C3.4 Implement minimal `Template`/`ContentPresenter` (decided — see §4 C3.4)
- [x] C4 Measure caching / fix Grid double-measure
- [x] C5 Styleable check glyph, radio dot, focus-visual kind (rect/dotted/glow/thick)

### Phase Themes — three waves (details in §5)
- [x] **Wave 1** — era chrome via `ApplyThemeChrome`: Win95/Amiga/NeXTSTEP raised/sunken `BevelBrush` chrome (pressed inverts), BeOS single-line bevels, DOS/Norton Turbo-Vision dialog buttons (light face, black text, hard 2px shadows, framed fields/lists), mac1bit black frame + invert-pressed + dither disabled-overlay (`PatternBrush.Dither`), C64/NES 2px chunky borders, LCARS pill buttons, e-ink/brutalist ink frames, nord/dracula/gruvbox/solarized subtle 1px frames + radii
- [x] **Wave 2** — Luna XP glossy gradient face + `#003C74` frame + radius 3 + orange hover ring, Aqua gel-gradient buttons (r12) + pinstripe chrome (`PatternBrush.Pinstripes`), macOS 9 gradient buttons, studio/aurora/neumorphic/material/fluent radii + borders, vaporwave neon frames — *(open polish: parchment paper-noise texture, macos9 pinstripe headers)*
- [x] **Wave 3** (adds R8/R9 blur/post-FX): Fluent Acrylic, Aero, Liquid Glass, Green Phosphor CRT, Amber Terminal, Cyberpunk Neon + CRs: glass, terminal — backed by the shader-free R8/R9

### Phase D — Demo project
- [x] D1 Theme Gallery screen (live mini-preview cards, instant apply)
- [x] D2 Showcase tabs: all controls (+Slider/ProgressBar), Visual-FX playground, animation/easing tab
- [x] D3 Theme inspector (palette swatches, RGB edit, export C# theme code to clipboard)
- [x] D4 `--screenshot <dir>` CLI mode (render each theme to PNG)
- [x] D5 launchSettings scaling (see Decision D5)
- [x] D6 F3 debug overlay (FPS, batch flushes, layout passes)
- [x] D7 README theme-gallery grid from D4 screenshots

### Phase W — World-space UI (in-game screens, e.g. a DOS terminal on an adventure-game computer)
- [x] W1 Off-screen rendering: `UISurface` renders a `Screen` into a `RenderTarget2D` at a fixed virtual size (builds on `ScreenSizeMode.Manual`)
- [x] W2 Multi-surface engine: remove the single-engine statics (`ScreenEngine.Instance`, `FocusedControl`, `ScaleFactor`) or scope them per surface
- [x] W3 Virtual input: wire up the existing-but-unused `IInputSource` (`Input/IInputSource.cs`) so a surface consumes mapped input instead of polling `Mouse`/`TouchPanel`
- [x] W4 World mapping helpers: ray → quad plane → UV → virtual mouse coords; focus/activation model (which surface owns keyboard)
- [x] W5 Software cursor + per-surface post-FX (R9 CRT barrel on the in-world monitor = the money shot)
- [x] W6 Demo: "Adventure Room" screen — perspective quad with a live DOS-themed surface on a computer prop

### Phase Q — Tests & benchmarks
- [x] Q1 Unit tests: new brushes, style resolution, palette→style mapping, registry completeness for all 30+ themes; ThemeIsland resolution/inheritance suite (see T3.5)
- [x] Q2 Visual regression (see Decision Q2)
- [x] Q3 Benchmarks: rounded-rect draw, blur pass, themed-tree redraw; UISurface render-to-target overhead

---

## §1 Project analysis & feedback

### 1.1 Architecture

| Finding | Where | Impact |
|---|---|---|
| Controls snapshot theme values in constructors via `PortableTheme.ResolveCurrent()` | `Control.cs:42-62`, `Button.cs:30-40` | No live restyle; demo must rebuild the whole tree on theme switch. Root cause of most theming friction. |
| `PortableTheme` is a flat bag of ~60 properties | `PortableTheme.cs` | No per-state styling, no per-control-type grouping; every new theme feature widens the bag. |
| Hover/pressed visuals hand-coded per control | `Button.ChangeVisualState` (`Button.cs:168`), similar in TextBox/Tab/ListBox | Each theme idea (glow, invert, ripple) needs code in N controls instead of style data. |
| `TileBrush`/`NineTileBrush` throw `NotImplementedException` | `Media/TileBrush.cs:10`, `NineTileBrush.cs:10` | Blocks dither/pinstripe/parchment/ornate-frame themes. |
| `ContentPresenter` empty stub; `Button.Template` dead scaffolding | `ContentPresenter.cs`, `Button.cs:18,42-45` | Half-built templating; decide (C3.4). |
| The demo owns the good ideas: semantic palette + registry | `DemoThemePalette.cs`, `DemoThemeRegistry.cs` | Should be promoted to the library so consumers get them (T1, T5). |
| `ScreenEngine` statics + "small hack" comments | `ScreenEngine.cs:16,92,104` | Single-engine assumption; test-bleed risk (tests already need `ResetState`). |

### 1.2 Performance

- `new RasterizerState{...}` allocated **every** `DrawControlTree` call (multiple per frame incl. overlays), never disposed — `Screen.cs:263`. → R1.1
- `SpriteSortMode.Immediate` everywhere: every `Draw`/`DrawString` flushes; worst case for many small controls. → R2
- No measure caching; `Grid.GetRowHeights/GetColumnWidths` measure children per auto track and again in `UpdateLayout` → children measured many times per pass. Any `InvalidateLayout` relayouts the whole screen. → C4
- `VisualTreeHelper.GetVisualTreeAsList` = recursive `yield`/`SelectMany`, allocating iterators per node, called every input tick and per scroll. → R1.2
- `TextBox` measures substrings in loops (`GetPositionForX`, `GetVisibleTextRange`, `DrawText`) — O(n²) per line. → C3.2
- `FontManager.LoadFonts` eagerly probes/loads up to 31 sizes × 4 styles per family; with ~15 theme fonts this explodes startup time and memory. → T6.1
- Gradient/frost textures rebuilt on any property setter, no cross-instance cache; 30 themes × many brushes = churn. → R1.3

### 1.3 Bugs / UX gaps

- Grid auto-sizing ignores spanning children (`Grid.cs:130,164,253`). → C3.1
- Hard-coded US keyboard layout in `Screen.TryGetCharacter` (`Screen.cs:575`); MonoGame's `Window.TextInput` event delivers correct localized chars. → C3.3
- CheckBox marker is a literal "X"; visually cheap in every theme. → C5
- No `Slider`/`ProgressBar` — a theme gallery without them looks incomplete. → C2
- Focus visual is a plain rect border only — retro themes want thick/dotted, modern want glow rings. → C5
- `MultiSampleAntiAlias = true` in the rasterizer does nothing for textured quads; real AA must come from shaders (R4) — remove misleading flag.

---

## §2 Phase R — Rendering foundation (implementation details)

### R1 Perf quick wins
1. **R1.1** `private static readonly RasterizerState ScissorRasterizer = new() { ScissorTestEnable = true };` in `Screen`; reuse in all `Begin` calls.
2. **R1.2** Keep a `List<Control>` field on `Screen`, `Clear()` + refill per tick instead of LINQ chains; or make `IterateVisualTree` allocation-free (explicit stack, no `SelectMany`).
3. **R1.3** Static `BrushTextureCache`: `Dictionary<CacheKey, Texture2D>` keyed by brush type + params hash (e.g. gradient stops+direction). Brushes look up before `new Texture2D`. Evict on `GraphicsDevice.DeviceReset`.

### R2 Batching — DECIDED: shader clip-rect
Chosen: pass the clip rect as a shader param (the R4 shader clips per-pixel); drop the scissor test for everything except a safety path. Requires R3/R4 first — until they land, do the cheap interim step (Deferred + batch restart on scissor change) so Wave-1 perf improves early.

Implementation details:
1. The universal `Primitives.fx` vertex/pixel pair becomes the default SpriteBatch effect for the whole UI pass. Add `float4 ClipRect` (screen-space min/max); pixel shader: `if (any(screenPos < ClipRect.xy) || any(screenPos > ClipRect.zw)) discard;` — or alpha-out via `step()` to keep it branch-free. Screen pos comes from a `VPOS`/`SV_Position` semantic or a passthrough `TEXCOORD1` computed in the VS from the ortho transform.
2. `EffectParameter` values can't vary per sprite inside one deferred batch → restart the batch (`End`/`Begin`) when `RenderContext.ClipRect` changes, same as the scissor variant — but no `GraphicsDevice` state change, and text/images/gradients all clip pixel-perfectly (scissor only clips to integer rects; shader clip supports the transformed/scaled subtrees where scissor rounding currently bleeds).
3. Track the active clip in `RenderContext` (replaces `GraphicsDevice.ScissorRectangle` writes in `Screen.DrawControl`); `ToScissorRectangle` floor/ceil logic disappears — keep `Rect ^ Rect` intersection as is.
4. Keep one `RasterizerState` **without** scissor (`RasterizerState.CullNone` equivalent, cached per R1.1). ScrollViewer needs no special path anymore — its content clip is just another `ClipRect` value.
5. Fallback when shaders unavailable (R3 gate): revert to scissor + Deferred-restart automatically; `RenderContext` abstracts which mechanism applies so draw code doesn't branch.
6. Batch-flush counter hooks here for the D6 overlay: increment on every internal `End`.

### R3 Shader infrastructure
1. New folder `src/MonoGame.PortableUI/Effects/` with `.fx` sources (`Primitives.fx`, `Blur.fx`, `PostFx.fx`).
2. Precompile with the `mgfxc` dotnet tool (`dotnet mgfxc Primitives.fx Primitives.ogl.mgfxo /Profile:OpenGL`) — wire into the csproj as a `BeforeBuild` target or check in the compiled `.mgfxo`. **This bypasses the demo's `Content.mgcb`** (which is Reach-profile and demo-owned); the library stays self-contained.
3. Embed as `EmbeddedResource`; load lazily: `new Effect(graphicsDevice, resourceBytes)`, cached per device in an `EffectCache`.
4. **Graceful fallback**: every feature that uses a shader must render acceptably without it (sharp corners, offset solid shadow, no blur) so the library still works if effect loading fails. Gate via `RenderCapabilities.ShadersAvailable`.
5. DECIDED — compiled effect distribution: check compiled `.mgfxo` into the repo (no build-time tool dependency for consumers).
   - Repo layout: `Effects/src/*.fx` (sources) + `Effects/compiled/*.ogl.mgfxo` (checked in, `EmbeddedResource`).
   - Add `eng/build-effects.ps1` (wraps `dotnet mgfxc … /Profile:OpenGL` per file) + a `dotnet tool` manifest entry so regeneration is one command; document in `eng/README.md`.
   - CI guard (optional): job that recompiles and diffs against the checked-in binaries so sources and binaries can't drift silently.

### R4 Rounded corners
1. New struct `CornerRadius(float TopLeft, TopRight, BottomRight, BottomLeft)` in `Common/` + implicit `float →` uniform conversion (WPF dialect).
2. `Primitives.fx` pixel shader — SDF rounded rect:
   - `float sdRoundRect(float2 p, float2 halfSize, float r) { float2 q = abs(p) - halfSize + r; return length(max(q,0)) + min(max(q.x,q.y),0) - r; }`
   - per-corner radius: select `r` by quadrant sign of `p` (`r = p.x>0 ? (p.y>0 ? rBR : rTR) : (p.y>0 ? rBL : rTL)`).
   - AA: `alpha = 1 - smoothstep(-1, 0, d)` (1px edge; multiply by fill color).
   - Border in the same pass: `borderAlpha = smoothstep on (abs(d + bw/2) - bw/2)` → fill where `d < -bw`, border band, transparent outside. Params: `RectSize`, `CornerRadii (float4)`, `BorderWidth`, `FillColor`, `BorderColor`; optional texture sampling for image/gradient fills.
3. Draw path: one `spriteBatch.Draw(pixel, rect, …)` with the effect bound and per-draw params via `EffectParameter` (requires batch restart per unique param set — acceptable; or use `Immediate` for shader-drawn chrome only).
4. CPU fallback — DECIDED: cached quarter-circle corner mask textures.
   - `CornerMaskCache` (uses R1.3 infra): one white AA quarter-disc `Texture2D` per distinct integer radius (rendered CPU-side once: per-pixel coverage = clamped `radius - distance`), tinted at draw time.
   - Composite per control: 4 corner draws (`SpriteEffects.FlipHorizontally/Vertically` reuse one texture) + 3 body rects (center + top/bottom strips between corners). Border fallback: 4 edge rects + 4 quarter-*ring* masks (second cache keyed by radius+thickness).
   - Per-corner radii work naturally (different mask per corner); shadows in fallback come from R7's baked path.
5. Focus ring becomes an outer rounded outline drawn by the same shader (offset SDF `d - ringOffset`).

### R5 Brush API v2
1. New `readonly struct BrushContext { Rect Rect; CornerRadius Radius; float Opacity; GraphicsDevice Device; float TimeSeconds; PointF? PointerPosition; }`.
2. `Brush.Draw(SpriteBatch, in BrushContext)` becomes the primary virtual; existing `Draw(SpriteBatch, Rect, float)` overloads forward with `Radius = 0` (back-compat, existing brushes keep working).
3. `TimeSeconds` (from `ScreenSystem.TotalTime`) enables animated brushes (aurora drift, specular sweep); `PointerPosition` enables Fluent reveal-highlight.
4. `Control.OnDraw` passes its resolved corner radius so **every** brush is rounded correctly.

### R6 New brushes (all honor `BrushContext.Radius` via R4 shader or mask fallback)

**R6.0 — staged delivery (per Theme-Plan-Review):** a first release-hardening pass replaces the `TileBrush`/`NineTileBrush` stubs with *minimal* production implementations (public API, tiled texture fill via wrap sampler; 9-slice as 9 stretched sub-rect draws; property-change texture invalidation; unit tests mirroring `BrushRegressionTests`). No `BrushContext`, no corner-radius/opacity integration, no shared caching — those arrive when R5/R6 land and both brushes are then upgraded in place. This unblocks texture-based theming (and world-space props) before the big rendering foundation.
1. **R6.1 LinearGradientBrush v2**: `GradientStop[] Stops` (offset 0–1 + color), `float AngleDegrees`. Implementation: bake stops into a 256×1 `Texture2D` (cached via R1.3); shader computes `t = dot(uv-0.5, dir)+0.5` and samples the strip; CPU fallback = existing 2×2 trick for 2-stop, rotated quad for angle. Keep `GradientDirection` as convenience presets.
2. **R6.2 RadialGradientBrush**: `Center (0–1 rel)`, `RadiusX/Y`, `Stops`. Shader: `t = length((uv-center)/radius)` → sample strip. Fallback: cached 64×64 radial texture stretched.
3. **R6.3 ImageBrush**: `Texture2D Source`, `Stretch` (reuse `Common/Stretch.cs`), `TileMode (None/Tile/FlipXY)`, `Rectangle? SourceRect`, `Color Tint`, `float Opacity`. Draw = one `spriteBatch.Draw` with computed dest/src; tile via `SamplerState.LinearWrap` + oversize source rect.
4. **R6.4 TileBrush**: pattern texture + `Scale`; draw with `LinearWrap` (`PointWrap` when `Theme.PixelSnapping`) and `sourceRect = (0,0,rect.W/scale,rect.H/scale)`. Powers: 2×2 dither (Mac 1-bit), pinstripes (Aqua/OS9), checkerboard (Amiga), grid overlays.
5. **R6.5 NineTileBrush**: `Texture2D`, `Thickness SliceMargins`; draw 9 sub-rect → dest-rect pairs (corners fixed, edges stretched one axis, center stretched/tiled — `bool TileCenter`). Powers: ornate RPG frames, DOS double-line boxes, baked shadows (R7 option B).

### R7 Shadows / elevation — DECIDED: both paths (SDF primary, baked fallback)
`class ShadowStyle { Color Color; Vector2 Offset; float Blur; float Spread; bool Inset; }` on `ControlStyle` (T2). Material elevation presets `Elevation.Level1–5` map to preset ShadowStyles. (`Inset` added for Neumorphic/Luna pressed states.)

Implementation details:
1. `IShadowRenderer` behind a single entry point `ShadowRenderer.Draw(spriteBatch, in BrushContext, ShadowStyle)`; selects implementation once via `RenderCapabilities.ShadersAvailable`.
2. **SDF path** (`Primitives.fx` technique `Shadow`): draw a quad expanded by `Blur + Spread` behind the control; alpha = `1 - smoothstep(-blur, blur, d)` on the same rounded SDF (Gaussian-ish falloff, exact corner match, zero textures). `Inset`: render *inside* the control rect with the SDF negated.
3. **Baked path**: CPU box-blur (3 passes ≈ Gaussian) of an AA rounded rect, once per `(radius, blur)` bucket — quantize blur to steps of 2 to bound the cache — stored as a 9-sliceable texture; drawn via the R6.5 NineTileBrush machinery, tinted per style. Shares `BrushTextureCache` (R1.3) and doubles as the no-shader answer everywhere.
4. `Blur = 0` short-circuits both paths to a plain offset rect (Win95/Brutalist/DOS hard shadows) — no shader, no cache entry.
5. Dual shadows (Neumorphic, Studio): `ControlStyle.StateStyle.Shadow` becomes `ShadowStyle[]?` (draw in array order) rather than a single nullable.

### R8 Real backdrop blur (frosted/liquid glass)
1. `BackdropManager` owned by `Screen`: when the frame contains any brush flagged `RequiresBackdrop`, render the content **behind** the glass layer into `RenderTarget2D SceneRT`.
2. Downsample ÷4 → two `RenderTarget2D` ping-pong buffers; **dual-filter Kawase blur** 2–3 iterations (`Blur.fx`: fixed 4/8-tap offsets scaled by iteration) — much cheaper than Gaussian at equal quality.
3. Glass brushes (`FrostedGlassBrush` v2, `AcrylicBrush`, `LiquidGlassBrush`) draw their quad sampling `BlurredRT` with **screen-space UVs** (`pos / screenSize`), then apply tint, saturation boost (luma lerp), and the existing procedural noise as a grain overlay. Rounded via R4 SDF alpha.
4. Existing `FrostedGlassBrush` CPU-noise texture is kept as the **no-shader fallback** and its grain generator is reused for the overlay.
5. DECIDED — backdrop scope: **layered by default, grab pass as opt-in**.
   - **Default (layered)**: `BackdropManager` produces at most two blur snapshots per frame — snapshot A = background pass (available to all main-tree glass), snapshot B = fully-drawn main tree (available to flyout/tooltip/dropdown layers). Covers panels-over-background and popups-over-UI, i.e. 95% of visuals, at fixed cost.
   - **Opt-in (`Control.BackdropMode = BackdropMode.GrabPass`)**: for pixel-correct stacked glass (glass card sliding over another glass card, draggable glass windows). Implementation: when the tree walk reaches a GrabPass control, `End` the batch, resolve the current backbuffer/RT state into a scratch RT (`GraphicsDevice.GetBackBufferData` is slow — instead render the whole UI into a working RT from the start whenever any GrabPass control exists, so "grab" = copy sub-rect of the working RT), blur only the control's rect (padded by blur radius) at ÷4, then continue. Cost ≈ one small blur per GrabPass control per frame; document as "use sparingly", and the D6 overlay reports GrabPass count.
   - `BackdropMode.Layered` is the enum default; themes never need to set it — only app code with stacked glass does.
6. Skip the whole pipeline when no visible backdrop brush exists (zero cost for non-glass themes); skip snapshot B when no popup layer is open.

### R8.5 Liquid Glass extras (Apple 2025 look)
- **Edge refraction**: in the glass shader, compute the SDF normal near the edge (`normalize(float2(ddx(d), ddy(d)))` or analytic) and offset the backdrop UV by `normal * refractionStrength * edgeFalloff` — the "lens" rim effect.
- **Specular sweep**: additive white highlight band, position driven by `BrushContext.TimeSeconds` (slow sweep) and/or `PointerPosition`.
- **Squircle — DECIDED: include.** Generalize corner shape into `enum CornerStyle { Round, Squircle, Cut }` on `StateStyle` (one enum also solves Cyberpunk's chamfered corners — `Cut` = 45° linear corner SDF `(|x|+|y|-r)/√2`). Squircle SDF: exact superellipse distance has no closed form; use the standard approximation `d = (pow(pow(|p.x|,n) + pow(|p.y|,n), 1/n) - r) * k` with n=4 and a gradient-normalization factor `k = 1/length(∇)` estimated via `fwidth(d)` for stable 1px AA — visually indistinguishable from Apple's continuous corners at UI sizes. Shader gets a `CornerStyle` int param branching between the three distance functions; CPU fallback renders squircle/cut corner masks the same way it renders round ones (per-pixel coverage from the same formulas).
- **Backdrop saturation boost** + slight brightness lift, per Apple's material recipe.

### R9 Per-theme post-process chain
1. `PortableTheme.PostEffects : IReadOnlyList<PostEffect>`; when non-empty, `Screen.Draw` renders the whole UI into a RT, then draws it through `PostFx.fx` techniques, in order.
2. Effects (each a technique + params): **Scanlines** (dark every Nth screen row, strength), **CRTBarrel** (barrel UV distortion + corner vignette + RGB phosphor mask via 3×1 pattern), **Vignette**, **FilmGrain** (hash noise + time), **Bloom** (bright-pass → reuse R8 Kawase blur → additive; for phosphor/neon glow), **DotMatrix** (Game Boy LCD grid).
3. `ScreenEngineOptions.Effect` (issue #34) stays as the separate user hook, applied after theme post-FX.
4. Perf note: chain shares RTs with R8; both are allocated lazily and resized on backbuffer change.

### R10 Pixel snapping policy
`Theme.PixelSnapping : bool` — when true (retro themes): round all draw positions to ints (`PointF.ToInts` already exists), `SamplerState.PointClamp/PointWrap`, radius forced to 0 or hard-stepped, AA smoothstep width 0. When false: `LinearClamp`, subpixel positions, 1px AA. Plumb through `RenderContext`.

---

## §3 Phase T — Theming architecture

### T1 `ThemePalette` in the library
Move `DemoThemePalette`'s semantic slots into `src/MonoGame.PortableUI/ThemePalette.cs` unchanged: `Background, Surface, SurfaceAlt, Text, HeadingText, MutedText, Primary, Secondary, Warning, Danger, Info, Selection, SelectionText, TabText, SelectedTabText, FieldFrame, FieldBorder, DisabledSurface, DisabledText` + optional `Brush?` overrides. Demo type becomes an alias/removed.

### T2 Style system
1. `class StateStyle { Brush? Background; Brush? BorderBrush; Thickness BorderThickness; CornerRadius CornerRadius; ShadowStyle? Shadow; Color? TextColor; }`
2. `class ControlStyle { StateStyle Normal, Hover, Pressed, Focused, Disabled, Checked; TimeSpan TransitionDuration; }` — unset state values fall back to `Normal` (nullable fields).
3. `PortableTheme` v2 composition: `ThemePalette Palette; Typography Typography; ThemeMetrics Metrics; ControlStyle Button, TextBox, CheckBox, RadioButton, ToggleButton, ComboBox, ListBoxItem, Tab, ToolTip, ContextMenu, ScrollBar, Slider, ProgressBar, Panel; IReadOnlyList<PostEffect> PostEffects; bool PixelSnapping;`
4. Helper `ControlStyleBuilder.FromPalette(palette)` generates sensible defaults for all controls from the 19 palette slots — a new theme is *palette + overrides*, not 60 properties. (This generalizes today's `DemoThemeRegistry.CreateTheme`, lines 533-612.)
5. DECIDED T2 — **clean break** from the flat `PortableTheme` properties (library is pre-1.0/alpha).
   - Migration is one atomic PR: delete the ~60 flat props; every control's theme reads switch to style paths (`theme.Button.Normal.Background` etc.); `DemoThemeRegistry.CreateTheme` mapping (lines 533-612) is replaced by `ControlStyleBuilder.FromPalette` + per-theme overrides; `ThemeRegressionTests` rewritten against styles.
   - Version signal: bump minor + release-notes "BREAKING: PortableTheme v2" section listing old→new property paths (a table, generated while migrating — cheap insurance for any early adopter).
   - `PortableTheme.CreateDefault()` keeps its exact current visual output (assert via Q2 screenshot once available) so the default look doesn't shift under consumers.

### T3 Live theme switching — DECIDED: resolve at draw/use time
Controls stop copying theme values in constructors; they hold no style state and read `Theme.Button.Hover.Background` etc. inside `OnDraw`/`ChangeVisualState`. Setting `ScreenEngineOptions.Theme` is instantly visible; only a layout invalidation is needed (padding/font may change). Per-frame cost ≈ property reads; zero events. Per-control overrides stay possible via nullable local props (`Control.Style`), checked before the theme — **a manually set property must never be overwritten by a global or island theme change** (resolution checks local first, always).

Implementation details:
1. Central resolver `Control.ResolveTheme()` (see T3.5 for the chain); controls call `ResolveTheme().Button.Hover.Background` — one virtual-free path, resolution result cacheable per frame (invalidated by a global theme version counter incremented on any theme/island assignment).
2. Layout-affecting theme values (padding, font, text size) require `Screen.InvalidateLayout()` on theme assignment — the `Options.Theme` setter and `ThemeIsland.Theme` setter both do this.
3. Switching preserves **all** control state by construction: focus, text-box content/cursor/selection, scroll offsets, tab selection — nothing is rebuilt. (Test in Q1.)

### T3.5 Theme Islands (per Theme-Plan-Review — first-class subtree theming)
A **`ThemeIsland`** is the public API for using multiple themes in one screen (game HUD in `aurora` + inventory in `parchment` + in-world computer in `dos`). The D1 gallery previews use this same public API, not a demo trick. `ThemeScope` remains at most an internal implementation term.

1. **Model**: `public class ThemeIsland : ContentControl { public PortableTheme? Theme { get; set; } }` — a visually transparent container (no background, no border by default; it only changes theme resolution for descendants). Container-control form because layout, draw, input, popups and generated children are all `Control`-based paths today.
2. **Resolution rule** (explicit, in this order): nearest ancestor `ThemeIsland.Theme` that is non-null → … outer islands … → `ScreenEngineOptions.Theme` → `PortableTheme.CreateDefault()` fallback. Implemented as a parent-chain walk in `Control.ResolveTheme()`; cached per control with the version-counter invalidation from T3.1 (assigning any island's `Theme` bumps the counter — cheap, no tree walk on assignment).
3. **Live switching**: assigning `ThemeIsland.Theme` restyles the subtree next frame without rebuild; assigning `ScreenEngineOptions.Theme` restyles everything *outside* islands. Both preserve focus/text/selection/scroll (Q1 tests).
4. **Overlay inheritance**: tooltips, context menus, combo-box dropdowns and flyout content resolve their theme from the **owner control that opened them**, not from the screen root — `FlyOut`/`ToolTipPopup` get an `Owner` control reference and their `ResolveTheme()` delegates to `Owner.ResolveTheme()`. (The dos computer's dropdown must look DOS even though the flyout layer hangs off the screen.)
5. **Generated children**: Button's internal text block, tab header buttons, list-box item buttons resolve through their `Parent` chain like any control — works automatically once T3 removes ctor snapshots, including children created *after* the parent joined an island. Guard with an explicit test.
6. **Nesting**: inner island wins for its descendants; siblings keep the outer/global theme. `Theme = null` makes an island transparent (pure pass-through).
7. **Q1 test additions** (from the review): nested-island resolution; runtime island switch preserves focus/text/selection/scroll; manual property overrides survive global *and* island theme changes; later-added descendants inherit correctly; popup content inherits the opener's island theme.
8. **Sequencing note**: T3.5 does not require the full ThemePalette/ControlStyle migration (T1/T2) — a useful first version can ship against the current flat `PortableTheme` as soon as T3's draw-time resolution exists.

### T4 State transitions & easing
1. `ColorAnimationTween` alongside the existing `Vector2`/`Double` tweens; animate `Color.Lerp`.
2. Brush crossfade: during a state change, draw old state brush at `1-t` and new at `t` for `TransitionDuration` (default 120ms, retro themes 0ms). Driven by the existing tween clock (`Control.UpdateAnimations`).
3. Extend `Easings` with QuadIn/Out/InOut, ExpoOut, BackOut, ElasticOut, BounceOut (standard Penner formulas) — needed for Metro press-tilt, Material ripple, Aqua bounce.
4. Material **ripple**: `RippleAnimation` on Button press — radial gradient circle (R6.2) expanding from the press point (via `BrushContext.PointerPosition`), alpha fading, clipped by the control's rounded SDF.

### T5 Theme registry & packaging — DECIDED: separate `MonoGame.PortableUI.Themes` NuGet
Registry: `static class PortableThemes { static IReadOnlyList<ThemeDefinition> All; static ThemeDefinition? Find(string id); }`; `ThemeDefinition { string Id, DisplayName; ThemeMetadata Meta; Func<PortableTheme> Create; }`. Demo keeps only its CLI/env resolution on top.

Packaging details:
1. New project `src/MonoGame.PortableUI.Themes/` referencing core; added to `MonoGame.PortableUI.slnx`, central package versions, same SourceLink/tag-versioning as core. One file per theme (`Themes/Win95Theme.cs`, …) + `PortableThemes.cs` registry.
2. Split of types: **core** keeps `PortableTheme`, `ThemePalette`, `ControlStyle`, `ThemeDefinition`, `ThemeMetadata`, `ThemeIsland` (the *mechanism*); the **Themes package** contains the 30+ definitions + `ControlStyleBuilder` era helpers (bevel builder, TUI frame builder) (the *content*). Core's `CreateDefault()` stays in core so core works standalone.
3. Theme **textures** (dither/pinstripe patterns, parchment frames): tiny procedural patterns are generated in code (no content pipeline); authored PNGs (parchment, noise) ship as embedded resources loaded via `Texture2D.FromStream` — works without mgcb.
4. **Fonts** — DECIDED: content-template approach (fonts cannot ship pre-built via mgcb from a NuGet).
   - The Themes package ships `contentFiles/any/any/ThemeContent/` (NuGet contentFiles mechanism → appears in the consumer's project on restore): `.ttf` files + license files + `themes-fonts.mgcb-snippet.txt` with ready-to-paste `#begin`/processor blocks matching the `FontManager` naming convention (`Fonts/{name}-{style}-{size}`).
   - Consumer setup = copy fonts into their `Content/`, paste the snippet into their `.mgcb`, call `FontManager.LoadFonts(game, PortableThemes.FontNames)`. Document as a 3-step README section in the Themes package.
   - Graceful degradation: `ThemeMetadata.FontName` falls back to `FontManager.DefaultFont` when the themed font isn't built — themes render with correct colors/shapes, wrong font, and `FontManager` logs one warning per missing family (no exception). `ThemeMetadata` exposes `RequiredFontFamilies` so apps can pre-check.
   - The demo keeps building all fonts via its own `Content.mgcb` as today, doubling as the reference integration.
5. Demo and tests reference the Themes project directly; `DemoThemeRegistryTests` becomes `ThemesRegistryTests` in the main test project.

### T6 Typography & fonts
1. **T6.1 Lazy loading**: `FontManager.GetFont` probes+loads on first request per (family, style, size); `LoadFonts` just registers names. Cache as today.
2. `class Typography { string FontFamily; float BaseSize; float HeadingScale; float SmallScale; FontStyle HeadingStyle; }` on the theme.
3. New fonts (all OFL unless noted), added to `Content.mgcb` like the existing 7:
   - **Selawik** (Microsoft's OFL Segoe-metric font) — Metro, Fluent, Aero, XP-ish
   - **Roboto** — Material 3
   - **VT323** — Phosphor/Amber CRT
   - **Orbitron** — Sci-Fi HUD, Cyberpunk
   - **MedievalSharp** — RPG Parchment (DEFAULT: cleaner than IM Fell at 12–16px SpriteFont sizes; IM Fell dropped)
   - **W95FA** — Win95/NeXT/BeOS (DEFAULT, with a hard gate: verify its freeware license permits repo+NuGet redistribution at implementation time; if not, fall back to **FreePixel**, else Selawik)
   - **ChiKareGo2** (free, Chicago-like) — Mac 1-bit / OS 9
   - **Topaz a1200-style** (check license; Amiga CR) — else keep jersey10
   - Existing: pressstart2p (C64/NES), silkscreen (GB), px437ibmvga8x16 (DOS/Norton), ibmplexmono (terminal), atkinsonhyperlegible (studio/glass/aurora), jersey10 (amiga)
4. Store each font's license file next to the `.ttf` as today (`<None Update>` copy).

### T7 Theme metadata
`class ThemeMetadata { string Era; bool IsDark; Color[] PreviewSwatches; bool ReducedMotion; string Description; }` — powers gallery cards (D1), README table (D7), and lets E-Ink disable animations globally (`TransitionDuration=0`, no tweens).

---

## §4 Phase C — Controls & bug fixes

- **C1 Chrome properties everywhere**: `Control` gains `CornerRadius`, `BorderBrush`, `BorderThickness` (style-resolved, locally overridable). Base `OnDraw` renders background+border via R4; `Border` control remains as the WPF-style explicit decorator. Button/TextBox/ComboBox/Tab drop their bespoke frame code in favor of the shared path.
- **C2.1 Slider**: `Minimum/Maximum/Value/Step`, horizontal/vertical; track = rounded rect (theme `Slider.Normal`), filled portion = `Primary`, thumb = draggable rounded rect/circle with hover/pressed states; mouse capture like ScrollViewer thumb; keyboard arrows; `ValueChanged` event.
- **C2.2 ProgressBar**: `Minimum/Maximum/Value`, `IsIndeterminate` (reuses `ProgressIndicator` dots or a sweeping bar); track+fill from style. DECIDED: `ProgressBarStyle.Segmented` flag — fill drawn as N blocks with a gap (`Metrics.ProgressSegmentWidth`/`ProgressSegmentGap`, e.g. 8px/2px); partial last block is dropped, not clipped (authentic DOS/Win95 chunk behavior); smooth mode remains the default for modern themes.
- **C3.1 Grid spans**: distribute a spanning child's measured excess across its auto tracks proportionally (WPF-ish) instead of skipping (`Grid.cs:130,164,253`).
- **C3.2 TextBox measuring**: cache per-line prefix widths (`float[] _prefixWidths`, invalidated on edit of that line); `GetPositionForX` becomes binary search over prefix widths.
- **C3.3 Text input**: subscribe `Game.Window.TextInput` in `ScreenEngine.Initialize`; route chars to the focused control; keep `Keys` polling for navigation/commands only. Removes the US-layout table (`Screen.cs:575`).
- **C3.4 Templating — DECIDED: implement minimal `Template`** (revives the dead scaffolding instead of deleting it).
  - `ContentControl.Template : Func<Control>?` — when set, the factory's control tree *replaces* the control's default visual (default = the built-in `OnDraw` chrome + content layout). Instantiated lazily on first measure, cached, re-instantiated when the property changes (`InvalidateLayout(true)`).
  - `ContentPresenter` becomes real: constructed with (or attached to) the owning `ContentControl`, it measures/arranges/draws `Owner.Content` at its own slot — the "put the content here" marker inside a template, mirroring WPF.
  - Scope guard: template children are normal controls (theme resolution via parent chain → works with `ThemeIsland` automatically; input/hit-testing via the existing tree walk). Templated control keeps owning its input events and visual states; the template only swaps visuals. No `TemplateBinding`/triggers — styles (T2) remain the styling mechanism; templates are for *structure* (e.g. a game health-bar button with icon slots).
  - First consumers: `Button` (re-enable the commented scaffolding, `Button.cs:42-45`) and the D2 demo gets one custom-templated button as the reference sample. Q1 test: templated button raises Click, presents Content via presenter, restyles on theme switch.
- **C4 Measure caching**: `Control` caches `MeasureLayout()` result + a `_measureValid` flag invalidated by `InvalidateLayout`; Grid/StackPanel reuse the cached value within one pass (fixes double/quadruple measures).
- **C5 Styleable indicators**: `CheckBox` glyph kind (`Checkmark | Cross | FilledBox`) drawn as 2 AA line segments via the R4 shader (or font glyph fallback); RadioButton dot = R6.2 circle; `FocusVisualKind { Border, Dotted, Glow, ThickInset }` on the style — Dotted = TileBrush 2×2 pattern border (Win95), Glow = shadow in accent color (neon/fluent).

---

## §5 Theme catalog (35 core ids)

Format — **id · Name (status)** | font | radius | key colors | specification. Wave = feature dependency (see §0).

### Retro 8-bit
1. **c64 · C64 (existing, CRs)** | pressstart2p | r0 | bg `#40318D`, frame `#7869C4`, text `#FFFFFF`, accents green/yellow/red/cyan | CRs: outer PETSCII-style screen frame (thick `#7869C4` border around the whole screen, like a C64 display); blinking block cursor in TextBox (block invert, 500ms); optional subtle scanlines (R9) — DEFAULT: off, exposed as a theme flag; PixelSnapping on; 2px chunky borders everywhere; ListBox selection = full-row invert. Wave 1.
2. **gameboy · Game Boy DMG (existing, CRs)** | silkscreen | r0 | `#0F380F`/`#306230`/`#8BAC0F`/`#9BBC0F` only | CRs: enforce strict 4-shade palette (audit every slot); DotMatrix post-FX (R9) simulating the LCD grid; hard 1-shade-darker offset shadows (2px, Blur 0); pressed = shift content 1px down-right; PixelSnapping. Wave 1 (+R9 for grid).
3. **nes · NES 8-Bit (new)** | pressstart2p | r0 | bg `#000000`, blue `#3CBCFC`, red `#F83800`, white `#FCFCFC`, gray `#7C7C7C` | 4px thick white borders (double border: white outer, blue inner); buttons = red with white text, pressed inverts; selection = blue block; hard shadows; TextBox = white on black with block cursor. Wave 1.
4. **mac1bit · Mac System 1-bit (new)** | ChiKareGo2 | r=4 top corners only (classic Mac window curve), else 0 | pure `#FFFFFF`/`#000000` | 50% dither patterns via 2×2 `TileBrush` (R6.4) for disabled surfaces & scrollbar gutters; 1px black borders; solid black offset shadow (2,2); pressed = full invert; radio/check drawn 1-bit style; PixelSnapping. Wave 1 (+R6.4).

### Text-mode
5. **dos · DOS / Turbo Vision (existing, CRs)** | px437ibmvga8x16 | r0 | desktop `#0000A8`, dialog `#C0C0C0`, cyan `#00A8A8`, yellow `#FCFC54`, black | CRs: double-line box-drawing borders — DEFAULT: (rec) `NineTileBrush` with a pre-rendered CP437 double-line frame texture (authentic glyph look, one texture); half-cell solid black shadow right+bottom of dialogs (offset 8,4, Blur 0); menu hotkey letters yellow; scrollbar = block-character style (▒ track via dither TileBrush, ■ thumb); blinking underscore cursor. Wave 1 (+R6.5). **Flagship world-space theme** (Phase W): dos + CRT post-FX (R9) on a `UISurface` = the in-game computer for the adventure-game use case — keep every dos visual free of screen-size assumptions (frame/shadow sizes in virtual units) so it renders correctly at e.g. 640×400 on a prop monitor.
6. **norton · Norton Blue TUI (new)** | px437ibmvga8x16 | r0 | panel `#0000A8`, frame cyan `#00A8A8`, text `#FFFFFF`, selection `#00A8A8`/black text, fkey bar black+red | Twin-panel look: single-line frames, cyan on blue; bottom status strip styled as F-key bar; selection = cyan block; same shadow/cursor infra as dos. Wave 1.
7. **phosphor · Green Phosphor CRT (new)** | VT323 | r0 | bg `#001100`, text `#33FF33`, dim `#1A801A` | Everything monochrome green, 1px green borders; **Bloom** post-FX for glyph glow + **Scanlines** + **CRTBarrel** (R9); block cursor blink; focus = brighter glow (C5 Glow); selection = green block/black text. Wave 3 (R9).
8. **amber · Amber Terminal (new)** | VT323 | r0 | bg `#1A0F00`, text `#FFB000`, dim `#805800` | Same infra as phosphor with amber palette; optional slow flicker (opacity noise ±2%) — DEFAULT: off (motion-sensitivity), exposed as a theme flag. Wave 3 (shares phosphor's FX).

### 16/32-bit GUI
9. **amiga · Amiga Workbench (existing, CRs)** | jersey10 (or Topaz-style, T6) | r0 | WB1.3 four colors: blue `#0055AA`, white `#FFFFFF`, black `#000000`, orange `#FF8800` | CRs: strict 4-color audit; bevel borders — 2px white top+left / black bottom+right (raised), inverted when pressed; window-title checkerboard pattern via `TileBrush` on panel headers; scrollbar with chunky orange thumb + arrow buttons look. Wave 1 (+R6.4).
10. **win95 · Windows 95 (new)** | W95FA/FreePixel (T6) | r0 | face `#C0C0C0`, highlight `#FFFFFF`, shadow `#808080`, darkshadow `#404040`, title `#000080→#1084D0` gradient, desktop `#008080` | Classic 3D bevels: outer 1px darkshadow/highlight + inner 1px shadow/white (4-line bevel, drawn as border rects — no shader needed); pressed = bevel invert + content shift 1px; **dotted focus rect** via 2×2 TileBrush border (C5 Dotted); title-bar gradient on panel headers (R6.1 2-stop horizontal); TextBox = sunken bevel white field; scrollbar with bevel buttons. Wave 1.
11. **macos9 · Mac OS 9 Platinum (new)** | ChiKareGo2 or atkinsonhyperlegible | r=6 (lozenge buttons r=height/2) | platinum `#CCCCCC`, stripe `#BBBBBB`, accent lavender `#6666CC`, text black | Horizontal pinstripes on headers (TileBrush 1×2); buttons = subtle vertical gradient + 1px `#888` border, lozenge-rounded; default-button = lavender ring; scrollbar = platinum with directional arrows both ends. Wave 2 (R6.1/R6.4).
12. **nextstep · NeXTSTEP (new — promoted from stretch)** | Selawik | r0 | grays `#AAAAAA`/`#555555`, black titles, dark chiseled bevels | Deep-gray chisel bevels (reuses the Win95 bevel style builder with darker ramp: highlight `#D0D0D0`, shadow `#303030`); black title strips with white text on panel headers; wide scrollbar (Metrics override) with the classic bottom-corner arrow pair; pressed = bevel invert; focus = white 1px inset. Wave 1 (shares win95 infra).
13. **beos · BeOS R5 (new — promoted from stretch)** | Selawik | r0 (tab corners r=4) | face `#D8D8D8`, tab yellow `#FFCB00`, accent blue `#336698`, bevel light `#FFFFFF`/dark `#9A9A9A` | Signature **yellow tab headers** — TabControl style: selected tab = yellow rounded-top block wider than content, unselected = gray; soft 1px bevels on buttons (gentler than win95: 1-line not 2); blue determinate ProgressBar; checkbox = blue checkmark on white sunken box. Wave 1 (shares win95 infra).

### 2000s gloss
14. **luna · Windows XP Luna (new)** | Selawik | r=3 | chrome `#3C81F3→#1941A5` vertical gradient, button face `#F4F3EE`, border `#003C74`, hover glow orange `#F9B233`, start green `#3AAA35` | Buttons: cream face + 1px navy border + top white sheen strip (R6.1 2-stop overlay upper 40%); hover = orange inner glow (R7 shadow inset, accent color); panel headers = blue gradient chrome with white bold text; TabControl = Luna tab shapes (top-rounded); scrollbar = gradient thumb with grip dots. Wave 2.
15. **aqua · macOS Aqua (new)** | atkinsonhyperlegible | r=height/2 buttons (pill/gel), panels r=8 | pinstripe bg `#F0F0F0`/`#E8E8E8`, gel blue `#88BFFC→#3B88FD`, traffic `#FF5F57`/`#FEBC2E`/`#28C840` | **Gel buttons**: multi-stop vertical gradient (light→saturated→light, R6.1) + white top-half highlight overlay (rounded, alpha 0.55→0) + soft drop shadow (R7 Blur 4); pinstripe background via TileBrush; focused field = blue glow ring (C5 Glow); ScrollBar = gel-blue pill thumb in white trough; CheckBox/Radio gel style. Wave 2.
16. **aero · Windows Aero / 7 (new)** | Selawik | r=6 | glass tint `#B8D6FB` α~0.55, accent `#2FA6DE`, text `#1E395B`, sheen white | Panel chrome = light **backdrop blur** (R8) with white diagonal sheen (R6.1 diagonal, alpha band); hover = soft radial glow following pointer (R6.2 + `PointerPosition`); buttons = subtle vertical gradient + 1px `#5A8BB0` border + inner white 1px line; blue-green selection gradient. Wave 3 (R8).

### Flat era
17. **metro · Metro / Windows 8 (new, required)** | Selawik (Light for headings) | r0 strictly | bg `#1D1D1D` — DEFAULT: dark only (light variant skipped; trivial to add later as a palette twin à la solarized), tiles `#1BA1E2` `#E51400` `#60A917` `#F09609` `#AA00FF`, text white | Pure flat: zero radius, zero gradients, zero shadows; typography-first (Typography.HeadingScale ≈ 2.2, light weight); buttons = flat tile colors, hover = 2px white border only, **pressed = tilt** (existing Scale + small Translation toward click point, Easings.QuadOut); selection = accent block; ProgressBar = flat accent bar; generous padding metrics. Wave 1.
18. **fluent · Fluent / Windows 10-11 (new, required)** | Selawik | r=4 (controls) / 8 (panels/flyouts) | accent `#0078D4`, acrylic dark `#2C2C2C` α0.8 + noise, text `#FFFFFF`, stroke white α0.08 | **Acrylic** surfaces = R8 backdrop blur + tint + luminosity + the FrostedGlass noise overlay; **Reveal highlight**: hover border/background = radial gradient centered at `BrushContext.PointerPosition` (R6.2), fading with distance; 1px subtle stroke on cards; soft shadows (R7 Level2) on flyouts; focus = 2px white ring outside 1px black (Win11 style); state transitions 150ms. Wave 3 (R8).
19. **material · Material Design 3 (new, required)** | Roboto | r=12 buttons (pill FAB r=height/2), fields r=4 top | primary `#6750A4`, on-primary `#FFFFFF`, surface `#FFFBFE`, on-surface `#1C1B1F`, surface-variant `#E7E0EC`, error `#B3261E` — DEFAULT: light only (dark variant skipped; add later as a palette twin if wanted) | **Elevation shadows** R7 Levels 1–5 (cards L1, buttons hover L2, flyouts L3); **ripple** on press (T4.4: expanding radial circle from press point, α 0.12, clipped to rounded shape); **state layers**: hover = on-color α0.08 overlay, pressed α0.12; filled/tonal/outlined button styles mapped to Primary/Secondary/Danger buttons in the demo; TextBox = filled field with animated 2px bottom accent line on focus. Wave 2 (+T4.4). 
### Modern glass
20. **glass · Frosted Glass (existing, CRs)** | atkinsonhyperlegible | r=12 | night-blue backdrop, cyan/coral/amber/violet accents (keep current palette) | CRs: replace fake CPU-noise frost with **real backdrop blur** (R8) keeping the grain overlay for texture; 1px white α0.25 border + brighter top edge (keep GlassStackPanel's sheen idea, move into style); soft shadow R7 (Blur 16, α0.35); `GlassBackdropBrush` fate — DEFAULT: (rec) promote a generalized `DecorativeBackdropBrush` (params for gradient base, soft bands, underlay cards, grid) into the Themes package so glass/vaporwave/liquid all reuse it; retire `GlassStackPanel` once shadows/borders come from styles. Wave 3.
21. **liquid · Liquid Glass (new, required)** | atkinsonhyperlegible | r=squircle 20 (R8.5) | near-transparent surfaces over vivid animated backdrop; text — DEFAULT: fixed light text for v1 (adaptive light/dark by backdrop luma deferred: needs an average-luma estimate of the blurred RT region — doable later via a 1×1 mip/downsample readback, listed as a liquid v2 enhancement) | Full R8.5 stack: edge **refraction** rim, slow **specular sweep**, backdrop saturation boost; buttons = floating glass pills with strong lensing; pressed = brief scale 0.97 + highlight flash; needs an animated colorful `BackgroundBrush` (drifting radial blobs, R6.2 + time) to show off refraction. Wave 3 (flagship).
22. **aurora · Aurora Modern (existing, CRs)** | atkinsonhyperlegible | r=10 | dark teal base (keep), aurora green/violet/pink accents | CRs: upgrade all 2-stop gradients to **multi-stop** (R6.1); slow hue drift on the background gradient (time-driven stop colors, ~60s cycle, honors `ReducedMotion`); accent-colored **glow shadows** on primary buttons (R7, accent color, Blur 12); backdrop = 2–3 large blurred radial blobs (R6.2) instead of flat gradient; subtle grain overlay. Wave 2 (blobs) / gains more in Wave 3.

### Stylized
23. **terminal · Terminal Glass (existing, CRs)** | ibmplexmono | r=6 | graphite `#16191C`, neon green `#39FF14`, cyan accent | CRs: subtle backdrop blur on panels (R8, low radius); neon green **focus glow** (C5 Glow); faint scanlines (R9, α0.04); selection = green α0.25; caret = green block. Wave 3.
24. **studio · Soft Studio (existing, CRs)** | atkinsonhyperlegible | r=14 | paper `#F7F4EF`, ink `#2A2A2A`, blue `#3E6FB0`, coral `#E2725B` | CRs: neumorphic-lite dual shadows on raised panels (light `#FFFFFF` top-left + soft gray `#D9D4CB` bottom-right, R7 ×2); paper-grain background texture (ImageBrush tile, R6.3, tiny noise PNG asset); refined hover = coral underline/border instead of fill swap. Wave 2.
25. **cyberpunk · Cyberpunk Neon (new)** | Orbitron | r0 + **cut corners** (one corner clipped 45°) — DEFAULT: (rec) `CornerStyle.Cut` from the R8.5 corner-style enum (no extra work now that squircle is in) | bg `#0A0A12`, yellow `#FCEE0A`, cyan `#00F0FF`, magenta `#FF003C` | Neon 1px borders with **bloom glow** (R9 bloom or R7 accent glow); hover = brief glitch (2-frame ±2px RGB-split offset, honors ReducedMotion); scanlines α0.06; selection = yellow block/black text; headers uppercase tracked-out. Wave 3.
26. **vaporwave · Vaporwave Sunset (new)** | Orbitron headings / atkinsonhyperlegible body | r=8 | pink `#FF71CE`, blue `#01CDFE`, mint `#05FFA1`, purple `#B967FF`, bg `#1A1030` | Sunset multi-stop gradient surfaces (pink→purple→blue, R6.1); **horizon-grid backdrop** (perspective grid — reuse/param the `GlassBackdropBrush` grid at an angle, or dedicated brush); glow shadows in pink; selection mint; optional slow gradient shift. Wave 2.
27. **nord · Nord (new)** | atkinsonhyperlegible / ibmplexmono for fields | r=6 | polar `#2E3440` `#3B4252` `#434C5E`, snow `#D8DEE9` `#ECEFF4`, frost `#88C0D0` `#81A1C1` `#5E81AC`, aurora red `#BF616A` | Calm flat: 1px `#4C566A` borders, frost-blue primary, minimal shadows (R7 Level1), 100ms transitions; selection `#88C0D0` α0.3. Wave 1.
28. **dracula · Dracula (new)** | ibmplexmono headings / atkinsonhyperlegible | r=6 | bg `#282A36`, line `#44475A`, fg `#F8F8F2`, purple `#BD93F9`, pink `#FF79C6`, green `#50FA7B`, cyan `#8BE9FD`, red `#FF5555` | Code-editor vibe: purple primary, pink hover accents, green success/toggle-on, `#44475A` selection; 1px line borders; subtle shadows. Wave 1.
29. **solarized · Solarized Light + Dark (new, one definition, two ids `solarized-light`/`solarized-dark`)** | ibmplexmono | r=4 | shared accents blue `#268BD2` cyan `#2AA198` yellow `#B58900` red `#DC322F`; light: bg `#FDF6E3` surface `#EEE8D5` text `#657B83`; dark: bg `#002B36` surface `#073642` text `#839496` | Single `SolarizedTheme(bool dark)` factory proving palette-driven theming (T2.4); flat, 1px borders, no shadows. Wave 1.
30. **gruvbox · Gruvbox (new)** | ibmplexmono | r=4 | bg `#282828`, surface `#3C3836`, fg `#EBDBB2`, orange `#D65D0E`, yellow `#D79921`, aqua `#689D6A`, red `#CC241D` | Warm retro-dark; orange primary, aqua secondary; slightly grainy background (ImageBrush noise, α0.03). Wave 1 (grain → Wave 2).
31. **parchment · RPG Parchment (new)** | IM Fell English / MedievalSharp (T6) | r=4 | parchment `#E8D8B0`, ink `#3B2A18`, wood `#5C3A1E`, gold `#C9A227`, wax red `#8B0000` | Parchment paper background (ImageBrush, R6.3); **ornate NineTileBrush frames** (R6.5) for panels/buttons (gold-corner wood frame); pressed = darkened center; asset task: source CC0 UI pack (Kenney "Fantasy UI Borders" or hand-draw 3 frame textures ~48×48); selection = wax-red banner; ProgressBar = gold-filled ornate trough. Wave 2 (most asset-heavy — the showcase for texture theming).
32. **lcars · Sci-Fi HUD / LCARS-style (new)** | Orbitron (condensed feel) | **asymmetric per-corner radius** (left end pill r=height/2, right square — proves `CornerRadius` 4-value support) | black bg, orange `#FF9C00`, peach `#FFCC99`, lilac `#CC99CC`, blue `#9999FF`, red alert `#CC6666` | End-cap bars: buttons/tabs = pill-one-side blocks in rotating palette colors, black text, uppercase; panel frames = thick L-shaped bars (Grid + corner blocks); no shadows/gradients — pure bold flat shapes; hover = brighten 15%, pressed = white flash 80ms. Wave 1 (needs per-corner R4).
33. **eink · E-Ink Paper (new)** | atkinsonhyperlegible (DEFAULT; serif option dropped) | r=3 | paper `#F5F2EA`, ink `#333333`, mid-gray `#8A8680` only | Grayscale-only audit; `ReducedMotion` = true (TransitionDuration 0, no tweens, no post-FX); dither pattern (TileBrush) for the few mid-tones; 1px ink borders; selection = inverted block; pressed = instant invert, no animation — demonstrates the accessibility flags. Wave 1.
34. **neumorphic · Neumorphism (new)** | atkinsonhyperlegible | r=16 | single hue `#E0E5EC`, text `#4A5568`, accent `#5B7FFF`, shadows `#A3B1C6`/`#FFFFFF` | Extruded look: **dual shadows** (dark bottom-right + white top-left, R7 ×2, Blur 12); pressed = **inset** shadows (invert offsets — needs `ShadowStyle.Inset` flag on R7); fields = inset by default; low-contrast warning noted in metadata description (a11y). Wave 2.
35. **brutalist · Brutalist (new)** | atkinsonhyperlegible bold (DEFAULT; no extra display font — one less mgcb family) | r0 | white `#FFFFFF`, black `#000000`, accent `#FF3300` | 3px solid black borders; **hard offset shadows** (solid black, offset 6,6, Blur 0); hover = full color inversion; pressed = shadow collapses to 2,2 + control shifts 4,4 (physical push); huge bold uppercase headings; no easing (linear 60ms). Wave 1.

**Count: 35 core ids** (31 entries; solarized = 2 ids; NeXTSTEP + BeOS promoted from stretch) — comfortably ≥ 25. All 8 existing ids kept; all user-named themes present (C64 ✓, DOS Turbo Vision ✓, frosted glass ✓, Liquid Glass ✓, Windows 8 ✓ metro, Windows 10 ✓ fluent, Material ✓).

---

## §6 Phase D — Demo project improvements

### D1 Theme Gallery screen
1. New `GalleryScreen : Screen`, reachable from the header; `ScrollViewer` + wrap-style `Grid` of theme cards (~260×170).
2. Each card: theme name + era chip (T7 metadata) + **live mini-preview** — a small control tree (button, checkbox, textbox, progress) wrapped in a **`ThemeIsland`** (T3.5) with that theme assigned. This is the same public API consumers use — the gallery doubles as the Theme-Island integration test. Fallback — APPROVED: if profiling shows the ~35 live islands too slow (unlikely, ≈220 controls, less than the Stress tab), pre-render each card's subtree once into a small `RenderTarget2D` thumbnail (~232×120) on first visibility, redraw only when the theme version counter bumps or the card resizes; W1's `UISurface` provides exactly this mechanism for free.
3. Click card → `ScreenEngine.Options.Theme = def.Create()` — instant with T3, no rebuild.
4. Filter chips by era/dark/light (uses T7).

### D2 Showcase tabs (MainScreen)
1. Controls tab: add `Slider` + `ProgressBar` (determinate + indeterminate) panels; wire to `_status` like the rest.
2. New **Visual FX tab**: live sliders bound to a sample panel — corner radius 0–24, shadow blur/offset, blur radius, gradient angle, opacity — the R-feature playground and manual test surface.
3. New **Motion tab**: buttons triggering each easing on a moving box; state-transition duration slider.
4. Keep Stress tab; add a themed-heavy variant (500 rounded+shadowed controls) as the R-perf canary.

### D3 Theme inspector
Side panel (toggle from header): list `ThemePalette` slots as color swatches; click → RGB sliders editing a mutable palette copy; "Apply" regenerates styles via `ControlStyleBuilder.FromPalette`; "Copy C#" serializes the palette/overrides as compilable theme code to the clipboard (uses existing `IClipboardService`).

### D4 Screenshot mode
`--screenshot <dir>` CLI (parsed next to `--theme`): headless-ish run — for each registered theme: apply, `RunOneFrame`/manual `Update+Draw` into a `RenderTarget2D` (1180×760), `SaveAsPng($"{dir}/{id}.png")`, exit. Also `--screenshot-screen gallery|main|second`. Feeds D7 and Q2.

### D5 launchSettings — DECIDED: relax the test, keep ~8 representative profiles
Invert `Launch_settings_include_profile_for_each_demo_preset`: instead of "every preset has a profile", assert "every profile's `--theme` id exists in the registry" (catches typos/renames, allows a curated subset). Keep one profile per era group (c64, dos, win95, aqua, metro, material, glass, liquid) so each rendering wave has a one-click launch. Add a `--screenshot` profile once D4 lands.

### D6 Debug overlay
F3 toggles an overlay TextBlock (top-right, drawn last): FPS (frame-time EMA), SpriteBatch flush count (counter incremented in the batching wrapper, R2), layout passes/frame, blur passes/frame. Cheap and permanent.

### D7 README gallery
Grid of D4 screenshots (`docs/themes/*.png`), one row per era, linked from the top of the README — the project's shop window. Regenerate via `dotnet run -- --screenshot docs/themes`.

---

## §7 Phase Q — Tests & benchmarks

- **Q1 Unit (headless, existing patterns)**: brush param/clamp tests for every new brush (mirror `BrushRegressionTests`); `CornerRadius` struct ops; `ShadowStyle` defaults; style fallback resolution (unset Hover → Normal); `ControlStyleBuilder.FromPalette` maps every slot; registry completeness — all ids unique, metadata non-empty, `Create()` returns for **all** themes (extend `DemoThemeRegistryTests`); solarized twins share accents; E-Ink has `ReducedMotion`; live-switch test: change theme, assert a control resolves new values without rebuild (T3); the full ThemeIsland suite from T3.5.7 (nesting, state preservation, override survival, late-added children, popup inheritance); Phase-W mapper/input-routing tests (see W-order notes).
- **Q2 Visual regression — DECIDED: local-only screenshot-diff harness on top of D4.**
  1. Baselines checked in under `tests/MonoGame.PortableUI.Tests/VisualBaselines/{themeId}.png` (1180×760, captured via `--screenshot`); regenerate deliberately with `eng/update-visual-baselines.ps1` (runs the demo in screenshot mode into the baselines folder).
  2. Diff runner `eng/visual-diff.ps1`: runs `--screenshot` into a temp dir, then per theme computes per-pixel RMSE (ignore-alpha; optional 1px erode to tolerate AA jitter); fail threshold ~1% RMSE or >0.1% pixels differing beyond ±8/255. On failure, write `{id}.diff.png` (abs-difference, amplified) next to the report for eyeballing.
  3. Also expressible as MSTest tests marked `[TestCategory("Visual")]` that shell out to the demo — excluded by default (`dotnet test --filter TestCategory!=Visual`) so CI stays green without a GPU; developers run them before merging visual-affecting PRs.
  4. Time-animated brushes (aurora drift, liquid specular) must render deterministically in screenshot mode: `--screenshot` pins `ScreenSystem.TotalTime` to a fixed value.
  5. Optional later: try the harness on a GPU-less CI runner (llvmpipe); if it happens to produce stable output, promote to a nightly job — no commitment now.
- **Q3 Benchmarks** (`benchmarks/PortableUiBenchmarks.cs`): add rounded-rect quad draw (shader vs fallback), one Kawase blur pass at 1180×760÷4, full themed-tree redraw for a Wave-1 vs Wave-3 theme, measure-cache on/off layout pass.

---

## §8 Phase W — World-space UI (in-game screens)

Motivation: use PortableUI *inside* a 3D/2D game world — the driving case is an adventure game where the player walks up to a computer and the monitor shows a live, interactive **DOS-themed UI** (dos theme + R9 CRT barrel/scanlines = a convincingly period-correct machine). Same mechanism covers cockpit MFDs, sci-fi door panels (lcars), phones, kiosks.

### W1 Off-screen rendering — `UISurface`
1. `public sealed class UISurface : IDisposable { Screen Screen; int Width, Height; RenderTarget2D Target; PortableTheme? Theme; bool IsInteractive; void Update(GameTime); void Draw(); }` — owns its `RenderTarget2D` (`PreserveContents` off, recreated on size change) and renders its screen into it each frame. DEFAULT: no dirty tracking in v1 (redraw every frame; a 640×400 RT is cheap) — noted as a later optimization for many-surface scenes, and D1 thumbnails already get lazy redraw via the theme version counter.
2. Builds on the existing `ScreenSizeMode.Manual` + `SetScreenSize` path: the surface's screen always lays out at its fixed virtual size (e.g. 640×400 for the DOS computer), independent of the backbuffer. `ApplyViewportSize` auto-tracking is bypassed for surfaces.
3. The game draws `surface.Target` however it wants: as a texture on a 3D quad/mesh (`BasicEffect`/custom shader), on a 2D sprite with rotation/scale, or in an `Image` control (UI-in-UI). The library does not own the 3D draw — it just guarantees a clean RT with premultiplied alpha.
4. R9 post-FX run per surface (each surface has its own theme → its own chain): the CRT barrel happens inside the RT, so the curvature is part of the monitor texture. R8 backdrop blur also works per surface (its "screen" is the RT).
5. The main full-window UI becomes just the default surface internally — one code path (`ScreenComponent` hosts `UISurface MainSurface` sized by viewport).

### W2 Multi-surface engine (de-static-ing)
The single-engine statics are the blocker: `ScreenEngine.Instance`, `ScreenEngine.FocusedControl`, `ScaleFactor`, plus the "probably better if it's internal / small hack" members (`ScreenEngine.cs:16,92,104`).
1. Move per-UI state into an engine/surface instance: focused control, active flyout/tooltip, mouse capture, screen stack (`NavigateToScreen`/`NavigateBack` become per-surface — the in-game computer can navigate its own screens!).
2. Controls reach their engine via their `Screen` (walk `Parent` to root, cache). DECIDED — **hard cut together with the T2 clean break**: no `[Obsolete]` facade period; one big "v2" breaking release covers both PortableTheme v2 and the de-static-ed engine.
   - Migration inventory (do in the same PR as T2): `ScreenEngine.Instance` → surface reference; `ScreenEngine.FocusedControl` (incl. `Control.IsFocused`) → `Surface.FocusedControl`; `ScreenEngine.ScaleFactor` → `UISurface.ScaleFactor`; `NavigateToScreen`/`NavigateBack` statics → instance methods (main-surface convenience overloads kept on `ScreenEngine` as thin instance-forwarders); the three "small hack" keyboard members (`ScreenEngine.cs:16,92,104`) become internal per-surface state — resolves that TODO for free.
   - Test setups `ResetFocus`/`ResetState`/`ResetTime` are deleted; tests construct a fresh surface per test instead.
   - Release notes: single "BREAKING: v2 — themes & multi-surface" section listing both old→new maps (extends the T2 table).
3. `SolidColorBrush.Pixel` and other device-bound statics are per-`GraphicsDevice` anyway (single device in MonoGame) — leave, but route through R1.3's cache for device-reset safety.
4. Tests: this *removes* the test-bleed problem noted in §1.1 (per-instance state instead of `ResetState()` hacks) — rewrite those setups.

### W3 Virtual input — activate `IInputSource`
`Input/IInputSource.cs` already defines the right abstraction (`MousePosition`, `PressedMouseButtons`, `ScrollWheelValue`, `Touches`) but **nothing implements or consumes it** — `Screen.Update` polls `Mouse.GetState()`/`TouchPanel` directly.
1. Implement `DeviceInputSource` (wraps today's polling — default for the main surface) and make `Screen.Update` consume `IInputSource` exclusively. Zero behavior change for existing users.
2. `VirtualInputSource : IInputSource` with settable state: the game feeds `SetPointer(PointF uv, bool leftDown, …)` per frame from its own picking logic; `NullInputSource` for purely decorative surfaces (`IsInteractive = false` skips input entirely — cheap billboards).
3. Keyboard: per-surface routing — text input (`Window.TextInput`, C3.3) and key commands go to the surface that currently **has input focus** (see W4.3). `IKeyboard`/on-screen keyboard hooks stay per-surface.
4. Add `float ScaleFactor`/DPI per surface (replaces the global `ScreenEngine.ScaleFactor`).

### W4 World mapping helpers
The library ships the math, the game supplies the geometry:
1. `WorldSurfaceMapper.TryMapRayToSurface(Ray ray, Matrix quadWorld, Vector2 quadSize, out PointF uiPoint)` — intersect ray with the quad's plane (`Plane.Transform(new Plane(Vector3.Backward, 0), quadWorld)`), convert hit to local space via `Matrix.Invert(quadWorld)`, reject outside [0,1]² UV, scale UV by surface virtual size. For 2D games: `TryMapPointToSurface(PointF screenPoint, Matrix spriteTransform, …)` (inverse transform, same UV step).
2. Mouse ray from camera: `GraphicsDevice.Viewport.Unproject` near/far — provide a one-line helper `WorldSurfaceMapper.GetMouseRay(Viewport, Matrix view, Matrix projection, PointF mousePos)`.
3. **Activation model**: `UISurface.HasInputFocus` — the game decides when the player "uses" the computer (proximity + click/interact key) and calls `surface.Focus()`; while focused, the surface's `VirtualInputSource` receives mapped input and keyboard; the HUD surface ignores input or keeps hover-only. Escape/interact-again releases. (The game owns this policy; the library only guarantees exactly one keyboard-focused surface.)
4. Hover-at-distance: mapping runs every frame regardless of focus if the game wants hover highlights on the monitor from across the room — cheap (one plane intersection).

### W5 Software cursor & per-surface polish
1. The OS cursor lives on the real window, so an interactive world surface draws its own cursor: `UISurface.CursorVisual` = themed `Brush`/texture drawn last at `VirtualInputSource.MousePosition` (dos = blinking block or arrow glyph from px437; modern = small arrow texture). Skipped when `null`.
2. Per-surface tooltip/flyout layers already render inside the surface's own RT after W2 — dropdowns on the in-game computer stay on its screen (and inherit the dos theme via T3.5 overlay inheritance).
3. Optional emissive trick for the adventure game: the RT is also usable as an **emissive map** on the monitor material so the screen glows in a dark room — document in the W6 sample, no library work.

### W6 Demo — "Adventure Room"
DECIDED: **in-demo** (no separate sample project). Reachable as a screen from the demo header ("Adventure Room" button → `NavigateToScreen`), not a TabItem — it needs to own the whole frame: the screen's background pass renders the 3D scene (`BasicEffect`, depth on, then restore sprite states before the UI pass), the regular UI layer shows only a thin hint/status strip, and the monitor's `UISurface` updates/draws independently. A simple 3D room (few textured quads, `BasicEffect`), a desk with a monitor quad showing a `UISurface` (640×400, dos theme, R9 CRT chain) running a small Norton-style file browser UI; walk/mouse-look or fixed camera + cursor; click the monitor → surface takes focus, type in a TextBox on it; F-key bar works; second prop (sci-fi door panel, lcars theme, hover-only) shows two surfaces with different themes coexisting — the Theme-Islands-plus-surfaces showcase.

### W-order & scope notes
- W3 (input abstraction) and W1 (render-to-target) are independently useful and can land early — W1 also gives D4 screenshots and the D1 thumbnail fallback for free.
- W2 is the invasive one; schedule it adjacent to the T2 clean break so the breaking changes batch into one release.
- Q additions: mapper math tests (ray→UV corners/misses/oblique angles), virtual-input routing tests (two surfaces, focus swap), surface navigation stack tests.

---

## §9 Execution order

1. **P0** quick wins (R1, C3.1, C3.2, T6.1, **R6.0** minimal Tile/NineTile brushes) — immediate, low-risk, unblocks perf headroom + texture theming.
2. **R3 → R4 → R5 → R2** shader infra + rounded corners + brush context + shader clip-rect batching (the keystone; everything visual depends on it; R2's chosen shader-clip design needs R3/R4 anyway).
3. **T1 → T2 → T3 → T3.5 → T4** palette, styles, live switching, Theme Islands, transitions (the keystone architecture; do before writing 25 themes against the old flat bag). T3.5's first version may land even earlier, right after T3, against the flat theme.
4. **C1, C2, C4, C5** chrome props, Slider/ProgressBar, measure cache, indicators.
5. **R6 + R7** brushes + shadows → **Theme Wave 1** (≈19 themes/CRs incl. the promoted NeXTSTEP/BeOS — instant visible payoff).
6. **Theme Wave 2** (gradient/texture themes, ≈9) + R6 polish + parchment assets.
7. **R8 → R8.5 → R9** blur + liquid + post-FX → **Theme Wave 3** (≈8, incl. flagship Liquid Glass).
8. **W1 + W3** UISurface + input abstraction (can start any time after step 2; independent wins), then **W2** de-static-ing batched with the T2 breaking release, then **W4–W6** mapping, cursor, Adventure Room demo.
9. **D1–D7** gallery (needs T3.5), FX tab, inspector, screenshots (easier via W1), README.
10. **Q1–Q3** continuously per phase; Q2 baseline capture once Wave 3 lands.

Dependency notes: Wave 1 needs only steps 2–5; the demo gallery (D1) needs T3.5 ThemeIslands; screenshots (D4) are independent and can land early to document progress; the adventure-game DOS computer (W6) needs W1–W5 + the dos Wave-1 CRs + R9 for the CRT look.
