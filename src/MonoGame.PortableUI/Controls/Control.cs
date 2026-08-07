using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Effects;
using MonoGame.PortableUI.Exceptions;
using MonoGame.PortableUI.Media;
using ControlAnimation = MonoGame.PortableUI.Animation.Animation;

namespace MonoGame.PortableUI.Controls
{
    public abstract class Control : UIElement
    {
        private readonly Timer _longPressTimer;
        private readonly Timer _toolTipHoverTimer;
        private readonly Timer _toolTipLongPressTimer;
        private readonly List<ControlAnimation> _animations = new List<ControlAnimation>();

        private ContextMenu? _contextMenu;
        private float _height;
        private float _maxHeight;
        private float _maxWidth;
        private float _minHeight;
        private float _minWidth;
        private FrameworkElement? _parent;
        private float _width;
        private bool _isEnabled;
        private TimeSpan? _lastClickAt;
        private TimeSpan? _lastRightClickAt;
        private PointF _lastToolTipAnchorPosition;
        private bool _suppressUpdate;
        private string? _toolTip;
        private long _resolvedThemeVersion = -1;
        private PortableTheme? _resolvedTheme;
        private PortableTheme? _appliedTheme;
        private static readonly ScreenEngineOptions DefaultOptions = new ScreenEngineOptions();
        public bool HandleTouchDownEnter { get; set; }

        protected Control()
        {
            var theme = PortableTheme.ResolveCurrent();

            SnapToPixel = theme.PixelSnapping;
            Opacity = 1;
            IsEnabled = true;
            IsVisible = true;
            Scale = new Vector2(1, 1);
            Translation = new Vector2();
            Margin = new Thickness(0);
            Width = Size.Auto;
            Height = Size.Auto;
            MinWidth = 0;
            MinHeight = 0;
            MaxWidth = Size.Infinity;
            MaxHeight = Size.Infinity;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Position = new PointF(0, 0);
            FocusBorderBrush = theme.FocusBorderBrush;
            FocusBorderWidth = theme.FocusBorderWidth;
            FocusVisualKind = theme.FocusVisualKind;
            DisabledOverlayBrush = theme.DisabledOverlayBrush;
            // BorderThickness/CornerRadius intentionally stay unset so theme styles can supply them.
            _appliedTheme = theme;

            _longPressTimer = new Timer(300);
            _longPressTimer.Elapsed += OnLongPressTimerElapsed;
            _toolTipHoverTimer = new Timer(0);
            _toolTipHoverTimer.Elapsed += OnToolTipHoverTimerElapsed;
            _toolTipLongPressTimer = new Timer(0);
            _toolTipLongPressTimer.Elapsed += OnToolTipLongPressTimerElapsed;
        }

        private static readonly MouseButton[] AllMouseButtons = { MouseButton.Left, MouseButton.Middle, MouseButton.Right };

        protected Dictionary<MouseButton, ButtonState> MouseButtonStates { get; } = new Dictionary<MouseButton, ButtonState>
        {
            {MouseButton.Left, ButtonState.Released},
            {MouseButton.Middle, ButtonState.Released},
            {MouseButton.Right, ButtonState.Released}
        };

        public bool IsFocused
        {
            get { return ScreenEngine.FocusedControl == this; }
        }

        /// <summary>Whether pointer-down moves keyboard focus to this control. Non-interactive
        /// controls (TextBlock, Image, Border, panels, progress indicators) default to false so
        /// clicking them doesn't blur an active TextBox and hide the soft keyboard. A direct
        /// <see cref="Focus"/> call is programmatic and ignores this flag.</summary>
        public bool IsFocusable { get; set; } = true;

        internal Screen? Screen
        {
            get { return Parent as Screen ?? (Parent as Control)?.Screen; }
        }

        /// <summary>Routes subsequent mouse move/up events to this control even when the
        /// pointer leaves its bounds, until <see cref="ReleaseMouse"/> is called.</summary>
        protected void CaptureMouse() => Screen?.CaptureMouse(this);

        protected void ReleaseMouse() => Screen?.ReleaseMouse(this);

        public object? Tag { get; set; }

        public string? ToolTip
        {
            get { return _toolTip; }
            set
            {
                var normalized = string.IsNullOrEmpty(value) ? null : value;
                if (_toolTip == normalized)
                    return;

                _toolTip = normalized;
                if (_toolTip == null)
                    ClearToolTipState();
            }
        }

        public ContextMenu? ContextMenu
        {
            get { return _contextMenu; }
            set
            {
                LongTouch -= ShowContextMenuTouch;
                MouseDown -= ShowContextMenuDown;
                RightClick -= ShowContextMenuClick;
                Click -= ShowContextMenuLeftClick;
                _contextMenu = value;
                if (_contextMenu == null)
                    return;
                LongTouch += ShowContextMenuTouch;
                MouseDown += ShowContextMenuDown;
                RightClick += ShowContextMenuClick;
                Click += ShowContextMenuLeftClick;
            }
        }

