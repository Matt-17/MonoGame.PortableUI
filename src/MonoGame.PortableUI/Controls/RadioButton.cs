using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class RadioButton : ToggleButton
    {
        private static readonly Dictionary<string, List<RadioButton>> RadioButtonDictionary = new Dictionary<string, List<RadioButton>>();
        private string _radioGroup = "";

        public RadioButton()
        {
            var theme = PortableTheme.ResolveCurrent();

            DotBrush = theme.RadioButtonDotBrush;
            DotSize = theme.RadioButtonDotSize;
        }

        protected override void OnToggleClick()
        {
            // Clicking the selected radio keeps it selected; only an unchecked one toggles on.
            IsChecked = true;
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (ReferenceEquals(DotBrush, oldTheme.RadioButtonDotBrush))
                DotBrush = newTheme.RadioButtonDotBrush;
            if (DotSize.Equals(oldTheme.RadioButtonDotSize))
                DotSize = newTheme.RadioButtonDotSize;
        }

        public Brush? DotBrush { get; set; }

        public float DotSize { get; set; }

        public string RadioGroup
        {
            get { return _radioGroup; }
            set
            {
                RadioButton.RemoveFromList(_radioGroup, this);
                _radioGroup = value;
                RadioButton.AddToList(_radioGroup, this);
            }
        }

        /// <summary>
        /// Detaching a radio button from its visual tree must drop it from the static group
        /// registry, or a later screen reusing the same group name would read/drive stale buttons
        /// left behind by a screen that is no longer showing (the registry is keyed by group name
        /// only, and nothing else ever removes an entry).
        /// </summary>
        public override FrameworkElement? Parent
        {
            get { return base.Parent; }
            internal set
            {
                var wasAttached = base.Parent != null;
                base.Parent = value;
                if (value == null && wasAttached)
                    RadioButton.RemoveFromList(_radioGroup, this);
                else if (value != null && !wasAttached)
                    RadioButton.AddToList(_radioGroup, this);
            }
        }

        private static void AddToList(string? radioGroup, RadioButton radioButton)
        {
            if (string.IsNullOrEmpty(radioGroup))
                return;
            if (!RadioButtonDictionary.TryGetValue(radioGroup, out var list))
            {
                list = new List<RadioButton>();
                RadioButtonDictionary.Add(radioGroup, list);
                radioButton.IsChecked = true;
            }

            if (!list.Contains(radioButton))
                list.Add(radioButton);
        }

        private static void RemoveFromList(string? radioGroup, RadioButton radioButton)
        {
            if (string.IsNullOrEmpty(radioGroup))
                return;
            if (!RadioButtonDictionary.ContainsKey(radioGroup))
                return;
            var list = RadioButtonDictionary[radioGroup];
            list.Remove(radioButton);
            if (list.Count == 0)
                RadioButtonDictionary.Remove(radioGroup);
        }
        private bool _isSettingGroup = false;
        private static void SetGroupChecked(string radioGroup, RadioButton radioButton)
        {
            if (!RadioButtonDictionary.ContainsKey(radioGroup))
                return;
            var list = RadioButtonDictionary[radioGroup];
            foreach (var button in list)
            {
                button._isSettingGroup = true;
                button.IsChecked = button == radioButton;
                button._isSettingGroup = false;
            }
        }

        protected override void OnChecked(bool e)
        {
            if (!_isSettingGroup)
                RadioButton.SetGroupChecked(RadioGroup, this);
            base.OnChecked(e);
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            if (!IsChecked || DotBrush == null || DotSize <= 0)
                return;

            var dot = new Rect(
                rect.Left + Math.Max(4, DotSize / 2),
                rect.Top + (rect.Height - DotSize) / 2,
                DotSize,
                DotSize);
            DotBrush.Draw(spriteBatch, dot, RenderOpacity);
        }
    }
}
