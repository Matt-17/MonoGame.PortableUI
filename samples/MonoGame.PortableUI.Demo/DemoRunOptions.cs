using System;

namespace MonoGame.PortableUI.Demo
{
    public sealed class DemoRunOptions
    {
        public DemoThemePreset InitialThemePreset { get; init; } = DemoThemeRegistry.Default;
        public string? ScreenshotDirectory { get; init; }
        public string ScreenshotScreen { get; init; } = "controls";
        public bool IsScreenshotMode => !string.IsNullOrWhiteSpace(ScreenshotDirectory);

        public static DemoRunOptions Parse(string[]? args)
        {
            return new DemoRunOptions
            {
                InitialThemePreset = DemoThemeRegistry.ResolveStartupTheme(args),
                ScreenshotDirectory = TryParseValue(args, "--screenshot"),
                ScreenshotScreen = TryParseValue(args, "--screenshot-screen") ?? "controls"
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
