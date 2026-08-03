# Bundled fonts

All fonts shipped with the demo (`samples/MonoGame.PortableUI.Demo/Content/Fonts`) are free and
open licensed — usable in commercial projects. None of them require payment or restrict
distribution; the OFL/Apache/CC licenses require keeping their license notice with the font files.

| Font | File | Used by themes | Author | License | Source |
|---|---|---|---|---|---|
| Atkinson Hyperlegible | `AtkinsonHyperlegible-Regular.ttf` | studio, aurora, glass, aqua, macos9, mac1bit, nord, parchment, eink, neumorphic, brutalist, liquid | Braille Institute | [SIL OFL 1.1](https://openfontlicense.org/) | [Google Fonts](https://fonts.google.com/specimen/Atkinson+Hyperlegible) |
| IBM Plex Mono | `IBMPlexMono-Regular.ttf` | terminal, dracula, solarized, gruvbox | IBM | SIL OFL 1.1 | [Google Fonts](https://fonts.google.com/specimen/IBM+Plex+Mono) |
| Jersey 10 | `Jersey10-Regular.ttf` | amiga | Sarah Cadigan-Fried | SIL OFL 1.1 | [Google Fonts](https://fonts.google.com/specimen/Jersey+10) |
| Orbitron | `Orbitron-Variable.ttf` | cyberpunk, vaporwave, lcars | Matt McInerney | SIL OFL 1.1 | [Google Fonts](https://fonts.google.com/specimen/Orbitron) |
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
