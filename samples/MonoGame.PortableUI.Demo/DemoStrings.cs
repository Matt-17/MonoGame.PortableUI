using MonoGame.PortableUI.Localization;

namespace MonoGame.PortableUI.Demo
{
    /// <summary>Demo string catalogues (a real app would load one JSON file per language).</summary>
    internal static class DemoStrings
    {
        private static bool _loaded;

        private const string English = """
        {
          "actions": {
            "primary": "Primary action",
            "secondary": "Secondary action",
            "toasts": "Show three toasts",
            "danger": "Danger action"
          },
          "toast": {
            "first": "First message",
            "second": "Second message",
            "third": "Third message, shown longer"
          },
          "language": {
            "label": "Language (Localizer)",
            "status": "Language: {0}"
          }
        }
        """;

        // "actions.danger" is left out on purpose: it falls back to English.
        private const string German = """
        {
          "actions": {
            "primary": "Hauptaktion",
            "secondary": "Zweitaktion",
            "toasts": "Drei Meldungen zeigen"
          },
          "toast": {
            "first": "Erste Meldung",
            "second": "Zweite Meldung",
            "third": "Dritte Meldung, länger sichtbar"
          },
          "language": {
            "label": "Sprache (Localizer)",
            "status": "Sprache: {0}"
          }
        }
        """;

        public static void EnsureLoaded()
        {
            if (_loaded)
                return;
            _loaded = true;
            Localizer.Default.LoadJson("en", English);
            Localizer.Default.LoadJson("de", German);
        }
    }
}