        protected HoverStates HoverState { get; set; }
        protected TouchStates TouchState { get; set; }
        internal bool IsMouseHovering => HoverState == HoverStates.Hovering;

        public override FrameworkElement? Parent
        {
            get { return _parent; }
            internal set
            {
                if (_parent == value)
                    return;
                if (_parent != null && value != null)
                    throw new MultipleParentException();
                _parent = value;
            }
        }

        public float Width
        {
            get { return _width; }
            set
            {
                if (Math.Abs(_width - value) < float.Epsilon)
                    return;
                _width = value;
                InvalidateLayout(true);
            }
        }

        public float Height
        {
            get { return _height; }
            set
            {
                if (Math.Abs(_height - value) < float.Epsilon)
                    return;
                _height = value;
                InvalidateLayout(true);
            }
        }

        public float MinWidth
        {
            get { return _minWidth; }
            set
            {
                _minWidth = value;
                if (MaxWidth.IsFixed() && MaxWidth < _minWidth)
                    _maxWidth = _minWidth;
                InvalidateLayout(true);
            }
        }

        public float MaxWidth
        {
            get { return _maxWidth; }
            set
            {
                _maxWidth = value;
                if (_maxWidth.IsFixed() && _maxWidth < MinWidth)
                    _minWidth = _maxWidth;
                InvalidateLayout(true);
            }
        }

        public float MinHeight
        {
            get { return _minHeight; }
            set
            {
                _minHeight = value;
                if (MaxHeight.IsFixed() && MaxHeight < _minHeight)
                    _maxHeight = _minHeight;
                InvalidateLayout(true);
            }
        }

        public float MaxHeight
        {
            get { return _maxHeight; }
            set
            {
                _maxHeight = value;
                if (_maxHeight.IsFixed() && _maxHeight < MinHeight)
                    _minHeight = _maxHeight;
                InvalidateLayout(true);
            }
        }

        public Rect BoundingRect { get; protected set; }
        public Rect ClientRect { get; protected set; }
        public Rect ClippingRect { get; protected set; }

        /// <summary>
        ///     When true, descendants are scissor-clipped to this control's bounds (e.g. ScrollViewer).
        ///     Default is false so effects like drop shadows can render outside ancestor bounds.
        /// </summary>
        protected internal virtual bool ClipsDescendants => false;

        public Thickness Margin { get; set; }

        public Vector2 Scale { get; set; }

        public Vector2 Translation { get; set; }

        public double Opacity { get; set; }

        protected float RenderOpacity { get; private set; } = 1;

        protected Vector2 RenderScale { get; private set; } = Vector2.One;

        public bool ShowFocusVisual { get; set; }

        public Brush? FocusBorderBrush { get; set; }

        public float FocusBorderWidth { get; set; }

        public FocusVisualKind FocusVisualKind { get; set; }

        public Brush? DisabledOverlayBrush { get; set; }

        private Brush? _backgroundBrushOverride;
        private bool _backgroundBrushSet;
        private Brush? _borderBrushOverride;
        private bool _borderBrushSet;
        private Thickness? _borderThicknessOverride;
        private CornerRadius? _cornerRadiusOverride;
        private ShadowStyle? _shadowOverride;
        private bool _shadowSet;

        // Visual properties resolve theme StateStyle values live unless explicitly set —
        // explicit assignments (user code or control constructors) always win (T2).
        public override Brush? BackgroundBrush
        {
            get => _backgroundBrushSet ? _backgroundBrushOverride : ResolveStateStyle()?.Background ?? GetThemeBackgroundBrush(ResolveTheme());
            set
            {
                _backgroundBrushOverride = value;
                _backgroundBrushSet = true;
            }
        }

        public Brush? BorderBrush
        {
            get => _borderBrushSet ? _borderBrushOverride : ResolveStateStyle()?.BorderBrush;
            set
            {
                _borderBrushOverride = value;
                _borderBrushSet = true;
            }
        }

        public Thickness BorderThickness
        {
            get => _borderThicknessOverride ?? ResolveStateStyle()?.BorderThickness ?? default;
            set => _borderThicknessOverride = value;
        }

        /// <summary>
        /// When both this and <see cref="BorderBevelDark"/> are set, the (rounded) border is drawn as a
        /// diagonal bevel — this colour at the top-left blending to <see cref="BorderBevelDark"/> at the
        /// bottom-right — instead of a flat <see cref="BorderBrush"/>. Gives glass panels a lit edge.
        /// </summary>
        public Color? BorderBevelLight { get; set; }

        public Color? BorderBevelDark { get; set; }

        public CornerRadius CornerRadius
        {
            get => _cornerRadiusOverride ?? ResolveStateStyle()?.CornerRadius ?? default;
            set => _cornerRadiusOverride = value;
        }

