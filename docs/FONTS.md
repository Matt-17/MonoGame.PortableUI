# Bundled fonts

## Runtime TTF fonts for themes

Themes name their font (`Typography.FontName`, e.g. `atkinsonhyperlegible`). Register a TTF/OTF under that name
and every theme naming it draws with a runtime font (FontStashSharp) instead of the baked SpriteFont:

```csharp
FontManager.RegisterDynamicFontFile("atkinsonhyperlegible", "Content/Fonts/AtkinsonHyperlegible-Regular.ttf");
```

- Sharp at any size, `TextScaling` and HiDPI factor; any character the font has (no baked character set).
- Sizes are em sizes like a SpriteFont's (`TextSize 14` = 14 pt), so layouts match; `sizeScale` adjusts fonts whose
  SpriteFont was baked at another size than its name says (the demo: Press Start 2P 11/14, VT323 16/14).
- **Pixel fonts** (outlines on a coarse grid: Press Start 2P, Px437, Jersey 10, Silkscreen) are detected and drawn
  only at whole multiples of their pixel grid, never resampled - crisp, no anti-aliased edges. Sizes in between round
  to the nearest multiple, so headings and body text of a pixel theme may end up the same size.
- Text in motion (breathing, pop) keeps the glyphs of its resting size and scales them, so it glides instead of
  re-rasterizing at every intermediate size.
- A theme's own `Typography.DynamicFont` and a control's `DynamicFont` win; unregistered or unloadable names keep the
  SpriteFont. The demo registers all fonts below (`DemoFonts.RegisterThemeFonts`); `--sprite-fonts` compares.

## Licenses

All fonts shipped with the demo (`samples/MonoGame.PortableUI.Demo/Content/Fonts`) are free and
open licensed — usable in commercial projects. None of them require payment or restrict
distribution; the OFL/Apache/CC licenses require keeping their license notice with the font files.

| Font | File | Used by themes | Author | License | Source |
|---|---|---|---|---|---|
| Anton | `Anton-Regular.ttf` | phantom | Vernon Adams | SIL OFL 1.1 | [Google Fonts](https://fonts.google.com/specimen/Anton) |
| Atkinson Hyperlegible | `AtkinsonHyperlegible-Regular.ttf` | studio, aurora, glass, aqua, macos9, mac1bit, nord, parchment, eink, neumorphic, brutalist, liquid | Braille Institute | [SIL OFL 1.1](https://openfontlicense.org/) | [Google Fonts](https://fonts.google.com/specimen/Atkinson+Hyperlegible) |
| Barlow Condensed SemiBold | `BarlowCondensed-SemiBold.ttf` | tactical | Jeremy Tribby | SIL OFL 1.1 | [Google Fonts](https://fonts.google.com/specimen/Barlow+Condensed) |
| Cinzel SemiBold | `Cinzel-SemiBold.ttf` (weight 600 instance of the variable font) | fantasy | Natanael Gama | SIL OFL 1.1 | [Google Fonts](https://fonts.google.com/specimen/Cinzel) |
| Fredoka SemiBold | `Fredoka-SemiBold.ttf` (weight 600 instance of the variable font) | cozy | Milena Brandão | SIL OFL 1.1 | [Google Fonts](https://fonts.google.com/specimen/Fredoka) |
| IBM Plex Mono | `IBMPlexMono-Regular.ttf` | terminal, dracula, solarized, gruvbox | IBM | SIL OFL 1.1 | [Google Fonts](https://fonts.google.com/specimen/IBM+Plex+Mono) |
| Jersey 10 | `Jersey10-Regular.ttf` | amiga | Sarah Cadigan-Fried | SIL OFL 1.1 | [Google Fonts](https://fonts.google.com/specimen/Jersey+10) |
| Orbitron | `Orbitron-Variable.ttf` | cyberpunk, vaporwave, lcars, scifi | Matt McInerney | SIL OFL 1.1 | [Google Fonts](https://fonts.google.com/specimen/Orbitron) |
| Press Start 2P | `PressStart2P-Regular.ttf` | c64, nes | CodeMan38 | SIL OFL 1.1 | [Google Fonts](https://fonts.google.com/specimen/Press+Start+2P) |
| Px437 IBM VGA 8x16 | `Px437_IBM_VGA_8x16.ttf` | dos, norton | VileR (int10h.org) | [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | [The Oldschool PC Font Resource](https://int10h.org/oldschool-pc-fonts/) |
| Roboto | `Roboto-Variable.ttf` | material | Google (Christian Robertson) | SIL OFL 1.1 | [Google Fonts](https://fonts.google.com/specimen/Roboto) |
| Selawik | `Selawik-Regular.ttf` | win95, nextstep, beos, luna, aero, metro, fluent, default | Microsoft | SIL OFL 1.1 | [github.com/microsoft/Selawik](https://github.com/microsoft/Selawik) |
| Silkscreen | `Silkscreen-Regular.ttf` | gameboy | Jason Kottke | SIL OFL 1.1 | [Google Fonts](https://fonts.google.com/specimen/Silkscreen) |
| VT323 | `VT323-Regular.ttf` | phosphor, amber | Peter Hull | SIL OFL 1.1 | [Google Fonts](https://fonts.google.com/specimen/VT323) |

Notes:

- **SIL OFL 1.1** — free for any use including commercial; fonts may be bundled/redistributed but
  not sold on their own; keep the license text with the font files.
- **CC BY-SA 4.0** (Px437 IBM VGA) — free for any use including commercial with attribution;
  derivative *fonts* must be shared alike. This is the least-restrictive faithful VGA ROM font
  available; no public-domain equivalent exists.
- No proprietary system fonts are referenced anymore: previously several `.spritefont` files
  silently aliased Segoe UI/Consolas; they now build from the bundled open fonts above.
- The `default`/`Segoe`/`arial` spritefonts used by tests/fallbacks still reference system fonts
  present on Windows (Segoe UI, Arial); they are not bundled or redistributed.
