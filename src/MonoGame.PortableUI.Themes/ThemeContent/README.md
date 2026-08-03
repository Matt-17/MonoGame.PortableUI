# MonoGame.PortableUI.Themes — theme fonts

The theme catalog references ten open-licensed fonts. Fonts cannot ship
pre-built from a NuGet package (they must go through your game's content
pipeline), so this folder gives you everything to build them yourself:

1. **Copy** everything from `ThemeContent/Fonts/` into your game's
   `Content/Fonts/` folder (the `.ttf` files, the `.spritefont` descriptors and
   the license files).
2. **Paste** the blocks from `themes-fonts.mgcb-snippet.txt` into your
   `Content.mgcb`.
3. **Register** the fonts at startup:

   ```csharp
   FontManager.LoadFonts(this, PortableThemes.FontNames.ToArray());
   ```

You only need the fonts of the themes you actually use — each theme's font
family is on its `ThemeDefinition.FontName` (`PortableThemes.FontNames` lists
all of them).

Missing fonts degrade gracefully: a theme whose font is not built renders with
the correct colors and shapes using your default font, and `FontManager` logs
one warning per missing font family.

Licenses: see the `*-OFL.txt` / `*-CC-BY-SA-4.0.txt` files next to the fonts
and `FONTS.md` for per-font attribution.