        public ShadowStyle? Shadow
        {
            get
            {
                if (_shadowSet)
                    return _shadowOverride;
                var shadows = ResolveStateStyle()?.Shadows;
                return shadows is { Length: > 0 } ? shadows[0] : GetThemeShadow(ResolveTheme());
            }
            set
            {
                _shadowOverride = value;
                _shadowSet = true;
            }
        }

        /// <summary>Per-control style override; when null the control uses its theme style slot.</summary>
        public ControlStyle? Style { get; set; }

        /// <summary>The theme style slot this control consumes (e.g. Button → theme.Button); null = unstyled.</summary>
        protected virtual ControlStyle? GetThemeStyle(PortableTheme theme) => null;

        /// <summary>Flat-theme fallback used when neither an override nor a style background exists.</summary>
        protected virtual Brush? GetThemeBackgroundBrush(PortableTheme theme) => null;

        /// <summary>Flat-theme fallback shadow (e.g. theme.ButtonShadow).</summary>
        protected virtual ShadowStyle? GetThemeShadow(PortableTheme theme) => null;

        /// <summary>The state used to pick the StateStyle; interactive controls override this.</summary>
        protected virtual ControlVisualState GetVisualState()
        {
            if (!IsEnabled)
                return ControlVisualState.Disabled;
            if (IsFocused)
                return ControlVisualState.Focused;
            return ControlVisualState.Normal;
        }

        internal ControlStyle? ResolveStyle()
        {
            return Style ?? GetThemeStyle(ResolveTheme());
        }

        protected StateStyle? ResolveStateStyle()
        {
            return ResolveStyle()?.GetResolved(GetVisualState());
        }

        internal Control? ThemeOwner { get; set; }

        public BackdropMode BackdropMode { get; set; } = BackdropMode.Layered;

        public void SuppressUpdate(bool suppress)
        {
            _suppressUpdate = suppress;
        }

        protected internal PortableTheme ResolveTheme()
        {
            if (ThemeOwner != null && !ReferenceEquals(ThemeOwner, this))
                return ThemeOwner.ResolveTheme();

            var version = ThemeVersion.Current;
            if (_resolvedTheme != null && _resolvedThemeVersion == version)
                return _resolvedTheme;

            var parent = (FrameworkElement?)this;
            while (parent != null)
            {
                if (parent is ThemeIsland { Theme: { } islandTheme })
                    return CacheResolvedTheme(islandTheme, version);

                parent = parent.Parent;
            }

            var theme = Screen?.ScreenEngine?.Options.Theme
                ?? ScreenEngine.Instance?.Options.Theme
                ?? PortableTheme.ResolveCurrent();
            return CacheResolvedTheme(theme, version);
        }

        private PortableTheme CacheResolvedTheme(PortableTheme theme, long version)
        {
            _resolvedTheme = theme;
            _resolvedThemeVersion = version;
            return theme;
        }

        /// <summary>
        ///     T3 live switching: re-seeds theme-derived snapshots when the resolved theme changed
        ///     (Options.Theme setter, ThemeIsland reassignment, or the control moving into an island).
        ///     Values the user overrode — no longer equal to the previous theme's value — are kept.
        /// </summary>
        internal void RefreshThemeResources()
        {
            var current = ResolveTheme();
            if (ReferenceEquals(_appliedTheme, current))
                return;

            var previous = _appliedTheme;
            _appliedTheme = current;
            if (previous != null)
                OnThemeChanged(previous, current);
        }

        /// <summary>
        ///     Re-apply constructor theme snapshots: for each themed property, assign the new
        ///     theme's value only when the current value still equals the old theme's value
        ///     (reference equality for brushes — theme brushes are shared instances).
        /// </summary>
        protected virtual void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            if (ReferenceEquals(FocusBorderBrush, oldTheme.FocusBorderBrush))
                FocusBorderBrush = newTheme.FocusBorderBrush;
            if (FocusBorderWidth.Equals(oldTheme.FocusBorderWidth))
                FocusBorderWidth = newTheme.FocusBorderWidth;
            if (FocusVisualKind == oldTheme.FocusVisualKind)
                FocusVisualKind = newTheme.FocusVisualKind;
            if (ReferenceEquals(DisabledOverlayBrush, oldTheme.DisabledOverlayBrush))
                DisabledOverlayBrush = newTheme.DisabledOverlayBrush;
            if (SnapToPixel == oldTheme.PixelSnapping)
                SnapToPixel = newTheme.PixelSnapping;
        }

        /// <summary>Helper for OnThemeChanged implementations: re-seed a brush-typed property.</summary>
        protected static void SwapThemeBrush<T>(ref T? field, T? oldValue, T? newValue) where T : Brush
        {
            if (ReferenceEquals(field, oldValue))
                field = newValue;
        }

        /// <summary>Reverts an explicit BackgroundBrush assignment back to theme/style resolution.</summary>
        internal void ClearBackgroundBrushOverride()
        {
            _backgroundBrushSet = false;
            _backgroundBrushOverride = null;
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled == value)
                    return;

