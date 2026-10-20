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
        private ButtonVariant _variant;
        private bool _textColorFromTheme;
        private Vector2 _pressedScaleOrigin;
        private Vector2 _pressedTranslationOrigin;

        public Button()
        {
            var theme = PortableTheme.ResolveCurrent();

            Padding = theme.ButtonPadding;
            if (theme.ButtonMargin != null)
                Margin = theme.ButtonMargin.Value;
            // Background/border/corner radius/shadow resolve live from theme.Button (T2);
            // only overlay/text snapshots are seeded here and re-seeded in OnThemeChanged (T3).
            HoverColor = theme.ButtonHoverBrush;
            PressedColor = theme.ButtonPressedBrush;
            TextColor = theme.ButtonTextColor;
            _textColorFromTheme = true;
            HoverTextColor = theme.ButtonHoverTextColor;
            PressedTextColor = theme.ButtonPressedTextColor;
            DisabledTextColor = theme.DisabledTextColor;
            TextAlignment = TextAlignment.Center;
            ShowFocusVisual = true;
            KeyPressed += ActivateOnKeyPressed;
            KeyDown += ActivateOnKeyDown;
        }

        /// <summary>Internal chrome buttons (list items, tab headers, menu entries) opt out.</summary>
        internal bool UseThemeStyle { get; set; } = true;

        /// <summary>
        ///     The button's role. Primary/Secondary/Danger take chrome and text color from the theme's
        ///     matching slot (<see cref="PortableTheme.PrimaryButton"/> …) unless set explicitly.
        /// </summary>
        public ButtonVariant Variant
        {
            get => _variant;
            set
            {
                if (_variant == value)
                    return;
                _variant = value;
                ChangeVisualState();
                InvalidateLayout(false);
            }
        }

        protected override Audio.UiSound? GetClickSound() => Audio.UiSound.Click;

        protected override ControlStyle? GetThemeStyle(PortableTheme theme)
        {
            if (!UseThemeStyle)
                return null;
            return _variant switch
            {
                ButtonVariant.Primary => theme.PrimaryButton,
                ButtonVariant.Secondary => theme.SecondaryButton,
                ButtonVariant.Danger => theme.DangerButton,
                _ => theme.Button
            };
        }

        protected override bool UsesThemeMinimumSize => UseThemeStyle;

        protected override Media.Brush? GetThemeBackgroundBrush(PortableTheme theme)
        {
            return theme.ButtonBackgroundBrush;
        }

        protected override ShadowStyle? GetThemeShadow(PortableTheme theme)
        {
            return UseThemeStyle ? theme.ButtonShadow : null;
        }

        protected override ControlVisualState GetVisualState()
        {
            if (!IsEnabled)
                return ControlVisualState.Disabled;
            if (IsPressedVisualState())
                return ControlVisualState.Pressed;
            if (ShowsHoverLook)
                return ControlVisualState.Hover;
            if (IsFocused)
                return ControlVisualState.Focused;
            return ControlVisualState.Normal;
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (Padding.Equals(oldTheme.ButtonPadding))
                Padding = newTheme.ButtonPadding;
            if (oldTheme.ButtonMargin is { } oldMargin && Margin.Equals(oldMargin))
                Margin = newTheme.ButtonMargin ?? new Thickness(0);
            else if (oldTheme.ButtonMargin == null && newTheme.ButtonMargin is { } newMargin && Margin.Equals(new Thickness(0)))
                Margin = newMargin;
            if (ReferenceEquals(HoverColor, oldTheme.ButtonHoverBrush))
                HoverColor = newTheme.ButtonHoverBrush;
            if (ReferenceEquals(PressedColor, oldTheme.ButtonPressedBrush))
                PressedColor = newTheme.ButtonPressedBrush;
            if (TextColor.Equals(oldTheme.ButtonTextColor))
            {
                TextColor = newTheme.ButtonTextColor;
                _textColorFromTheme = true;
            }
            if (Nullable.Equals(HoverTextColor, oldTheme.ButtonHoverTextColor))
                HoverTextColor = newTheme.ButtonHoverTextColor;
            if (Nullable.Equals(PressedTextColor, oldTheme.ButtonPressedTextColor))
                PressedTextColor = newTheme.ButtonPressedTextColor;
            if (Nullable.Equals(DisabledTextColor, oldTheme.DisabledTextColor))
                DisabledTextColor = newTheme.DisabledTextColor;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            var context = new BrushContext(rect, CornerRadius, RenderOpacity, spriteBatch.GraphicsDevice, (float)ScreenSystem.TotalTime.TotalSeconds, null, ToRender(1f));
            var theme = _variant != ButtonVariant.Standard ? ResolveTheme() : null;
            if (EffectiveRipple is { } ripple)
            {
                // The ripple is the pressed look; hover keeps its overlay.
                DrawRipple(spriteBatch, rect, ripple);
                if (ShowsHoverLook && !IsPressedVisualState())
                    DrawStateOverlay(spriteBatch, HoverColor, in context);
                return;
            }
            if (IsPressedVisualState())
                DrawStateOverlay(spriteBatch, theme != null && ReferenceEquals(PressedColor, theme.ButtonPressedBrush) ? theme.VariantButtonPressedBrush : PressedColor, in context);
            else if (ShowsHoverLook)
                DrawStateOverlay(spriteBatch, theme != null && ReferenceEquals(HoverColor, theme.ButtonHoverBrush) ? theme.VariantButtonHoverBrush : HoverColor, in context);
        }

        private void DrawStateOverlay(SpriteBatch spriteBatch, Brush brush, in BrushContext context)
        {
            // Cut-corner faces: the overlay takes the same shape instead of refilling the corners.
            if (BackgroundBrush is ChamferBrush chamfer && brush is SolidColorBrush solid)
            {
                chamfer.DrawShape(spriteBatch, in context, solid.Color);
                return;
            }

            // Backdrop brushes (frosted glass/acrylic) draw square; on rounded buttons that reads
            // as the button losing its corners — use their solid stand-in with the proper radius.
            if (!context.Radius.IsEmpty && brush.RoundedFallbackColor is { } fallback)
            {
                RoundedRectRenderer.DrawSolid(spriteBatch, context.Rect, context.Radius, Media.Brush.ApplyOpacity(fallback, RenderOpacity));
                return;
            }

            brush.Draw(spriteBatch, in context);
        }

        /// <summary>
        ///     Material ripple shown while pressed instead of <see cref="PressedColor"/>; null uses the theme's
        ///     <see cref="PortableTheme.ButtonRipple"/> (chrome buttons such as list rows:
        ///     <see cref="PortableTheme.ItemRipple"/>), which are off by default.
        /// </summary>
        public RippleStyle? Ripple { get; set; }

        private RippleStyle? EffectiveRipple
            => Ripple ?? (UseThemeStyle ? ResolveTheme().ButtonRipple : ResolveTheme().ItemRipple);

        // The running ripple: its origin as a fraction of the control, when it started and was released.
        private Vector2 _rippleOrigin;
        private TimeSpan _rippleStart;
        private TimeSpan? _rippleRelease;
        private bool _rippleActive;

        private void StartRipple()
        {
            var bounds = ClippingRect;
            var press = LastPressPosition;
            _rippleOrigin = bounds.Width > 0 && bounds.Height > 0 && bounds.Contains(press)
                ? new Vector2((press.X - bounds.Left) / bounds.Width, (press.Y - bounds.Top) / bounds.Height)
                : new Vector2(0.5f, 0.5f);
            _rippleStart = ScreenSystem.TotalTime;
            _rippleRelease = null;
            _rippleActive = true;
        }

        private void DrawRipple(SpriteBatch spriteBatch, Rect rect, RippleStyle ripple)
        {
            if (!_rippleActive)
                return;
            var now = ScreenSystem.TotalTime;
            var expand = Math.Max(1, ripple.ExpandDuration.TotalMilliseconds);
            var grow = (float)Math.Min(1, (now - _rippleStart).TotalMilliseconds / expand);
            var opacity = 1f;
            if (_rippleRelease is { } released)
            {
                // Fades once it has grown: a quick tap still shows the full circle.
                var fadeStart = released > _rippleStart + ripple.ExpandDuration ? released : _rippleStart + ripple.ExpandDuration;
                var fade = (now - fadeStart).TotalMilliseconds / Math.Max(1, ripple.FadeDuration.TotalMilliseconds);
                if (fade >= 1)
                {
                    _rippleActive = false;
                    return;
                }
                opacity = (float)Math.Clamp(1 - fade, 0, 1);
            }

            var clip = ChromeRect(rect);
            var center = new Vector2(clip.Left + clip.Width * _rippleOrigin.X, clip.Top + clip.Height * _rippleOrigin.Y);
            // Material's decelerating growth up to the farthest corner.
            var reach = MathF.Sqrt(MathF.Pow(Math.Max(center.X - clip.Left, clip.Right - center.X), 2)
                + MathF.Pow(Math.Max(center.Y - clip.Top, clip.Bottom - center.Y), 2));
            // The soft edge lies outside the reach at full size, so the whole control ends up covered.
            reach /= 1 - Math.Clamp(ripple.EdgeSoftness, 0, 0.9f);
            var eased = 1 - (1 - grow) * (1 - grow);
            var radius = reach * (0.1f + 0.9f * eased);
            RippleRenderer.Draw(spriteBatch, clip, ToRender(CornerRadius), center, radius, ripple.EdgeSoftness, Media.Brush.ApplyOpacity(ripple.Color, RenderOpacity * opacity));
            ScreenEngine.RequestAnimationFrame();
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
                _textColorFromTheme = false;
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

            // The label fades between state colours with the face (theme style's TransitionDuration).
            textBlock.OwnerTextColorTransition = ResolveStyle()?.TransitionDuration ?? TimeSpan.Zero;
            var color = TextColor;
            if (_variant != ButtonVariant.Standard && _textColorFromTheme)
            {
                // Variant faces are colored: the slot's per-state text color wins over the
                // standard button's hover/pressed snapshots, which are meant for a neutral face.
                color = ResolveStateStyle()?.TextColor ?? color;
                if (!IsEnabled && DisabledTextColor != null)
                    color = (Color)DisabledTextColor;
                textBlock.TextColor = color;
                return;
            }
            if (!IsEnabled && DisabledTextColor != null)
                color = (Color)DisabledTextColor;
            else if (ShowsHoverLook && HoverTextColor != null)
                color = (Color)HoverTextColor;
            if (IsEnabled && isPressed && PressedTextColor != null)
                color = (Color)PressedTextColor;
            textBlock.TextColor = color;
        }

        private void UpdatePressedAnimation(bool isPressed)
        {
            // A ripple is the pressed feedback; the button does not also shrink (Material).
            if (!AnimatePressedState || EffectiveRipple != null || _isPressedVisualState == isPressed)
                return;

            _isPressedVisualState = isPressed;
            // The style's transition duration drives the press animation (press a bit faster than
            // release); zero (reduced-motion themes) jumps straight to the target.
            var transition = ResolveStyle()?.TransitionDuration ?? TimeSpan.FromMilliseconds(120);
            var duration = TimeSpan.FromMilliseconds(transition.TotalMilliseconds * (isPressed ? 0.6 : 0.75));
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

            if (duration <= TimeSpan.Zero)
            {
                Scale = targetScale;
                Translation = targetTranslation;
                return;
            }

            this.Animate()
                .Scale(targetScale)
                .TranslateTo(targetTranslation)
                .Duration(duration)
                .Ease(Easings.CubicOut)
                .Start();
        }

        private bool _appliedPressedVisual;

        // The touch pressed look switches on (after the delay) and off (after tap feedback) with
        // time, not with an input event: re-apply the visual state when it flips.
        internal override void OnFrameUpdate()
        {
            base.OnFrameUpdate();
            var pressed = IsPressedVisualState();
            if (pressed != _appliedPressedVisual)
            {
                if (pressed)
                    StartRipple();
                else if (_rippleActive)
                    _rippleRelease = ScreenSystem.TotalTime;
                _appliedPressedVisual = pressed;
                ChangeVisualState();
                RequestRedraw();
            }
        }

        /// <summary>Draws the pressed look regardless of input (a list's touch hold highlight).</summary>
        internal bool ForcePressedVisual
        {
            get => _forcePressedVisual;
            set
            {
                if (_forcePressedVisual == value)
                    return;
                _forcePressedVisual = value;
                InvalidateLayout(false);
            }
        }

        private bool _forcePressedVisual;

        private bool IsPressedVisualState()
        {
            return ForcePressedVisual || HoverState == HoverStates.Hovering && MouseButtonStates[MouseButton.Left] == ButtonState.Pressed
                || IsTouchPressedVisual;
        }

        #endregion
    }
}
