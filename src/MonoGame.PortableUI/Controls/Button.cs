using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     Button
    /// </summary>
    public class Button : ContentControl
    {
        private Color _textColor;
        private Color? _pressedTextColor;
        private Color? _hoverTextColor;
        private Color? _disabledTextColor;
        private TextAlignment _textAlignment;
        private bool _isPressedVisualState;
        private Vector2 _pressedScaleOrigin;
        private Vector2 _pressedTranslationOrigin;

        public Button()
        {
            var theme = PortableTheme.ResolveCurrent();

            Padding = theme.ButtonPadding;
            BackgroundBrush = theme.ButtonBackgroundBrush;
            HoverColor = theme.ButtonHoverBrush;
            PressedColor = theme.ButtonPressedBrush;
            TextColor = theme.ButtonTextColor;
            HoverTextColor = theme.ButtonHoverTextColor;
            PressedTextColor = theme.ButtonPressedTextColor;
            DisabledTextColor = theme.DisabledTextColor;
            TextAlignment = TextAlignment.Center;
            ShowFocusVisual = true;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            var clientRect = rect;
            if (IsPressedVisualState())
                PressedColor.Draw(spriteBatch, clientRect, RenderOpacity);
            else if (HoverState == HoverStates.Hovering)
                HoverColor.Draw(spriteBatch, clientRect, RenderOpacity);
        }

        #region Properties

        public Brush HoverColor { get; set; }
        public Brush PressedColor { get; set; }
        public bool AnimatePressedState { get; set; } = true;
        public float PressedHorizontalInset { get; set; } = 2;
        public float PressedVerticalInset { get; set; } = 1;
        public Vector2 PressedTranslation { get; set; } = Vector2.Zero;

        public string Text
        {
            get
            {
                var textBlock = Content as TextBlock;
                return textBlock?.Text ?? "";
            }
            set
            {
                var textBlock = Content as TextBlock;
                if (textBlock == null && Content == null)
                {
                    textBlock = new TextBlock();
                    SetTextAlignment(textBlock);
                    Content = textBlock;
                    ChangeVisualState();
                }
                if (textBlock != null && textBlock.Text != value)
                    textBlock.Text = value;
            }
        }

        private void SetTextAlignment(TextBlock textBlock)
        {
            textBlock.TextAlignment = TextAlignment;
            switch (TextAlignment)
            {
                case TextAlignment.Left:
                    textBlock.HorizontalAlignment = HorizontalAlignment.Left;
                    break;
                case TextAlignment.Right:
                    textBlock.HorizontalAlignment = HorizontalAlignment.Right;
                    break;
                default:
                    textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                    break;
            }
        }

        public Color TextColor
        {
            get { return _textColor; }
            set
            {
                _textColor = value;
                ChangeVisualState();
                InvalidateLayout(false);
            }
        }

        public Color? PressedTextColor
        {
            get { return _pressedTextColor; }
            set
            {
                _pressedTextColor = value;
                ChangeVisualState();
                InvalidateLayout(false);
            }
        }

        public Color? HoverTextColor
        {
            get { return _hoverTextColor; }
            set
            {
                _hoverTextColor = value;
                ChangeVisualState();
                InvalidateLayout(false);
            }
        }

        public Color? DisabledTextColor
        {
            get { return _disabledTextColor; }
            set
            {
                _disabledTextColor = value;
                ChangeVisualState();
                InvalidateLayout(false);
            }
        }

        public TextAlignment TextAlignment
        {
            get
            {
                return _textAlignment;
            }
            set
            {
                _textAlignment = value;
                var textBlock = Content as TextBlock;
                if (textBlock != null)
                {
                    SetTextAlignment(textBlock);
                    ChangeVisualState();
                }
            }
        }

        internal override void ChangeVisualState()
        {
            var isPressed = IsPressedVisualState();
            UpdatePressedAnimation(isPressed);

            var textBlock = Content as TextBlock;
            if (textBlock == null)
                return;

            var color = TextColor;
            if (!IsEnabled && DisabledTextColor != null)
                color = (Color)DisabledTextColor;
            else if (HoverState == HoverStates.Hovering && HoverTextColor != null)
                color = (Color)HoverTextColor;
            if (IsEnabled && isPressed && PressedTextColor != null)
                color = (Color)PressedTextColor;
            textBlock.TextColor = color;
        }

        private void UpdatePressedAnimation(bool isPressed)
        {
            if (!AnimatePressedState || _isPressedVisualState == isPressed)
                return;

            _isPressedVisualState = isPressed;
            var duration = TimeSpan.FromMilliseconds(isPressed ? 70 : 90);
            Vector2 targetScale;
            Vector2 targetTranslation;

            if (isPressed)
            {
                _pressedScaleOrigin = Scale;
                _pressedTranslationOrigin = Translation;
                var width = Math.Max(1, ClippingRect.Width);
                var height = Math.Max(1, ClippingRect.Height);
                var scaleX = Math.Max(0.01f, (width - PressedHorizontalInset * 2) / width);
                var scaleY = Math.Max(0.01f, (height - PressedVerticalInset * 2) / height);
                targetScale = new Vector2(_pressedScaleOrigin.X * scaleX, _pressedScaleOrigin.Y * scaleY);
                targetTranslation = _pressedTranslationOrigin + PressedTranslation;
            }
            else
            {
                targetScale = _pressedScaleOrigin == Vector2.Zero ? Vector2.One : _pressedScaleOrigin;
                targetTranslation = _pressedTranslationOrigin;
            }

            this.Animate()
                .Scale(targetScale)
                .TranslateTo(targetTranslation)
                .Duration(duration)
                .Ease(Easings.CubicOut)
                .Start();
        }

        private bool IsPressedVisualState()
        {
            return HoverState == HoverStates.Hovering && MouseButtonStates[MouseButton.Left] == ButtonState.Pressed
                || TouchState == TouchStates.Touched;
        }

        #endregion
    }
}
