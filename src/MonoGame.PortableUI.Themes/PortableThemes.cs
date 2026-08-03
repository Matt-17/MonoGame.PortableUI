using System;
using System.Collections.Generic;

namespace MonoGame.PortableUI.Themes;

/// <summary>
///     The theme catalog of the MonoGame.PortableUI.Themes package. Every entry lives in its
///     own file under <c>Themes/</c> so a single theme can be copied into a project and
///     customized without pulling in the rest of the catalog.
/// </summary>
public static class PortableThemes
{
    public const string DefaultThemeId = "default";

    public static IReadOnlyList<ThemeDefinition> All { get; } = new[]
    {
        DefaultTheme.Create(),
        GlassTheme.Create(),
        C64Theme.Create(),
        GameBoyTheme.Create(),
        DosTheme.Create(),
        AmigaTheme.Create(),
        TerminalTheme.Create(),
        StudioTheme.Create(),
        AuroraTheme.Create(),
        NesTheme.Create(),
        Mac1BitTheme.Create(),
        NortonTheme.Create(),
        PhosphorTheme.Create(),
        AmberTheme.Create(),
        Win95Theme.Create(),
        MacOs9Theme.Create(),
        NextStepTheme.Create(),
        BeOsTheme.Create(),
        LunaTheme.Create(),
        AquaTheme.Create(),
        AeroTheme.Create(),
        MetroTheme.Create(),
        FluentTheme.Create(),
        MaterialTheme.Create(),
        LiquidGlassTheme.Create(),
        CyberpunkTheme.Create(),
        VaporwaveTheme.Create(),
        NordTheme.Create(),
        DraculaTheme.Create(),
        SolarizedLightTheme.Create(),
        SolarizedDarkTheme.Create(),
        GruvboxTheme.Create(),
        ParchmentTheme.Create(),
        LcarsTheme.Create(),
        EInkTheme.Create(),
        NeumorphicTheme.Create(),
        BrutalistTheme.Create()
    };

    /// <summary>Distinct theme font families in catalog order (excluding the built-in "default" font).</summary>
    public static IReadOnlyList<string> FontNames { get; } = CollectFontNames();

    public static ThemeDefinition Default => Resolve(DefaultThemeId);

    /// <summary>Returns the theme with the given id (case-insensitive), or null when unknown.</summary>
    public static ThemeDefinition? Find(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        foreach (var theme in All)
        {
            if (string.Equals(theme.Id, id.Trim(), StringComparison.OrdinalIgnoreCase))
                return theme;
        }

        return null;
    }

    /// <summary>Returns the theme with the given id, falling back to the default theme when unknown.</summary>
    public static ThemeDefinition Resolve(string? id)
    {
        var found = Find(id);
        if (found != null)
            return found;

        foreach (var theme in All)
        {
            if (string.Equals(theme.Id, DefaultThemeId, StringComparison.Ordinal))
                return theme;
        }

        throw new InvalidOperationException("The theme catalog does not contain the default theme.");
    }

    private static IReadOnlyList<string> CollectFontNames()
    {
        var names = new List<string>();
        foreach (var theme in All)
        {
            if (string.Equals(theme.FontName, "default", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!names.Contains(theme.FontName))
                names.Add(theme.FontName);
        }

        return names;
    }
}