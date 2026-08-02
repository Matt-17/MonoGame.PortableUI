using System;
using System.Collections.Generic;
using System.Linq;
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

        public static int GetGroupValue(string group)
        {
            if (!RadioButtonDictionary.ContainsKey(@group))
            {
                throw new ArgumentOutOfRangeException();
            }
            var radioButtons = RadioButtonDictionary[@group];
            return radioButtons.Select((b, i) => new { RadioButton = b, Index = i }).Single(x => x.RadioButton.IsChecked).Index;
        }

        private static void AddToList(string? radioGroup, RadioButton radioButton)
        {
            if (string.IsNullOrEmpty(radioGroup))
                return;
            if (!RadioButtonDictionary.ContainsKey(radioGroup))
            {
                RadioButtonDictionary.Add(radioGroup, new List<RadioButton>());
                radioButton.IsChecked = true;
            }

            var list = RadioButtonDictionary[radioGroup];
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