                _isEnabled = value;
                if (!_isEnabled)
                {
                    if (ScreenEngine.FocusedControl == this)
                        ScreenEngine.FocusedControl = null;
                    ResetInputs();
                }
                else
                {
                    ChangeVisualState();
                }
                InvalidateLayout(false);
            }
        }

        /// <summary>
        ///     When false, this control and its whole subtree are ignored by input routing
        ///     (hover, clicks, scroll, touch, focus-by-click, tooltips) while still rendering
        ///     and animating normally. Like WPF's IsHitTestVisible.
        /// </summary>
        public bool IsHitTestVisible { get; set; } = true;

        /// <summary>When true this control can be a drop target: it receives DragEnter/DragOver/DragLeave/Drop.</summary>
        public bool AllowDrop { get; set; }

        public event DragEventHandler? DragEnter;
        public event DragEventHandler? DragOver;
        public event DragEventHandler? DragLeave;
        public event DragEventHandler? Drop;

        /// <summary>Starts a drag &amp; drop operation with this control as the source (see <see cref="DragDrop.DoDragDrop"/>).</summary>
        public DragOperation? BeginDrag(object? payload, DragDropEffects allowedEffects, Control? dragVisual = null)
        {
            return Screen?.BeginDrag(this, payload, allowedEffects, dragVisual);
        }

        protected internal virtual void OnDragEnter(DragEventArgs args)
        {
            DragEnter?.Invoke(this, args);
        }

        protected internal virtual void OnDragOver(DragEventArgs args)
        {
            DragOver?.Invoke(this, args);
        }

        protected internal virtual void OnDragLeave(DragEventArgs args)
        {
            DragLeave?.Invoke(this, args);
        }

        protected internal virtual void OnDrop(DragEventArgs args)
        {
            Drop?.Invoke(this, args);
        }

        public HorizontalAlignment HorizontalAlignment { get; set; }

        public VerticalAlignment VerticalAlignment { get; set; }

        public bool SnapToPixel { get; set; }
        
        internal PointF Position { get; set; }

        private void ShowContextMenuTouch(object? sender, EventArgs e) { ShowContextMenu(true); }
        private void ShowContextMenuClick(object? sender, EventArgs args) { if (ContextMenu?.ContextMenuType == ContextMenuTypes.OpenAndClick) ShowContextMenu(false); }
        private void ShowContextMenuDown(object? sender, MouseEventArgs args) { if (args.Buttons.Any(x => x == MouseButton.Right) && ContextMenu?.ContextMenuType == ContextMenuTypes.OpenAndHold) ShowContextMenu(false); }
        private void ShowContextMenuLeftClick(object? sender, EventArgs args) { if (ContextMenu?.ContextMenuType == ContextMenuTypes.OpenOnLeftClick) ShowContextMenu(false); }

        /// <summary>Opens the assigned <see cref="ContextMenu"/> programmatically (e.g. from a menu button).</summary>
        public void OpenContextMenu(bool optimizeForTouch = false)
        {
            ShowContextMenu(optimizeForTouch);
        }

        private void ShowContextMenu(bool optimizeForTouch)
        {
            var boundingRect = BoundingRect - Margin;
            var pointF = boundingRect;
            if (Screen != null && ContextMenu != null)
                Screen.CreateContextMenu(pointF.Offset, ContextMenu, optimizeForTouch, this);
        }

        public override void InvalidateLayout(bool boundsChanged)
        {
            if (_suppressUpdate)
                return;
            Parent?.InvalidateLayout(boundsChanged);
        }

        public override IEnumerable<Control> GetDescendants()
        {
            return Enumerable.Empty<Control>();
        }

        protected internal virtual bool CapturesInputBeforeDescendants(BaseEventArgs args)
        {
            return false;
        }

        public virtual void OnClick()
        {
            Click?.Invoke(this, EventArgs.Empty);
            if (IsDoubleClick(ref _lastClickAt))
                DoubleClick?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Raises Click when Enter or Space is pressed. Clickable controls (Button,
        /// CheckBox, ToggleSwitch) subscribe this to their own KeyPressed so the focused control
        /// can be activated from the keyboard, matching mouse/touch behavior.</summary>
        private protected void ActivateOnKeyPressed(object? sender, KeyEventArgs args)
        {
            if (args.Modifiers != KeyboardModifiers.None)
                return;

            if ((args.InputType == InputType.Command && args.Command == KeyboardCommand.Enter)
                || (args.InputType == InputType.Char && args.Char == ' '))
                OnClick();
        }

        public virtual void OnRightClick()
        {
            RightClick?.Invoke(this, EventArgs.Empty);
            if (IsDoubleClick(ref _lastRightClickAt))
                RightDoubleClick?.Invoke(this, EventArgs.Empty);
        }

        private void OnLongPressTimerElapsed(object? sender, EventArgs e)
        {
            _longPressTimer?.Stop();
            _toolTipLongPressTimer.Stop();
            TouchState = TouchStates.Released;
            ChangeVisualState();
            LongTouch?.Invoke(this, EventArgs.Empty);
        }

        private void OnToolTipHoverTimerElapsed(object? sender, EventArgs e)
        {
            ShowToolTip(_lastToolTipAnchorPosition);
        }

        private void OnToolTipLongPressTimerElapsed(object? sender, EventArgs e)
        {
            _toolTipLongPressTimer.Stop();
            if (!HasToolTip || ContextMenu != null)
                return;

            TouchState = TouchStates.Released;
            ChangeVisualState();
            ShowToolTip(_lastToolTipAnchorPosition);
        }

        protected internal virtual void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            if (Shadow != null && !Shadow.Inset)
                ShadowRenderer.Draw(spriteBatch, rect, CornerRadius, Shadow, RenderOpacity);

            if (BackgroundBrush != null)
            {
                var context = new BrushContext(rect, CornerRadius, RenderOpacity, spriteBatch.GraphicsDevice, (float)ScreenSystem.TotalTime.TotalSeconds);
                BackgroundBrush.Draw(spriteBatch, in context);
            }

            if (Shadow != null && Shadow.Inset)
                ShadowRenderer.Draw(spriteBatch, rect, CornerRadius, Shadow, RenderOpacity);

            if (BorderBevelLight is { } bevelLight && BorderBevelDark is { } bevelDark && HasBorder(BorderThickness) && !CornerRadius.IsEmpty)
            {
                RoundedRectRenderer.DrawBevelBorder(spriteBatch, rect, CornerRadius, BorderThickness, bevelLight, bevelDark, RenderOpacity);
            }
            else if (BorderBrush != null && HasBorder(BorderThickness))
            {
                if (!CornerRadius.IsEmpty && BorderBrush is SolidColorBrush solidBorder)
                    RoundedRectRenderer.DrawBorder(spriteBatch, rect, CornerRadius, BorderThickness, Brush.ApplyOpacity(solidBorder.Color, RenderOpacity));
                else
                    DrawBorder(spriteBatch, rect, BorderThickness, BorderBrush, RenderOpacity);
            }
        }

        private bool? _overridesDrawOverlay;

        /// <summary>Whether the renderer must open the second per-control SpriteBatch for
        /// <see cref="OnDrawOverlay"/> this frame. Skipping it for the common case (no override,
        /// enabled, unfocused) halves the per-control Begin/End pairs.</summary>
        internal bool NeedsOverlayPass
        {
            get
            {
                _overridesDrawOverlay ??= GetType().GetMethod(
                        nameof(OnDrawOverlay),
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                        binder: null,
                        new[] { typeof(SpriteBatch), typeof(Rect) },
                        modifiers: null)!
                    .DeclaringType != typeof(Control);
                if (_overridesDrawOverlay.Value)
                    return true;

                if (ShowFocusVisual && IsFocused && FocusBorderWidth > 0 && FocusBorderBrush != null)
                    return true;

                return !IsEnabled && DisabledOverlayBrush != null;
            }
        }

        protected internal virtual void OnDrawOverlay(SpriteBatch spriteBatch, Rect rect)
        {
            if (ShowFocusVisual && IsFocused && FocusBorderWidth > 0 && FocusBorderBrush != null)
                DrawFocusVisual(spriteBatch, rect, FocusBorderWidth, FocusBorderBrush, FocusVisualKind, RenderOpacity);

            if (!IsEnabled && DisabledOverlayBrush is { } disabledOverlay)
            {
                // Carry the corner radius so the dim overlay follows the control's rounded shape;
                // the plain (rect, opacity) overload draws a square that spills past the corners.
                var context = new BrushContext(rect, CornerRadius, RenderOpacity, spriteBatch.GraphicsDevice, (float)ScreenSystem.TotalTime.TotalSeconds);
                disabledOverlay.Draw(spriteBatch, in context);
            }
        }


        public virtual void UpdateLayout(Rect rect)
        {
            if (_suppressUpdate)
                return;

            if (IsGone)
                BoundingRect = Rect.Empty;

            var measuredSize = MeasureLayout();
            var offset = rect.Offset;

            BoundingRect = GetRectForAlignment(rect, measuredSize, offset);
            ClippingRect = BoundingRect - Margin;
        }

        protected Rect GetRectForAlignment(Rect rect, Size measuredSize, PointF offset)
        {
            switch (VerticalAlignment)
            {
                case VerticalAlignment.Stretch:
                    if (!Height.IsFixed() && rect.Height.IsFixed()) measuredSize.Height = rect.Height;
                    break;
                case VerticalAlignment.Center:
                    offset.Y += (rect.Height - measuredSize.Height) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    offset.Y += rect.Height - measuredSize.Height;
                    break;
            }

            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Stretch:
                    if (!Width.IsFixed() && rect.Width.IsFixed()) measuredSize.Width = rect.Width;
                    break;
                case HorizontalAlignment.Center:
                    offset.X += (rect.Width - measuredSize.Width) / 2;
                    break;
                case HorizontalAlignment.Right:
                    offset.X += rect.Width - measuredSize.Width;
                    break;
            }
            measuredSize = ApplyConstraints(measuredSize);
            return new Rect(offset, measuredSize);
        }

        public virtual Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            var width = Width.IsFixed() ? Width : 0;
            var height = Height.IsFixed() ? Height : 0;

            return ApplyConstraints(new Size(width, height)) + Margin;
        }

        protected Size ApplyConstraints(Size size)
        {
            if (MinWidth.IsFixed())
                size.Width = Math.Max(size.Width, MinWidth);
            if (MinHeight.IsFixed())
                size.Height = Math.Max(size.Height, MinHeight);
            if (MaxWidth.IsFixed())
                size.Width = Math.Min(size.Width, MaxWidth);
            if (MaxHeight.IsFixed())
                size.Height = Math.Min(size.Height, MaxHeight);
            return size;
        }

        protected virtual void OnLongTouch()
        {
            LongTouch?.Invoke(this, EventArgs.Empty);
        }

        internal virtual void ChangeVisualState()
        {
        }

        #region Events

        public event MouseEventHandler? MouseEnter;
        public event MouseEventHandler? MouseLeave;
        public event MouseEventHandler? MouseMove;
        public event MouseEventHandler? MouseDown;
        public event MouseEventHandler? MouseUp;
        public event ScrollWheelChangedEventHandler? ScrollWheelChanged;
        public event TouchEventHandler? TouchDown;
        public event TouchEventHandler? TouchUp;
        public event TouchEventHandler? TouchMove;
        public event TouchEventHandler? TouchCancel;

        public event EventHandler? Click;
        public event EventHandler? DoubleClick;
        public event EventHandler? RightClick;
        public event EventHandler? RightDoubleClick;
        public event EventHandler? LongTouch;
        public event KeyEventHandler? KeyPressed;
        public event KeyEventHandler? KeyDown;
        public event KeyEventHandler? KeyUp;

        public event GotFocusEventHandler? GotFocus;
        public event LostFocusEventHandler? LostFocus;

        #endregion

        #region Event handlers

        internal void OnMouseEnter(MouseEventArgs args)
        {
            HoverState = HoverStates.Hovering;
            foreach (var button in AllMouseButtons)
            {
                if (MouseButtonStates[button] == ButtonState.Pressed && !args.Buttons.Contains(button))
                    MouseButtonStates[button] = ButtonState.Released;
            }
            MouseEnter?.Invoke(this, args);
            StartToolTipHover(args.Position);
            ChangeVisualState();
        }

        internal void OnMouseLeave(MouseEventArgs args)
        {
            HoverState = HoverStates.NotHovering;
            ClearToolTipState();
            MouseLeave?.Invoke(this, args);
            ChangeVisualState();
        }

        internal void OnMouseDown(MouseEventArgs args)
        {
            ClearToolTipState();
            foreach (var button in args.Buttons)
                MouseButtonStates[button] = ButtonState.Pressed;
            // Only a primary click on a focusable control moves focus; right-clicks and clicks on
            // labels/decoration must not blur an active TextBox (which would hide the soft keyboard).
            if (IsFocusable && args.Buttons.Contains(MouseButton.Left))
                Focus();
            MouseDown?.Invoke(this, args);
            ChangeVisualState();
            if ((Click != null && args.Buttons.Contains(MouseButton.Left)) || (RightClick != null && args.Buttons.Contains(MouseButton.Right)))
                args.Handled = true;
        }


        internal void OnMouseUp(MouseEventArgs args)
        {
            var changed = new List<MouseButton>();
            foreach (var button in args.Buttons)
            {
                if (MouseButtonStates[button] == ButtonState.Pressed)
                {
                    MouseButtonStates[button] = ButtonState.Released;
                    changed.Add(button);
                }

            }
            MouseUp?.Invoke(this, args);
            ChangeVisualState();
            if (Click != null && changed.Contains(MouseButton.Left))
            {
                OnClick();
                args.Handled = true;
            }
            if (RightClick != null && changed.Contains(MouseButton.Right))
            {
                OnRightClick();
                args.Handled = true;
            }
        }

        internal void OnTouchDown(TouchEventArgs args)
        {
            TouchState = TouchStates.Touched;
            StartToolTipLongPress(args.Position);
            TouchDown?.Invoke(this, args);
            ChangeVisualState();
            if (LongTouch != null)
                _longPressTimer?.Start();

            if (Click != null)
                args.Handled = true;
        }

        internal void OnTouchUp(TouchEventArgs args)
        {
            _longPressTimer.Stop();
            _toolTipLongPressTimer.Stop();
            Screen?.ClearToolTip(this);
            if (TouchState == TouchStates.Touched)
            {
                TouchState = TouchStates.Released;
                TouchUp?.Invoke(this, args);
                ChangeVisualState();

                if (Click != null)
                {
                    OnClick();
                    args.Handled = true;
                }
            }
            else
                TouchUp?.Invoke(this, args);
        }

        internal void OnTouchMove(TouchEventArgs args)
        {
            _lastToolTipAnchorPosition = args.Position;
            Screen?.UpdateToolTip(this, args.Position);
            if (HandleTouchDownEnter)
            {
                TouchState = TouchStates.Touched;
                ChangeVisualState();
            }
            TouchMove?.Invoke(this, args);
        }

        internal void OnMouseMove(MouseEventArgs args)
        {
            _lastToolTipAnchorPosition = args.Position;
            Screen?.UpdateToolTip(this, args.Position);
            MouseMove?.Invoke(this, args);
        }

        internal void OnTouchCancel(TouchEventArgs args)
        {
            _longPressTimer.Stop();
            ClearToolTipState();
            TouchState = TouchStates.Released;
            TouchCancel?.Invoke(this, args);
            ChangeVisualState();
        }

        protected internal void OnKeyPressed(string key)
        {
            KeyPressed?.Invoke(this, new KeyEventArgs(key));
        }

        protected internal void OnKeyPressed(string key, KeyboardModifiers modifiers)
        {
            KeyPressed?.Invoke(this, new KeyEventArgs(key, modifiers));
        }

        protected internal void OnKeyPressed(char key)
        {
            KeyPressed?.Invoke(this, new KeyEventArgs(key));
        }

        protected internal void OnKeyPressed(char key, KeyboardModifiers modifiers)
        {
            KeyPressed?.Invoke(this, new KeyEventArgs(key, modifiers));
        }

        protected internal void OnKeyPressed(KeyboardCommand key)
        {
            KeyPressed?.Invoke(this, new KeyEventArgs(key));
        }

        protected internal void OnKeyPressed(KeyboardCommand key, KeyboardModifiers modifiers)
        {
            KeyPressed?.Invoke(this, new KeyEventArgs(key, modifiers));
        }

        #endregion

        protected internal virtual void OnScrollWheelChanged(ScrollWheelChangedEventArgs args)
        {
            ScrollWheelChanged?.Invoke(this, args);
        }

        public void ResetInputs()
        {
            TouchState = TouchStates.Released;
            MouseButtonStates[MouseButton.Left] = ButtonState.Released;
            MouseButtonStates[MouseButton.Middle] = ButtonState.Released;
            MouseButtonStates[MouseButton.Right] = ButtonState.Released;
            HoverState = HoverStates.NotHovering;
            ClearToolTipState();
            ChangeVisualState();
        }

        internal void UpdateTimers()
        {
            _longPressTimer.Update();
            _toolTipHoverTimer.Update();
            _toolTipLongPressTimer.Update();
            UpdateAnimations();
        }

        internal void StartAnimation(ControlAnimation animation)
        {
            for (var i = _animations.Count - 1; i >= 0; i--)
            {
                if (_animations[i].RemoveConflictingTweens(animation))
                    _animations.RemoveAt(i);
            }

            _animations.Add(animation);
        }

        internal void RemoveAnimation(ControlAnimation animation)
        {
            _animations.Remove(animation);
        }

        internal void SetRenderState(float opacity, Vector2 scale)
        {
            RenderOpacity = opacity;
            RenderScale = scale;
        }

        protected internal virtual void OnGotFocus(GotFocusEventArgs args)
        {
            GotFocus?.Invoke(this, args);
        }

        protected internal virtual void OnLostFocus(LostFocusEventArgs args)
        {
            LostFocus?.Invoke(this, args);
        }

        protected virtual void OnKeyDown(KeyEventArgs args)
        {
            KeyDown?.Invoke(this, args);
        }

        protected virtual void OnKeyUp(KeyEventArgs args)
        {
            KeyUp?.Invoke(this, args);
        }

        public void Focus()
        {
            if (!IsEnabled || !IsVisible || IsGone)
                return;
            ScreenEngine.FocusedControl = this;
        }

        private static bool IsDoubleClick(ref TimeSpan? lastClickAt)
        {
            var now = ScreenSystem.TotalTime;
            var threshold = ScreenEngine.Instance?.Options.DoubleClickThreshold ?? TimeSpan.FromMilliseconds(400);
            var isDoubleClick = lastClickAt.HasValue && now - lastClickAt.Value <= threshold;
            lastClickAt = now;
            return isDoubleClick;
        }

        private bool HasToolTip => !string.IsNullOrEmpty(_toolTip);

        private ScreenEngineOptions GetOptions()
        {
            return Screen?.ScreenEngine?.Options ?? ScreenEngine.Instance?.Options ?? DefaultOptions;
        }

        private void StartToolTipHover(PointF position)
        {
            _lastToolTipAnchorPosition = position;
            if (!HasToolTip)
                return;

            var options = GetOptions();
            _toolTipHoverTimer.WaitTime = Math.Max(0, (int)options.ToolTipHoverDelay.TotalMilliseconds);
            _toolTipHoverTimer.Start();
        }

        private void StartToolTipLongPress(PointF position)
        {
            _lastToolTipAnchorPosition = position;
            if (!HasToolTip || ContextMenu != null)
                return;

            var options = GetOptions();
            _toolTipLongPressTimer.WaitTime = Math.Max(0, (int)options.ToolTipLongPressDelay.TotalMilliseconds);
            _toolTipLongPressTimer.Start();
        }

        private void ShowToolTip(PointF position)
        {
            if (!HasToolTip || !IsEnabled || !IsVisible || IsGone)
                return;

            Screen?.ShowToolTip(this, _toolTip!, position);
        }

        private void ClearToolTipState()
        {
            _toolTipHoverTimer.Stop();
            _toolTipLongPressTimer.Stop();
            Screen?.ClearToolTip(this);
        }

        private static void DrawBorder(SpriteBatch spriteBatch, Rect rect, float width, Brush brush, float opacity)
        {
            DrawBorder(spriteBatch, rect, new Thickness(width), brush, opacity);
        }

        private void DrawFocusVisual(SpriteBatch spriteBatch, Rect rect, float width, Brush brush, FocusVisualKind kind, float opacity)
        {
            // Rounded controls get a focus ring that follows their corner radius.
            var radius = CornerRadius;
            if (!radius.IsEmpty && brush is SolidColorBrush solidBrush && kind != FocusVisualKind.Dotted)
            {
                var color = Media.Brush.ApplyOpacity(solidBrush.Color, opacity);
                switch (kind)
                {
                    case FocusVisualKind.Glow:
                        var glowRadius = new CornerRadius(
                            radius.TopLeft + width, radius.TopRight + width,
                            radius.BottomRight + width, radius.BottomLeft + width);
                        RoundedRectRenderer.DrawBorder(spriteBatch, rect + new Thickness(width), glowRadius, new Thickness(width * 3), Media.Brush.ApplyOpacity(solidBrush.Color, opacity * 0.28f));
                        RoundedRectRenderer.DrawBorder(spriteBatch, rect, radius, new Thickness(width), color);
                        break;
                    case FocusVisualKind.Thick:
                        RoundedRectRenderer.DrawBorder(spriteBatch, rect, radius, new Thickness(width * 2), color);
                        break;
                    default:
                        RoundedRectRenderer.DrawBorder(spriteBatch, rect, radius, new Thickness(width), color);
                        break;
                }

                return;
            }

            switch (kind)
            {
                case FocusVisualKind.Dotted:
                    DrawDottedBorder(spriteBatch, rect, width, brush, opacity);
                    break;
                case FocusVisualKind.Glow:
                    DrawBorder(spriteBatch, rect + new Thickness(width), width * 3, brush, opacity * 0.28f);
                    DrawBorder(spriteBatch, rect, width, brush, opacity);
                    break;
                case FocusVisualKind.Thick:
                    DrawBorder(spriteBatch, rect, width * 2, brush, opacity);
                    break;
                default:
                    DrawBorder(spriteBatch, rect, width, brush, opacity);
                    break;
            }
        }

        private static void DrawBorder(SpriteBatch spriteBatch, Rect rect, Thickness width, Brush brush, float opacity)
        {
            BorderRenderer.Draw(spriteBatch, rect, width, brush, opacity);
        }

        private static bool HasBorder(Thickness thickness)
        {
            return thickness.Left > 0 || thickness.Top > 0 || thickness.Right > 0 || thickness.Bottom > 0;
        }

        private static void DrawDottedBorder(SpriteBatch spriteBatch, Rect rect, float width, Brush brush, float opacity)
        {
            var dash = Math.Max(1, width * 2);
            DrawDottedLine(spriteBatch, new Rect(rect.Left, rect.Top, rect.Width, width), dash, true, brush, opacity);
            DrawDottedLine(spriteBatch, new Rect(rect.Left, rect.Bottom - width, rect.Width, width), dash, true, brush, opacity);
            DrawDottedLine(spriteBatch, new Rect(rect.Left, rect.Top, width, rect.Height), dash, false, brush, opacity);
            DrawDottedLine(spriteBatch, new Rect(rect.Right - width, rect.Top, width, rect.Height), dash, false, brush, opacity);
        }

        private static void DrawDottedLine(SpriteBatch spriteBatch, Rect line, float dash, bool horizontal, Brush brush, float opacity)
        {
            var length = horizontal ? line.Width : line.Height;
            for (var offset = 0f; offset < length; offset += dash * 2)
            {
                var segmentLength = Math.Min(dash, length - offset);
                var segment = horizontal
                    ? new Rect(line.Left + offset, line.Top, segmentLength, line.Height)
                    : new Rect(line.Left, line.Top + offset, line.Width, segmentLength);
                brush.Draw(spriteBatch, segment, opacity);
            }
        }

        private void UpdateAnimations()
        {
            for (var i = _animations.Count - 1; i >= 0; i--)
            {
                var animation = _animations[i];
                if (animation.Update())
                    _animations.Remove(animation);
            }
        }
    }
}
