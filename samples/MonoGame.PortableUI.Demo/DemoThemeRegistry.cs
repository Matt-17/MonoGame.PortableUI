using System;
using System.Collections.Generic;

using MonoGame.PortableUI.Themes;

namespace MonoGame.PortableUI.Demo
{
    /// <summary>
    ///     The demo's theme list and CLI/environment startup resolution. The demo defines only
    ///     one theme of its own — <see cref="DemoTheme"/>, the copy-me template — right after
    ///     the untouched "Default (no theme)" entry; everything else comes from the
    ///     MonoGame.PortableUI.Themes catalog.
    /// </summary>
    public static class DemoThemeRegistry
    {
        public const string DefaultThemeId = PortableThemes.DefaultThemeId;

        private static readonly IReadOnlyList<DemoThemePreset> AllPresets = BuildPresets();

        public static IReadOnlyList<DemoThemePreset> Presets => AllPresets;

        public static DemoThemePreset Default => Resolve(DefaultThemeId);

        public static IReadOnlyList<string> FontNames { get; } = BuildFontNames();

        public static DemoThemePreset Resolve(string? id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                foreach (var preset in Presets)
                {
                    if (string.Equals(preset.Id, id.Trim(), StringComparison.OrdinalIgnoreCase))
                        return preset;
                }
            }

            foreach (var preset in Presets)
            {
                if (string.Equals(preset.Id, DefaultThemeId, StringComparison.Ordinal))
                    return preset;
            }

            throw new InvalidOperationException("The demo theme registry does not contain the default preset.");
        }

        public static DemoThemePreset ResolveStartupTheme(string[]? args)
        {
            return ResolveStartupTheme(args, Environment.GetEnvironmentVariable("PORTABLEUI_DEMO_THEME"));
        }

        public static DemoThemePreset ResolveStartupTheme(string[]? args, string? environmentThemeId)
        {
            if (TryParseThemeArgument(args, out var argumentThemeId))
                return Resolve(argumentThemeId);

            if (!string.IsNullOrWhiteSpace(environmentThemeId))
                return Resolve(environmentThemeId);

            return Default;
        }

        public static bool TryParseThemeArgument(string[]? args, out string? themeId)
        {
            themeId = null;
            if (args == null)
                return false;

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, "--theme", StringComparison.OrdinalIgnoreCase))
                {
                    themeId = i + 1 < args.Length ? args[i + 1] : null;
                    return true;
                }

                const string prefix = "--theme=";
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    themeId = arg.Substring(prefix.Length);
                    return true;
                }
            }

            return false;
        }

        public static int IndexOf(string? id)
        {
            for (var i = 0; i < Presets.Count; i++)
            {
                if (string.Equals(Presets[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return 0;
        }

        private static IReadOnlyList<DemoThemePreset> BuildPresets()
        {
            var presets = new List<DemoThemePreset>
            {
                FromDefinition(PortableThemes.Default),
                DemoTheme.CreatePreset()
            };

            foreach (var definition in PortableThemes.All)
            {
                if (string.Equals(definition.Id, PortableThemes.DefaultThemeId, StringComparison.Ordinal))
                    continue;

                presets.Add(FromDefinition(definition));
            }

            return presets;
        }

        private static DemoThemePreset FromDefinition(ThemeDefinition definition)
        {
            return new DemoThemePreset
            {
                Id = definition.Id,
                DisplayName = definition.DisplayName,
                CreateTheme = definition.CreateTheme,
                Palette = definition.Palette,
                FontName = definition.FontName,
                ClearColor = definition.ClearColor,
                BackgroundColor = definition.BackgroundColor
            };
        }

        private static IReadOnlyList<string> BuildFontNames()
        {
            var names = new List<string>(PortableThemes.FontNames);
            foreach (var preset in Presets)
            {
                if (!string.Equals(preset.FontName, "default", StringComparison.OrdinalIgnoreCase) && !names.Contains(preset.FontName))
                    names.Add(preset.FontName);
            }

            return names;
        }
    }
}
