using System;

namespace MonoGame.PortableUI.Demo
{
    public sealed class DemoRunOptions
    {
        public DemoThemePreset InitialThemePreset { get; init; } = DemoThemeRegistry.Default;
        public string? ScreenshotDirectory { get; init; }
        public string ScreenshotScreen { get; init; } = "controls";
        /// <summary>Optional overlay in screenshots: modal, sheet or toast.</summary>
        public string? ScreenshotOverlay { get; init; }
        public bool IsScreenshotMode => !string.IsNullOrWhiteSpace(ScreenshotDirectory);

        /// <summary>--layout-scale 2: screenshots rendered at 2x pixels for the same layout (HiDPI check).</summary>
        public float LayoutScale { get; init; } = 1f;

        /// <summary>--screenshot-themes win95,luna: only render these theme ids.</summary>
        public string[]? ScreenshotThemes { get; init; }

        public static DemoRunOptions Parse(string[]? args)
        {
            // --text-scale 1.5 sets the app text size (TextScaling.AppScale) before anything is built.
            if (float.TryParse(TryParseValue(args, "--text-scale"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var textScale))
                MonoGame.PortableUI.Text.TextScaling.AppScale = textScale;

            // --language de switches the demo's Localizer before the screens are built.
            var language = TryParseValue(args, "--language");
            if (!string.IsNullOrWhiteSpace(language))
                MonoGame.PortableUI.Localization.Localizer.Default.Language = language;

            return new DemoRunOptions
            {
                InitialThemePreset = DemoThemeRegistry.ResolveStartupTheme(args),
                ScreenshotDirectory = TryParseValue(args, "--screenshot"),
                ScreenshotScreen = TryParseValue(args, "--screenshot-screen") ?? "controls",
                ScreenshotOverlay = TryParseValue(args, "--screenshot-overlay"),
                ScreenshotThemes = TryParseValue(args, "--screenshot-themes")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                LayoutScale = float.TryParse(TryParseValue(args, "--layout-scale"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var layoutScale) && layoutScale > 0 ? layoutScale : 1f
            };
        }

        private static string? TryParseValue(string[]? args, string name)
        {
            if (args == null)
                return null;

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase))
                    return i + 1 < args.Length ? args[i + 1] : null;

                var prefix = name + "=";
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return arg.Substring(prefix.Length);
            }

            return null;
        }
    }
}
