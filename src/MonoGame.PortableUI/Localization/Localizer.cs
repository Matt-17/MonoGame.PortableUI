using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace MonoGame.PortableUI.Localization
{
    /// <summary>What <see cref="Localizer.Get"/> returns for a key no catalogue contains.</summary>
    public enum MissingKeyBehavior
    {
        /// <summary>A visible marker such as <c>[!menu.save]</c>, for development builds.</summary>
        Placeholder,
        /// <summary>The key itself, which stays readable in a release build.</summary>
        Key
    }

    public sealed class MissingKeyEventArgs : EventArgs
    {
        public MissingKeyEventArgs(string key, string language)
        {
            Key = key;
            Language = language;
        }

        public string Key { get; }
        public string Language { get; }
    }

    /// <summary>
    ///     String catalogues per language with a fallback language and a runtime switch.
    ///     Controls bound with <see cref="LocalizationExtensions.Localize{T}"/> (or the
    ///     <c>LocalizedText</c> helpers) follow a language change on their screen's next update,
    ///     without rebuilding the screen.
    /// </summary>
    /// <remarks>
    ///     Lookup order for language <c>de-AT</c>: <c>de-AT</c>, <c>de</c>, then
    ///     <see cref="FallbackLanguage"/>. Catalogue files are JSON objects; nested objects are
    ///     flattened with dots (<c>{"menu": {"save": "Save"}}</c> gives the key <c>menu.save</c>).
    /// </remarks>
    public sealed class Localizer
    {
        private readonly Dictionary<string, Dictionary<string, string>> _catalogs = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        private string _language = "en";
        private CultureInfo _culture;
        private long _version;
        private static long _globalVersion;

        public Localizer()
        {
            _culture = ResolveCulture(_language);
        }

        /// <summary>The instance controls bind to by default.</summary>
        public static Localizer Default { get; } = new Localizer();

        /// <summary>Raised after <see cref="Language"/> changed or a catalogue was added.</summary>
        public event EventHandler? Changed;

        /// <summary>Raised for every lookup of an unknown key (log it during development).</summary>
        public event EventHandler<MissingKeyEventArgs>? MissingKey;

        public string FallbackLanguage { get; set; } = "en";

        public MissingKeyBehavior MissingKeyBehavior { get; set; } = MissingKeyBehavior.Placeholder;

        /// <summary>Culture name of the current language (e.g. "de" or "de-AT").</summary>
        public string Language
        {
            get => _language;
            set
            {
                value = string.IsNullOrWhiteSpace(value) ? FallbackLanguage : value.Trim();
                if (string.Equals(_language, value, StringComparison.OrdinalIgnoreCase))
                    return;
                _language = value;
                _culture = ResolveCulture(value);
                RaiseChanged();
            }
        }

        /// <summary>Culture used to format arguments in <see cref="Format"/>.</summary>
        public CultureInfo Culture => _culture;

        public IEnumerable<string> Languages => _catalogs.Keys;

        internal long Version => Interlocked.Read(ref _version);

        /// <summary>Bumped by every localizer's change; screens compare it once per update.</summary>
        internal static long GlobalVersion => Interlocked.Read(ref _globalVersion);

        /// <summary>Switches to the device/OS UI language (<see cref="CultureInfo.CurrentUICulture"/>).</summary>
        public void UseSystemLanguage() => Language = CultureInfo.CurrentUICulture.Name;

        /// <summary>Adds or merges strings for a language (later entries win).</summary>
        public void Add(string language, IEnumerable<KeyValuePair<string, string>> strings)
        {
            if (!_catalogs.TryGetValue(language, out var catalog))
                _catalogs[language] = catalog = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in strings)
                catalog[pair.Key] = pair.Value;
            RaiseChanged();
        }

        public void LoadJson(string language, string json)
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            var strings = new List<KeyValuePair<string, string>>();
            Flatten(document.RootElement, "", strings);
            Add(language, strings);
        }

        public void LoadJson(string language, Stream stream)
        {
            using var reader = new StreamReader(stream);
            LoadJson(language, reader.ReadToEnd());
        }

        public bool Contains(string key) => TryGet(key, out _);

        public bool TryGet(string key, out string value)
        {
            if (TryGetIn(_language, key, out value))
                return true;
            var dash = _language.IndexOf('-');
            if (dash > 0 && TryGetIn(_language.Substring(0, dash), key, out value))
                return true;
            return TryGetIn(FallbackLanguage, key, out value);
        }

        /// <summary>The string for <paramref name="key"/>; never throws (see <see cref="MissingKeyBehavior"/>).</summary>
        public string Get(string key)
        {
            if (TryGet(key, out var value))
                return value;
            MissingKey?.Invoke(this, new MissingKeyEventArgs(key, _language));
            return MissingKeyBehavior == MissingKeyBehavior.Placeholder ? "[!" + key + "]" : key;
        }

        /// <summary>
        ///     <see cref="string.Format(IFormatProvider, string, object[])"/> over the looked-up string,
        ///     in the current language's culture. A broken format string returns the raw text.
        /// </summary>
        public string Format(string key, params object?[] args)
        {
            var format = Get(key);
            if (args == null || args.Length == 0)
                return format;
            try
            {
                return string.Format(_culture, format, args);
            }
            catch (FormatException)
            {
                return format;
            }
        }

        public string this[string key] => Get(key);

        private bool TryGetIn(string language, string key, out string value)
        {
            if (_catalogs.TryGetValue(language, out var catalog) && catalog.TryGetValue(key, out var found))
            {
                value = found;
                return true;
            }
            value = "";
            return false;
        }

        private void RaiseChanged()
        {
            Interlocked.Increment(ref _version);
            Interlocked.Increment(ref _globalVersion);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private static void Flatten(JsonElement element, string prefix, List<KeyValuePair<string, string>> into)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                        Flatten(property.Value, prefix.Length == 0 ? property.Name : prefix + "." + property.Name, into);
                    break;
                case JsonValueKind.String:
                    into.Add(new KeyValuePair<string, string>(prefix, element.GetString() ?? ""));
                    break;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    break;
                default:
                    into.Add(new KeyValuePair<string, string>(prefix, element.GetRawText()));
                    break;
            }
        }

        private static CultureInfo ResolveCulture(string name)
        {
            try
            {
                return CultureInfo.GetCultureInfo(name);
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }
    }
}
