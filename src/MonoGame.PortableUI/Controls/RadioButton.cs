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

        /// <summary>
        ///     A classic radio: a round ring (filled with a dot when checked) followed by the label —
        ///     no button face. Ring, fill and dot follow the theme's check box and radio values; for a
        ///     segmented/button look use a <see cref="ToggleButton"/> group instead.
        /// </summary>
        public RadioButton()
        {
            var theme = PortableTheme.ResolveCurrent();

            DotBrush = theme.RadioButtonDotBrush;
            DotSize = theme.RadioButtonDotSize;
            BoxSize = theme.CheckBoxBoxSize;
            BoxSpacing = theme.CheckBoxBoxSpacing;
            RingBrush = theme.CheckBoxBoxBorderBrush;
            FillBrush = theme.CheckBoxBoxBackgroundBrush;
            TextColor = theme.CheckBoxTextColor;
            ToggleTextColor = null;
            TextAlignment = TextAlignment.Left;
            ShowFocusVisual = true;
            ApplyLabelPadding();
        }

        /// <summary>Diameter of the ring.</summary>
        public float BoxSize { get; set; }

        /// <summary>Gap between the ring and the label.</summary>
        public float BoxSpacing { get; set; }

        /// <summary>The ring's outline.</summary>
        public Brush? RingBrush { get; set; }

        /// <summary>The ring's inside.</summary>
        public Brush? FillBrush { get; set; }

        private void ApplyLabelPadding()
        {
            Padding = new Thickness(BoxSize + BoxSpacing, 0, 0, 0);
        }

        protected override ControlStyle? GetThemeStyle(PortableTheme theme) => null;

        protected override Brush? GetThemeBackgroundBrush(PortableTheme theme) => null;

        protected override ShadowStyle? GetThemeShadow(PortableTheme theme) => null;

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
            if (BoxSize.Equals(oldTheme.CheckBoxBoxSize))
                BoxSize = newTheme.CheckBoxBoxSize;
            if (BoxSpacing.Equals(oldTheme.CheckBoxBoxSpacing))
                BoxSpacing = newTheme.CheckBoxBoxSpacing;
            if (ReferenceEquals(RingBrush, oldTheme.CheckBoxBoxBorderBrush))
                RingBrush = newTheme.CheckBoxBoxBorderBrush;
            if (ReferenceEquals(FillBrush, oldTheme.CheckBoxBoxBackgroundBrush))
                FillBrush = newTheme.CheckBoxBoxBackgroundBrush;
            if (TextColor.Equals(newTheme.ButtonTextColor) || TextColor.Equals(oldTheme.CheckBoxTextColor))
                TextColor = newTheme.CheckBoxTextColor;
            ToggleTextColor = null;
            ApplyLabelPadding();
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
            RadioButtonDictionary.TryGetValue(radioGroup, out var list);
            // A group is scoped to one visual tree: same-named groups on other screens/surfaces
            // are independent, so "new" means no member in this button's tree yet.
            var isNewGroup = true;
            if (list != null)
            {
                var root = GetRoot(radioButton);
                foreach (var member in list)
                {
                    if (!ReferenceEquals(member, radioButton) && ReferenceEquals(GetRoot(member), root))
                    {
                        isNewGroup = false;
                        break;
                    }
                }
            }
            if (list == null)
            {
                list = new List<RadioButton>();
                RadioButtonDictionary.Add(radioGroup, list);
            }

            if (!list.Contains(radioButton))
                list.Add(radioButton);

            // A checked button joining wins the group, so re-attaching a group (e.g. moving its
            // panel) keeps the user's selection instead of ending up with two checked buttons.
            if (radioButton.IsChecked)
                SetGroupChecked(radioGroup, radioButton);
            else if (isNewGroup)
                radioButton.IsChecked = true;
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

        /// <summary>
        ///     The scope a group lives in: the root of the button's visual tree, or null for buttons
        ///     not attached yet (those form one shared scope, so code-built groups still work).
        /// </summary>
        private static FrameworkElement? GetRoot(FrameworkElement element)
        {
            if (element.Parent == null)
                return null;
            while (element.Parent != null)
                element = element.Parent;
            return element;
        }

        private static void SetGroupChecked(string radioGroup, RadioButton radioButton)
        {
            if (!RadioButtonDictionary.ContainsKey(radioGroup))
                return;
            var list = RadioButtonDictionary[radioGroup];
            var root = GetRoot(radioButton);
            foreach (var button in list)
            {
                if (!ReferenceEquals(GetRoot(button), root))
                    continue;
                button._isSettingGroup = true;
                button.IsChecked = button == radioButton;
                button._isSettingGroup = false;
            }
        }

        protected override void OnChecked(bool e)
        {
            // Only checking drives the group; unchecking in code must not re-check this button.
            if (e && !_isSettingGroup)
                RadioButton.SetGroupChecked(RadioGroup, this);
            base.OnChecked(e);
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            // No button face: ring (+ dot) at the left, the label is the content.
            var size = ToRender(BoxSize);
            var ring = new Rect(rect.Left, rect.Top + (rect.Height - size) / 2, size, size);
            var radius = new CornerRadius(size / 2);
            var device = spriteBatch.GraphicsDevice;
            // Brushes that honour a corner radius fill the circle; square-only chrome brushes (bevels,
            // frames, cut corners) would paint a square, so those fall back to a plain white disc.
            if (FillBrush is SolidColorBrush or LinearGradientBrush or GradientBrush or RadialGradientBrush)
                FillBrush.Draw(spriteBatch, new BrushContext(ring, radius, RenderOpacity, device, 0, null, ToRender(1f)));
            else if (FillBrush != null)
                RoundedRectRenderer.DrawSolid(spriteBatch, ring, radius, Brush.ApplyOpacity(Color.White, RenderOpacity));
            if (IsMouseHovering && IsEnabled && RingBrush is SolidColorBrush hover)
                RoundedRectRenderer.DrawSolid(spriteBatch, ring, radius, Brush.ApplyOpacity(hover.Color, RenderOpacity * 0.15f));
            if (RingBrush is SolidColorBrush solidRing)
            {
                var width = Math.Max(1, ToRender(1.5f));
                RoundedRectRenderer.DrawBorder(spriteBatch, ring, radius, new Thickness(width), Brush.ApplyOpacity(solidRing.Color, RenderOpacity));
            }

            if (!IsChecked || DotBrush == null || DotSize <= 0)
                return;
            var dotSize = Math.Min(ToRender(DotSize), size * 0.6f);
            var dot = new Rect(ring.Left + (size - dotSize) / 2, ring.Top + (size - dotSize) / 2, dotSize, dotSize);
            DotBrush.Draw(spriteBatch, new BrushContext(dot, new CornerRadius(dotSize / 2), RenderOpacity, device, 0, null, ToRender(1f)));
        }
    }
}
