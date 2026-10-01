using System;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Localization
{
    public static class LocalizationExtensions
    {
        /// <summary>
        ///     Binds <paramref name="apply"/> to the localizer: it runs now and again whenever the
        ///     language changes (on the screen's next update). Replaces an earlier binding.
        /// </summary>
        public static T Localize<T>(this T control, Action<T, Localizer> apply, Localizer? localizer = null) where T : Control
        {
            var source = localizer ?? Localizer.Default;
            control.LocalizationBinding = new LocalizationBinding(source, () => apply(control, source));
            apply(control, source);
            return control;
        }

        /// <summary>Removes a binding made with <see cref="Localize{T}"/>; the current text stays.</summary>
        public static T ClearLocalization<T>(this T control) where T : Control
        {
            control.LocalizationBinding = null;
            return control;
        }

        public static TextBlock LocalizedText(this TextBlock textBlock, string key, params object?[] args)
            => textBlock.Localize((c, l) => c.Text = l.Format(key, args));

        public static T LocalizedText<T>(this T button, string key, params object?[] args) where T : Button
            => button.Localize((c, l) => c.Text = l.Format(key, args));

        public static CheckBox LocalizedText(this CheckBox checkBox, string key, params object?[] args)
            => checkBox.Localize((c, l) => c.Text = l.Format(key, args));
    }

    internal sealed class LocalizationBinding
    {
        public LocalizationBinding(Localizer localizer, Action refresh)
        {
            Localizer = localizer;
            Refresh = refresh;
            AppliedVersion = localizer.Version;
        }

        public Localizer Localizer { get; }
        public Action Refresh { get; }
        public long AppliedVersion { get; set; }
    }
}
