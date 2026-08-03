namespace MonoGame.PortableUI.Themes;

/// <summary>NES 8-Bit: black screen, NES-palette blue/red accents, chunky primary borders.</summary>
public static class NesTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("nes", "NES 8-Bit", "pressstart2p", ThemeEra.Retro, ThemeBrightness.Dark,
            background: "#000000", surface: "#202020", surfaceAlt: "#7C7C7C", text: "#FCFCFC",
            primary: "#3CBCFC", secondary: "#F83800", selection: "#FCFCFC", selectionText: "#000000",
            styleTheme: theme => ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(theme.Palette.Primary), 2, 0));
    }
}