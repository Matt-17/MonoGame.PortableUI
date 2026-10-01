using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Localization;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class LocalizationTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private static Localizer CreateLocalizer()
        {
            var localizer = new Localizer();
            localizer.LoadJson("en", """{ "menu": { "save": "Save", "open": "Open" }, "count": "{0:N1} items" }""");
            localizer.LoadJson("de", """{ "menu": { "save": "Speichern" }, "count": "{0:N1} Einträge" }""");
            return localizer;
        }

        [TestMethod]
        public void Lookup_falls_back_from_region_to_language_to_fallback_language()
        {
            var localizer = CreateLocalizer();
            localizer.Language = "de-AT";

            Assert.AreEqual("Speichern", localizer.Get("menu.save"), "de-AT falls back to de");
            Assert.AreEqual("Open", localizer.Get("menu.open"), "missing in de falls back to en");
        }

        [TestMethod]
        public void Missing_keys_render_a_placeholder_or_the_key_and_raise_an_event()
        {
            var localizer = CreateLocalizer();
            string? reported = null;
            localizer.MissingKey += (s, e) => reported = e.Key;

            Assert.AreEqual("[!nope]", localizer.Get("nope"));
            Assert.AreEqual("nope", reported);

            localizer.MissingKeyBehavior = MissingKeyBehavior.Key;
            Assert.AreEqual("nope", localizer.Get("nope"));
        }

        [TestMethod]
        public void Format_uses_the_culture_of_the_current_language()
        {
            var localizer = CreateLocalizer();
            Assert.AreEqual("1,234.5 items", localizer.Format("count", 1234.5));

            localizer.Language = "de";
            Assert.AreEqual("1.234,5 Einträge", localizer.Format("count", 1234.5));
        }

        [TestMethod]
        public void Broken_format_strings_return_the_raw_text()
        {
            var localizer = new Localizer();
            localizer.Add("en", new[] { new System.Collections.Generic.KeyValuePair<string, string>("bad", "{0} and {1") });
            Assert.AreEqual("{0} and {1", localizer.Format("bad", 1));
        }

        [TestMethod]
        public void Changed_fires_on_language_switch_but_not_for_the_same_language()
        {
            var localizer = CreateLocalizer();
            var raised = 0;
            localizer.Changed += (s, e) => raised++;

            localizer.Language = "de";
            localizer.Language = "DE";
            Assert.AreEqual(1, raised);
        }

        [TestMethod]
        public void Bound_labels_follow_a_language_switch_on_the_next_update()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 300);
            var localizer = CreateLocalizer();
            var label = new TextBlock().Localize((c, l) => c.Text = l.Format("count", 3), localizer);
            var button = new Button().Localize((c, l) => c.Text = l["menu.save"], localizer);
            var screen = new TestScreen { Content = new StackPanel { Children = { label, button } } };
            engine.NavigateToScreen(screen);
            screen.Update();
            Assert.AreEqual("Save", button.Text);

            localizer.Language = "de";
            screen.Update();

            Assert.AreEqual("Speichern", button.Text);
            Assert.AreEqual("3,0 Einträge", label.Text);

            button.ClearLocalization();
            localizer.Language = "en";
            screen.Update();
            Assert.AreEqual("Speichern", button.Text, "an unbound control keeps its text");
            Assert.AreEqual("3.0 items", label.Text);
        }
    }
}
