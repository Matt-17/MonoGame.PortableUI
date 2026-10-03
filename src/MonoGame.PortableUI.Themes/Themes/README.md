# Themes

Each file here is one complete theme. You can use them in two ways.

## 1. As a package (quick start, prototypes)

Reference **CodeIX.PortableUI.Themes** from NuGet (namespace `MonoGame.PortableUI.Themes`) and pick a theme by id:

```csharp
var theme = PortableThemes.Find("luna")!;          // or PortableThemes.All
var engine = ScreenEngine.Initialize(this, new ScreenEngineOptions
{
    Theme = theme.CreateTheme()
});
// clear the back buffer with theme.ClearColor
```

Fonts: see `../ThemeContent/README.md` (copy the font files, paste the mgcb snippet,
`FontManager.LoadFonts(...)`). Without them a theme still works with your default font.

## 2. As a copy (your own, final look)

Every theme file depends only on the core **MonoGame.PortableUI** package
(`ThemeBuilder`, the brushes and the palette types live there). To adapt one:

1. Copy the theme file (e.g. `LunaTheme.cs`) into your project. Nothing else from this
   folder is needed.
2. Change the namespace and class name if you like, then edit colors, brushes and slots.
   The `styleTheme` callback is where the look is made — every assignment is a plain
   property on `PortableTheme`.
3. If the theme names a font (the third argument, e.g. `"cinzel"`), copy that font's
   `.ttf`, `.spritefont` and license from `../ThemeContent/Fonts/` and add its block from
   `../ThemeContent/themes-fonts.mgcb-snippet.txt` to your `Content.mgcb`, or change the
   name to a font you already have.
4. Use it: `Theme = LunaTheme.Create().CreateTheme()`.

## Writing a theme

- `ThemeBuilder.Catalog(...)` builds the palette from eight hex colors; the
  `styleTheme` callback then styles control slots (`theme.Button`, `theme.PrimaryButton`,
  `theme.TextBox`, …) and flat properties (`theme.TabSelectedHeaderBackgroundBrush`, …).
- `ThemeBuilder.Variants(theme, (style, color) => …)` styles the Primary/Secondary/Danger
  buttons in one go; `ThemeBuilder.Gloss(...)` makes vertical gradients (equal positions give
  a hard gloss edge).
- Shape brushes: `BevelBrush` (Win9x bevels), `FrameBrush` (rings, pixel-notched corners),
  `ChamferBrush` (cut corners, accent bar), `PatternBrush` (dither, pinstripes, hatch),
  `FrostedGlassBrush`. Shadows can be chained with `ShadowStyle.Also`.
- Use button variants in your UI (`new TextButton("Play") { Variant = ButtonVariant.Primary }`)
  instead of setting colors on controls, so the theme decides the look.
