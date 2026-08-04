using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Effects;
using MonoGame.PortableUI.Input;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI
{
    public abstract class Screen : FrameworkElement
    {

        protected Dictionary<MouseButton, ButtonState> MouseButtonStates { get; } = new Dictionary<MouseButton, ButtonState>
        {
            {MouseButton.Left, ButtonState.Released},
            {MouseButton.Middle, ButtonState.Released},
            {MouseButton.Right, ButtonState.Released},
        };

        public override FrameworkElement? Parent
        {
            get { return null; }
            internal set { }
        }

        private readonly Grid _mainGrid;

        internal PointF LastMousePosition;
        internal PointF LastTouchPosition;
        internal int LastScrollWheelValue;
        private FlyOut? _flyOut;
        private FlyOut? _dismissingFlyOut;
        private ToolTipPopup? _toolTip;
        private ToolTipPopup? _dismissingToolTip;
        private readonly List<Control> _visualTreeScratch = new List<Control>();
        private readonly List<MouseButton> _pressedMouseButtonsScratch = new List<MouseButton>(3);
        private Control? _toolTipOwner;
        private string? _toolTipText;
        private PointF _toolTipAnchorPosition;
        private ContextMenu? _activeContextMenu;
        private ContextMenu? _dismissingContextMenu;
        private FlyOutAnimationStyle _activeFlyOutAnimationStyle = FlyOutAnimationStyle.Popup;
        private Control? _capturedMouseControl;
        private Keys[] _lastPressedKeys = Array.Empty<Keys>();
        private bool _inIslandPostFx;
        private long _appliedThemeVersion = -1;
        // Rebuilt every Draw; Update reads the previous frame's entries for pointer inverse mapping.
        private readonly List<(Rect Rect, float Distortion)> _distortedIslands = new List<(Rect, float)>();
        private static readonly ScreenEngineOptions DefaultOptions = new ScreenEngineOptions();
        private static readonly RasterizerState ScissorRasterizer = new RasterizerState { ScissorTestEnable = true };

        protected Screen()
        {
            _mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(),
                    new RowDefinition {Height = GridLength.Auto}
                }
            };
            _mainGrid.Parent = this;
        }

        public bool Initialized { get; set; }

        public Rect ScreenRect => ScreenEngine?.ScreenRect ?? Rect.Empty;

        protected internal ScreenEngine? ScreenEngine { get; set; }

        public Control? Content
        {
            get { return _mainGrid.Children.Count > 0 ? _mainGrid.Children[0] : null; }
            set
            {
                if (value == null)
                {
                    _mainGrid.Children.Clear();
                    InvalidateLayout(true);
                    return;
                }

                if (_mainGrid.Children.Count == 0)
                    _mainGrid.AddChild(value);
                else
                    _mainGrid.Children[0] = value;
                InvalidateLayout(true);
            }
        }

        private FlyOut? FlyOut
        {
            get { return _flyOut; }
            set
            {
                if (_flyOut != null)
                {
                    RemoveFlyOutNow(_flyOut, _activeContextMenu);
                    _activeContextMenu = null;
                    _activeFlyOutAnimationStyle = FlyOutAnimationStyle.Popup;
                }
                if (_dismissingFlyOut != null)
                {
                    RemoveFlyOutNow(_dismissingFlyOut, _dismissingContextMenu);
                    _dismissingFlyOut = null;
                    _dismissingContextMenu = null;
                }
                _flyOut = value;
                if (_flyOut != null)
                {
                    _flyOut.NotifyShowing();
                    _flyOut.Parent = this;
                    _flyOut.NotifyShown();
                }
            }
        }


        public override void InvalidateLayout(bool boundsChanged)
        {
            ScreenEngine?.RecordLayoutPass();
            _mainGrid?.UpdateLayout(ScreenRect);
        }

        public override IEnumerable<Control> GetDescendants()
        {
            yield return _mainGrid;
        }

        internal void Draw(SpriteBatch spriteBatch)
        {
            var device = spriteBatch.GraphicsDevice;
            var engine = ScreenEngine;
            var theme = engine?.Options.Theme;

            _distortedIslands.Clear();
            PrepareBackdrop(spriteBatch, engine);

            var postEffects = theme?.PostEffects;
            // Island post-FX switches render targets mid-frame; the backbuffer discards its
            // contents on re-bind, so when FX islands exist the whole UI must render into a
            // PreserveContents target even without screen-level effects.
            var usePostFx = engine != null
                && ScreenRect.Width > 0 && ScreenRect.Height > 0
                && (postEffects is { Count: > 0 } && engine.PostProcess.CountEnabled(postEffects) > 0
                    || TreeHasPostFxIslands());
            RenderTargetBinding[]? previousTargets = null;
            RenderTarget2D? uiTarget = null;
            if (usePostFx)
            {
                previousTargets = device.GetRenderTargets();
                uiTarget = engine!.PostProcess.EnsureUiTarget((int)Math.Ceiling(ScreenRect.Width), (int)Math.Ceiling(ScreenRect.Height));
                device.SetRenderTarget(uiTarget);
                device.Clear(Color.Transparent);
            }

            OnBeforeDraw(spriteBatch);

            if (BackgroundBrush != null)
            {
                spriteBatch.Begin();
                BackgroundBrush.Draw(spriteBatch, ScreenRect);
                spriteBatch.End();
            }

            DrawControlTree(spriteBatch, _mainGrid, _mainGrid.BoundingRect);

            if (FlyOut != null)
                DrawControlTree(spriteBatch, FlyOut, GetOverlayScissor(FlyOut));
            if (_dismissingFlyOut != null)
                DrawControlTree(spriteBatch, _dismissingFlyOut, GetOverlayScissor(_dismissingFlyOut));

            if (_toolTip != null)
                DrawControlTree(spriteBatch, _toolTip, GetOverlayScissor(_toolTip));
            if (_dismissingToolTip != null)
                DrawControlTree(spriteBatch, _dismissingToolTip, GetOverlayScissor(_dismissingToolTip));

            if (_activeDrag?.DragVisual is { } dragGhost)
                DrawControlTree(spriteBatch, dragGhost, GetOverlayScissor(dragGhost));

            DrawDebugOverlay(spriteBatch);

            if (usePostFx)
            {
                if (previousTargets == null || previousTargets.Length == 0)
                    device.SetRenderTarget(null);
                else
                    device.SetRenderTargets(previousTargets);
                engine!.PostProcess.Compose(spriteBatch, uiTarget!, postEffects ?? Array.Empty<PostEffect>(), ScreenRect, engine.Backdrop);
                engine.RecordBatchFlush();
            }

            BackdropSource.Clear(device);
        }

        /// <summary>
        /// Optional external scene (e.g. the host game's frame) used as the backdrop that
        /// glass brushes blur and sample. Drawn beneath <see cref="FrameworkElement.BackgroundBrush"/>,
        /// which stays optional when an external backdrop is present. Set every frame by the
        /// host; the texture is only read during <see cref="Draw"/>.
        /// </summary>
        public Texture2D? ExternalBackdrop { get; set; }

        private void PrepareBackdrop(SpriteBatch spriteBatch, ScreenEngine? engine)
        {
            var device = spriteBatch.GraphicsDevice;
            BackdropSource.Clear(device);
            var external = ExternalBackdrop;
            if (external != null && external.IsDisposed)
                external = null;
            if (engine == null || (BackgroundBrush == null && external == null) || ScreenRect.Width <= 0 || ScreenRect.Height <= 0 || !TreeRequiresBackdrop())
                return;

            var backdrop = engine.Backdrop;
            backdrop.BeginFrame();
            var previousTargets = device.GetRenderTargets();
            var scene = backdrop.EnsureSceneTarget((int)Math.Ceiling(ScreenRect.Width), (int)Math.Ceiling(ScreenRect.Height));
            device.SetRenderTarget(scene);
            device.Clear(Color.Transparent);
            spriteBatch.Begin();
            if (external != null)
                spriteBatch.Draw(external, new Rectangle(0, 0, scene.Width, scene.Height), Color.White);
            BackgroundBrush?.Draw(spriteBatch, ScreenRect);
            spriteBatch.End();
            var blurred = backdrop.Blur(spriteBatch, scene);
            if (previousTargets.Length == 0)
                device.SetRenderTarget(null);
            else
                device.SetRenderTargets(previousTargets);
            BackdropSource.Set(device, blurred, ScreenRect);
            engine.RecordBatchFlush();
        }

        private bool TreeHasPostFxIslands()
        {
            var engine = ScreenEngine;
            if (engine == null)
                return false;

            _visualTreeScratch.Clear();
            VisualTreeHelper.AppendVisualTree(_mainGrid, _visualTreeScratch, false);
            if (_flyOut != null)
                VisualTreeHelper.AppendVisualTree(_flyOut, _visualTreeScratch, false);

            var found = false;
            foreach (var control in _visualTreeScratch)
            {
                if (control is ThemeIsland { IsVisible: true, Theme.PostEffects: { Count: > 0 } effects }
                    && engine.PostProcess.CountEnabled(effects) > 0)
                {
                    found = true;
                    break;
                }
            }

            _visualTreeScratch.Clear();
            return found;
        }

        private bool TreeRequiresBackdrop()
        {
            // The screen's own background brush is drawn directly (not part of _mainGrid), so it
            // must be checked explicitly — otherwise a full-screen glass background never triggers
            // the backdrop-blur pipeline.
            if (BackgroundBrush is { RequiresBackdrop: true })
                return true;

            _visualTreeScratch.Clear();
            VisualTreeHelper.AppendVisualTree(_mainGrid, _visualTreeScratch, false);
            if (_flyOut != null)
                VisualTreeHelper.AppendVisualTree(_flyOut, _visualTreeScratch, false);

            var requiresBackdrop = false;
            foreach (var control in _visualTreeScratch)
            {
                if (control.IsVisible && control.BackgroundBrush is { RequiresBackdrop: true })
                {
                    requiresBackdrop = true;
                    break;
                }
            }

            _visualTreeScratch.Clear();
            return requiresBackdrop;
        }

        protected internal virtual void OnBeforeDraw(SpriteBatch spriteBatch)
        {
        }

        public IInputSource InputSource { get; set; } = DeviceInputSource.Instance;

        internal void OnNavigationFrom(object? sender)
        {
            CancelDrag();
            _visualTreeScratch.Clear();
            VisualTreeHelper.AppendVisualTree(_mainGrid, _visualTreeScratch);
            foreach (var control in _visualTreeScratch)
            {
                control.ResetInputs();
            }
            _visualTreeScratch.Clear();
            _capturedMouseControl = null;
        }
        
        internal void CreateContextMenu(PointF position, ContextMenu content, bool optimizeForTouch, Control? owner = null)
        {
            ClearToolTip();
            content.OnOpening();
            FlyOut = new FlyOut(position, content.ContextMenuType == ContextMenuTypes.OpenAndHold)
            {
                Content = content.CreateControl(this, optimizeForTouch),
                ThemeOwner = owner
            };
            _activeFlyOutAnimationStyle = FlyOutAnimationStyle.Popup;
            _activeContextMenu = content;
            FlyOut.UpdateLayout(ScreenRect);
            AnimateFlyOutIn(FlyOut, _activeFlyOutAnimationStyle);
            content.OnOpened();
        }

        internal void ShowFlyOut(PointF position, Control content, bool removeOnRelease, Control? owner = null)
        {
            ClearToolTip();
            FlyOut = new FlyOut(position, removeOnRelease)
            {
                Content = content,
                ThemeOwner = owner
            };
            _activeFlyOutAnimationStyle = FlyOutAnimationStyle.DropDown;
            FlyOut.UpdateLayout(ScreenRect);
            AnimateFlyOutIn(FlyOut, _activeFlyOutAnimationStyle);
        }

        internal void ShowToolTip(Control owner, string text, PointF anchorPosition)
        {
            if (string.IsNullOrEmpty(text) || !owner.IsEnabled || !owner.IsVisible || owner.IsGone)
                return;

            if (_toolTipOwner != owner || _toolTipText != text)
                ClearToolTip();

            var created = _toolTip == null;
            if (created)
            {
                _dismissingToolTip = null;
                _toolTip = new ToolTipPopup(text);
                _toolTip.ThemeOwner = owner;
                _toolTip.Parent = this;
                _toolTipOwner = owner;
                _toolTipText = text;
            }

            _toolTipAnchorPosition = anchorPosition;
            UpdateToolTipLayout();
            if (created)
                AnimatePopupIn(_toolTip!, 0.98, new Vector2(0, 4), TimeSpan.FromMilliseconds(100));
        }

        internal void UpdateToolTip(Control owner, PointF anchorPosition)
        {
            if (_toolTipOwner != owner || _toolTip == null)
                return;

            _toolTipAnchorPosition = anchorPosition;
            UpdateToolTipLayout();
        }

        internal void ClearToolTip(Control? owner = null)
        {
            if (owner != null && _toolTipOwner != owner)
                return;

            if (_toolTip != null)
            {
                var toolTip = _toolTip;
                toolTip.Parent = null;
                _dismissingToolTip = toolTip;
                AnimatePopupOut(toolTip, 0.98, new Vector2(0, 4), TimeSpan.FromMilliseconds(80), () =>
                {
                    if (_dismissingToolTip == toolTip)
                        _dismissingToolTip = null;
                });
            }
            _toolTip = null;
            _toolTipOwner = null;
            _toolTipText = null;
        }

        internal bool IsToolTipVisibleFor(Control owner)
        {
            return _toolTipOwner == owner && _toolTip != null;
        }

        internal Rect ToolTipRect => _toolTip?.BoundingRect ?? Rect.Empty;

        private void UpdateToolTipLayout()
        {
            if (_toolTip == null)
                return;

            var options = ScreenEngine?.Options ?? DefaultOptions;
            var size = _toolTip.MeasureLayout();
            var position = _toolTipAnchorPosition + options.ToolTipOffset;
            var preferredRect = new Rect(position, size);
            var layoutRect = ClampPopupRect(preferredRect, ScreenRect, options.ToolTipScreenPadding);
            _toolTip.UpdateLayout(layoutRect);
        }

        private void DrawControlTree(SpriteBatch spriteBatch, Control control, Rect scissorRect)
        {
            if (scissorRect.Width <= 0 || scissorRect.Height <= 0)
                scissorRect = GetOverlayScissor(control);

            spriteBatch.GraphicsDevice.ScissorRectangle = ToScissorRectangle(scissorRect);
            DrawControlBatched(spriteBatch, control, RenderContext.Root(scissorRect));
        }

        private void DrawDebugOverlay(SpriteBatch spriteBatch)
        {
            var engine = ScreenEngine;
            if (engine == null || !engine.DebugOverlayEnabled || FontManager.DefaultFont == null)
                return;

            var text = $"FPS {engine.FramesPerSecond:0}  Batches {engine.BatchFlushesThisFrame}  Layout {engine.LayoutPassesThisFrame}";
            var size = FontManager.DefaultFont.MeasureString(text);
            var x = Math.Max(0, ScreenRect.Right - size.X - 8);
            var y = Math.Max(0, ScreenRect.Top + 8);

            spriteBatch.Begin();
            spriteBatch.DrawString(FontManager.DefaultFont, text, new Vector2(x, y), Color.White);
            spriteBatch.End();
            engine.RecordBatchFlush();
        }

        private Rect GetOverlayScissor(Control control)
        {
            if (ScreenRect.Width > 0 && ScreenRect.Height > 0)
                return ScreenRect;

            if (control.ClippingRect.Width > 0 && control.ClippingRect.Height > 0)
                return control.ClippingRect;

            return control.BoundingRect;
        }

        private void DrawControlBatched(SpriteBatch spriteBatch, Control control, RenderContext parentContext)
        {
            if (!control.IsVisible || control.IsGone)
                return;

            var context = parentContext.ForControl(control);
            if (context.ScissorRect.Width <= 0 || context.ScissorRect.Height <= 0)
                return;

            if (control is ThemeIsland island && TryComposeIslandPostFx(spriteBatch, island, parentContext, context))
                return;

            var oldRect = new Rect(spriteBatch.GraphicsDevice.ScissorRectangle);
            control.SetRenderState(context.Opacity, context.Scale);
            spriteBatch.GraphicsDevice.ScissorRectangle = ToScissorRectangle(context.ScissorRect);
            spriteBatch.Begin(SpriteSortMode.Deferred, rasterizerState: ScissorRasterizer, effect: ScreenEngine?.Options.Effect);
            control.OnDraw(spriteBatch, context.RenderRect);
            spriteBatch.End();
            ScreenEngine?.RecordBatchFlush();

            foreach (var c in control.GetDescendants())
            {
                DrawControlBatched(spriteBatch, c, context);
            }

            spriteBatch.GraphicsDevice.ScissorRectangle = ToScissorRectangle(context.ScissorRect);
            spriteBatch.Begin(SpriteSortMode.Deferred, rasterizerState: ScissorRasterizer, effect: ScreenEngine?.Options.Effect);
            control.OnDrawOverlay(spriteBatch, context.RenderRect);
            spriteBatch.End();
            ScreenEngine?.RecordBatchFlush();
            spriteBatch.GraphicsDevice.ScissorRectangle = oldRect;
        }

        /// <summary>
        ///     A ThemeIsland whose theme carries enabled post effects renders its subtree into an
        ///     offscreen target and composes the chain (barrel, scanlines, bloom, ...) into the
        ///     island's own rect — e.g. a themed CRT monitor inside another screen. The outermost
        ///     island wins; nested effect islands render flat inside it.
        /// </summary>
        private bool TryComposeIslandPostFx(SpriteBatch spriteBatch, ThemeIsland island, RenderContext parentContext, RenderContext context)
        {
            if (_inIslandPostFx)
                return false;

            var engine = ScreenEngine;
            var effects = island.Theme?.PostEffects;
            if (engine == null || effects is not { Count: > 0 } || engine.PostProcess.CountEnabled(effects) == 0)
                return false;

            var islandRect = context.RenderRect;
            if (islandRect.Width <= 0 || islandRect.Height <= 0)
                return false;

            var device = spriteBatch.GraphicsDevice;
            var previousTargets = device.GetRenderTargets();
            // Full-frame target so the subtree can keep drawing at absolute screen coordinates.
            var targetWidth = (int)Math.Ceiling(Math.Max(ScreenRect.Right, islandRect.Right));
            var targetHeight = (int)Math.Ceiling(Math.Max(ScreenRect.Bottom, islandRect.Bottom));
            var target = engine.PostProcess.EnsureIslandTarget(targetWidth, targetHeight);
            device.SetRenderTarget(target);
            device.Clear(Color.Transparent);

            _inIslandPostFx = true;
            try
            {
                DrawControlBatched(spriteBatch, island, parentContext);
            }
            finally
            {
                _inIslandPostFx = false;
            }

            if (previousTargets.Length == 0)
                device.SetRenderTarget(null);
            else
                device.SetRenderTargets(previousTargets);

            engine.PostProcess.Compose(spriteBatch, target, effects, islandRect, engine.Backdrop, islandRect);
            engine.RecordBatchFlush();

            var barrel = FindEnabledBarrel(effects);
            if (barrel != null)
                _distortedIslands.Add((islandRect, MathHelper.Clamp(barrel.Distortion, 0, 0.5f)));
            return true;
        }

        private static CrtBarrelPostEffect? FindEnabledBarrel(IReadOnlyList<PostEffect> effects)
        {
            foreach (var effect in effects)
            {
                if (effect is CrtBarrelPostEffect { Enabled: true } barrel)
                    return barrel;
            }

            return null;
        }

        private static Rectangle ToScissorRectangle(Rect rect)
        {
            var left = (int)Math.Floor(rect.Left);
            var top = (int)Math.Floor(rect.Top);
            var right = (int)Math.Ceiling(rect.Right);
            var bottom = (int)Math.Ceiling(rect.Bottom);
            return new Rectangle(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        }

        private readonly struct RenderContext
        {
            private readonly Matrix _transform;

            private RenderContext(Matrix transform, Vector2 scale, float opacity, Rect scissorRect, Rect childClipRect, Rect renderRect)
            {
                _transform = transform;
                Scale = scale;
                Opacity = opacity;
                ScissorRect = scissorRect;
                ChildClipRect = childClipRect;
                RenderRect = renderRect;
            }

            public Vector2 Scale { get; }
            public float Opacity { get; }
            public Rect ScissorRect { get; }
            public Rect ChildClipRect { get; }
            public Rect RenderRect { get; }

            public static RenderContext Root(Rect scissorRect)
            {
                return new RenderContext(Matrix.Identity, Vector2.One, 1, scissorRect, scissorRect, scissorRect);
            }

            public RenderContext ForControl(Control control)
            {
                var transform = CreateControlTransform(control) * _transform;
                var renderRect = TransformRect(control.ClippingRect, transform);
                // Drop shadows render outside the control's bounds; widen the scissor so they survive.
                var scissorSource = control.Shadow is { Inset: false } shadow
                    ? renderRect + new Thickness(shadow.Blur + shadow.Spread + Math.Max(Math.Abs(shadow.Offset.X), Math.Abs(shadow.Offset.Y)))
                    : renderRect;
                var scissorRect = ChildClipRect ^ scissorSource;
                // Only controls that clip their content (e.g. ScrollViewer) shrink the clip for
                // descendants; everything else inherits it so overflowing shadows survive.
                var childClipRect = control.ClipsDescendants ? ChildClipRect ^ renderRect : ChildClipRect;
                var scale = new Vector2(Scale.X * control.Scale.X, Scale.Y * control.Scale.Y);
                var opacity = Opacity * MathHelper.Clamp((float)control.Opacity, 0, 1);
                return new RenderContext(transform, scale, opacity, scissorRect, childClipRect, renderRect);
            }

            private static Matrix CreateControlTransform(Control control)
            {
                if (control.Scale == Vector2.One && control.Translation == Vector2.Zero)
                    return Matrix.Identity;

                var rect = control.ClippingRect;
                var origin = new Vector2(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
                return Matrix.CreateTranslation(-origin.X, -origin.Y, 0)
                    * Matrix.CreateScale(control.Scale.X, control.Scale.Y, 1)
                    * Matrix.CreateTranslation(origin.X + control.Translation.X, origin.Y + control.Translation.Y, 0);
            }

            private static Rect TransformRect(Rect rect, Matrix transform)
            {
                var topLeft = Vector2.Transform(new Vector2(rect.Left, rect.Top), transform);
                var topRight = Vector2.Transform(new Vector2(rect.Right, rect.Top), transform);
                var bottomLeft = Vector2.Transform(new Vector2(rect.Left, rect.Bottom), transform);
                var bottomRight = Vector2.Transform(new Vector2(rect.Right, rect.Bottom), transform);

                var left = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
                var top = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
                var right = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
                var bottom = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));
                return new Rect(left, top, right - left, bottom - top);
            }
        }

        internal void Update()
        {
            if (!Initialized)
            {
                InvalidateLayout(true);
                Initialized = true;
            }

            var inputSource = InputSource ?? DeviceInputSource.Instance;
            // All downstream consumers work in UI space: undo the CRT barrel displacement here.
            var mousePosition = TransformPointerPosition(inputSource.MousePosition);
            var pressedMouseButtons = SnapshotPressedMouseButtons(inputSource.PressedMouseButtons);
            TouchLocation touchState = default(TouchLocation);
            var touchCollection = inputSource.Touches;
            var hasTouch = touchCollection.Count > 0;
            if (hasTouch)
            {
                touchState = touchCollection[0];
            }
            var touchPosition = hasTouch ? TransformPointerPosition((PointF)touchState.Position.ToPoint()) : LastTouchPosition;

            Control content;

            if (FlyOut != null)
                content = FlyOut;
            else
                content = _mainGrid;

            var themeVersion = ThemeVersion.Current;
            if (_appliedThemeVersion != themeVersion)
            {
                _appliedThemeVersion = themeVersion;
                RefreshThemeForTree(_mainGrid);
                if (_flyOut != null)
                    RefreshThemeForTree(_flyOut);
                if (_toolTip != null)
                    RefreshThemeForTree(_toolTip);
                InvalidateLayout(true);
            }

            UpdateTimersForTree(content);
            if (_toolTip != null)
                UpdateTimersForTree(_toolTip);
            if (_dismissingFlyOut != null)
                UpdateTimersForTree(_dismissingFlyOut);
            if (_dismissingToolTip != null)
                UpdateTimersForTree(_dismissingToolTip);

            HandleKeyboardInput();

            if (_activeDrag is { } drag)
            {
                var dragPosition = drag.IsTouchDrag ? touchPosition : mousePosition;
                var pointerReleased = drag.IsTouchDrag
                    ? !hasTouch || touchState.State == TouchLocationState.Released
                    : GetButtonState(pressedMouseButtons, MouseButton.Left) == ButtonState.Released;

                if (Keyboard.GetState().IsKeyDown(Keys.Escape)
                    || (!drag.IsTouchDrag && GetButtonState(pressedMouseButtons, MouseButton.Right) == ButtonState.Pressed))
                    CancelDrag();
                else if (pointerReleased)
                    CompleteDrag(drag, dragPosition);
                else
                    UpdateDrag(drag, dragPosition, content);

                // Keep bookkeeping in sync while normal routing is suspended.
                MouseButtonStates[MouseButton.Left] = GetButtonState(pressedMouseButtons, MouseButton.Left);
                MouseButtonStates[MouseButton.Right] = GetButtonState(pressedMouseButtons, MouseButton.Right);
                MouseButtonStates[MouseButton.Middle] = GetButtonState(pressedMouseButtons, MouseButton.Middle);
                LastMousePosition = mousePosition;
                if (hasTouch)
                    LastTouchPosition = touchPosition;
                LastScrollWheelValue = inputSource.ScrollWheelValue;
                return;
            }

            if (mousePosition != LastMousePosition)
            {
                if (!RouteCapturedMouseMove(mousePosition, pressedMouseButtons))
                {
                    var args = new MouseEventArgs(mousePosition, pressedMouseButtons);
                    VisualTreeHelper.IterateVisualTree(content, args,
                        (c, a) => c.BoundingRect.Contains(a.Position) && !c.BoundingRect.Contains(LastMousePosition), (c, a) => { c.OnMouseEnter(a); }, (c, a) => c.BoundingRect.Contains(a.Position));
                    VisualTreeHelper.IterateVisualTree(content, args, (c, a) => c.BoundingRect.Contains(a.Position) && c.BoundingRect.Contains(LastMousePosition), (c, a) => { c.OnMouseMove(a); }, null);
                    VisualTreeHelper.IterateVisualTree(content, args, (c, a) => !c.BoundingRect.Contains(a.Position) && c.BoundingRect.Contains(LastMousePosition), (c, a) => { c.OnMouseLeave(a); }, (c, a) => c.BoundingRect.Contains(LastMousePosition));
                }
                LastMousePosition = mousePosition;
            }

            HandleMouseButton(GetButtonState(pressedMouseButtons, MouseButton.Left), ButtonState.Pressed, MouseButton.Left, mousePosition, content, (c, a) => c.OnMouseDown(a));
            HandleMouseButton(GetButtonState(pressedMouseButtons, MouseButton.Left), ButtonState.Released, MouseButton.Left, mousePosition, content, (c, a) => c.OnMouseUp(a));
            HandleMouseButton(GetButtonState(pressedMouseButtons, MouseButton.Right), ButtonState.Pressed, MouseButton.Right, mousePosition, content, (c, a) => c.OnMouseDown(a));
            HandleMouseButton(GetButtonState(pressedMouseButtons, MouseButton.Right), ButtonState.Released, MouseButton.Right, mousePosition, content, (c, a) => c.OnMouseUp(a));
            HandleMouseButton(GetButtonState(pressedMouseButtons, MouseButton.Middle), ButtonState.Pressed, MouseButton.Middle, mousePosition, content, (c, a) => c.OnMouseDown(a));
            HandleMouseButton(GetButtonState(pressedMouseButtons, MouseButton.Middle), ButtonState.Released, MouseButton.Middle, mousePosition, content, (c, a) => c.OnMouseUp(a));
            if (inputSource.ScrollWheelValue != LastScrollWheelValue)
            {
                var args = new ScrollWheelChangedEventArgs(mousePosition, inputSource.ScrollWheelValue - LastScrollWheelValue);

                VisualTreeHelper.IterateVisualTree(content, args, (c, a) => c.BoundingRect.Contains(a.Position), (c, a) => { c.OnScrollWheelChanged(a); }, null);

                LastScrollWheelValue = inputSource.ScrollWheelValue;
            }


            if (hasTouch && touchState.State == TouchLocationState.Pressed)
            {
                var args = new TouchEventArgs(touchPosition);
                VisualTreeHelper.IterateVisualTree(content, args,
                    (c, a) => c.BoundingRect.Contains(a.Position),
                    (c, a) => { c.OnTouchDown(a); },
                    null
                    );
                LastTouchPosition = touchPosition;
            }
            if (hasTouch && touchState.State == TouchLocationState.Released)
            {
                var args = new TouchEventArgs(touchPosition);
                VisualTreeHelper.IterateVisualTree(content, args,
                    (c, a) => c.BoundingRect.Contains(a.Position),
                    (c, a) => { c.OnTouchUp(a); },
                    null
                    );
            }
            if (hasTouch && touchState.State == TouchLocationState.Moved && touchPosition != LastTouchPosition)
            {
                var args = new TouchEventArgs(touchPosition);
                VisualTreeHelper.IterateVisualTree(content, args,
                    (c, a) => c.BoundingRect.Contains(a.Position) || c.BoundingRect.Contains(LastTouchPosition),
                    (c, a) =>
                    {
                        if (c.BoundingRect.Contains(a.Position))
                            c.OnTouchMove(a);
                        else
                            c.OnTouchCancel(a);
                    },
                    null
                    );
                LastTouchPosition = touchPosition;
            }
        }

        /// <summary>
        ///     Maps a raw pointer position to UI space: first through the screen-level barrel (if
        ///     the active theme distorts the whole screen), then through the innermost distorted
        ///     ThemeIsland containing the point.
        /// </summary>
        private PointF TransformPointerPosition(PointF position)
        {
            var screenDistortion = GetActiveBarrelDistortion();
            if (screenDistortion > 0 && ScreenRect.Width > 0 && ScreenRect.Height > 0)
                position = PostProcessManager.InverseBarrel(position, ScreenRect, screenDistortion);

            if (_distortedIslands.Count > 0)
            {
                var bestArea = float.MaxValue;
                var bestIndex = -1;
                for (var i = 0; i < _distortedIslands.Count; i++)
                {
                    var (rect, distortion) = _distortedIslands[i];
                    if (distortion <= 0 || !rect.Contains(position))
                        continue;

                    var area = rect.Width * rect.Height;
                    if (area < bestArea)
                    {
                        bestArea = area;
                        bestIndex = i;
                    }
                }

                if (bestIndex >= 0)
                {
                    var (rect, distortion) = _distortedIslands[bestIndex];
                    position = PostProcessManager.InverseBarrel(position, rect, distortion);
                }
            }

            return position;
        }

        private float GetActiveBarrelDistortion()
        {
            var effects = ScreenEngine?.Options.Theme?.PostEffects;
            if (effects == null)
                return 0;

            var barrel = FindEnabledBarrel(effects);
            return barrel == null ? 0 : MathHelper.Clamp(barrel.Distortion, 0, 0.5f);
        }

        private void UpdateTimersForTree(Control control)
        {
            _visualTreeScratch.Clear();
            VisualTreeHelper.AppendVisualTree(control, _visualTreeScratch, false);
            foreach (var visual in _visualTreeScratch)
                visual.UpdateTimers();
            _visualTreeScratch.Clear();
        }

        private void RefreshThemeForTree(Control control)
        {
            _visualTreeScratch.Clear();
            VisualTreeHelper.AppendVisualTree(control, _visualTreeScratch);
            foreach (var visual in _visualTreeScratch)
                visual.RefreshThemeResources();
            _visualTreeScratch.Clear();
        }

        private List<MouseButton> SnapshotPressedMouseButtons(IReadOnlyCollection<MouseButton> pressedMouseButtons)
        {
            _pressedMouseButtonsScratch.Clear();
            foreach (var button in pressedMouseButtons)
                _pressedMouseButtonsScratch.Add(button);
            return _pressedMouseButtonsScratch;
        }

        private static ButtonState GetButtonState(IReadOnlyCollection<MouseButton> pressedMouseButtons, MouseButton button)
        {
            return pressedMouseButtons.Contains(button) ? ButtonState.Pressed : ButtonState.Released;
        }

        private Keys _repeatKey = Keys.None;
        private TimeSpan _nextKeyRepeatTime;
        private static readonly TimeSpan KeyRepeatInitialDelay = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan KeyRepeatInterval = TimeSpan.FromMilliseconds(45);

        private void HandleKeyboardInput()
        {
            var keyboardState = Keyboard.GetState();
            var pressedKeys = keyboardState.GetPressedKeys();
            var modifiers = GetKeyboardModifiers(keyboardState);

            // The debug overlay toggle isn't control-specific, so it must not be gated behind a
            // focused control (otherwise F3 does nothing on a screen with nothing focused).
            if (Array.IndexOf(pressedKeys, Keys.F3) >= 0 && Array.IndexOf(_lastPressedKeys, Keys.F3) < 0)
                ScreenEngine?.ToggleDebugOverlay();

            var focusedControl = ScreenEngine.FocusedControl;

            // Focus is global while several screens can update per frame (UISurfaces): only the
            // screen that owns the focused control may process keys, or every screen would apply
            // the same backspace/arrow once each. Unattached controls keep the legacy routing.
            if (focusedControl == null || (focusedControl.Screen != null && focusedControl.Screen != this))
            {
                _lastPressedKeys = pressedKeys;
                _repeatKey = Keys.None;
                return;
            }

            foreach (var key in pressedKeys)
            {
                if (_lastPressedKeys.Contains(key))
                    continue;

                if (key == Keys.F3)
                    continue; // handled above regardless of focus

                var command = TryGetKeyboardCommand(key, modifiers);
                if (command.HasValue)
                {
                    focusedControl.OnKeyPressed(command.Value, modifiers);
                    // Typematic: one long pause after the first hit, then fast repeats while held.
                    _repeatKey = key;
                    _nextKeyRepeatTime = ScreenSystem.TotalTime + KeyRepeatInitialDelay;
                    continue;
                }

                if ((modifiers & (KeyboardModifiers.Control | KeyboardModifiers.Alt)) != KeyboardModifiers.None)
                    continue;
            }

            if (_repeatKey != Keys.None)
            {
                if (!keyboardState.IsKeyDown(_repeatKey))
                {
                    _repeatKey = Keys.None;
                }
                else if (ScreenSystem.TotalTime >= _nextKeyRepeatTime)
                {
                    var command = TryGetKeyboardCommand(_repeatKey, modifiers);
                    if (command.HasValue)
                        focusedControl.OnKeyPressed(command.Value, modifiers);
                    _nextKeyRepeatTime = ScreenSystem.TotalTime + KeyRepeatInterval;
                }
            }

            _lastPressedKeys = pressedKeys;
        }

        internal void HandleTextInput(char character)
        {
            if (char.IsControl(character))
                return;

            var focusedControl = ScreenEngine.FocusedControl;
            // Same ownership rule as HandleKeyboardInput: prevents double characters when both a
            // host screen and a surface screen receive the same TextInput event.
            if (focusedControl == null || (focusedControl.Screen != null && focusedControl.Screen != this))
                return;

            focusedControl.OnKeyPressed(character, KeyboardModifiers.None);
        }

        private static KeyboardModifiers GetKeyboardModifiers(KeyboardState keyboardState)
        {
            var modifiers = KeyboardModifiers.None;
            if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                modifiers |= KeyboardModifiers.Shift;
            if (keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl))
                modifiers |= KeyboardModifiers.Control;
            if (keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt))
                modifiers |= KeyboardModifiers.Alt;
            return modifiers;
        }

        private static KeyboardCommand? TryGetKeyboardCommand(Keys key, KeyboardModifiers modifiers)
        {
            if ((modifiers & KeyboardModifiers.Control) != 0)
            {
                switch (key)
                {
                    case Keys.A:
                        return KeyboardCommand.SelectAll;
                    case Keys.C:
                        return KeyboardCommand.Copy;
                    case Keys.X:
                        return KeyboardCommand.Cut;
                    case Keys.V:
                        return KeyboardCommand.Paste;
                }
            }

            switch (key)
            {
                case Keys.Back:
                    return KeyboardCommand.Backspace;
                case Keys.Enter:
                    return KeyboardCommand.Enter;
                case Keys.Left:
                    return KeyboardCommand.CursorLeft;
                case Keys.Right:
                    return KeyboardCommand.CursorRight;
                case Keys.Up:
                    return KeyboardCommand.CursorUp;
                case Keys.Down:
                    return KeyboardCommand.CursorDown;
                case Keys.Delete:
                    return KeyboardCommand.Delete;
                case Keys.Home:
                    return KeyboardCommand.Home;
                case Keys.End:
                    return KeyboardCommand.End;
                default:
                    return null;
            }
        }

        private void HandleMouseButton(ButtonState buttonState, ButtonState newState, MouseButton button, PointF position, Control control, Action<Control, MouseEventArgs> action)
        {
            if (buttonState != newState || MouseButtonStates[button] == newState)
                return;
            MouseButtonStates[button] = newState;
            var args = new MouseEventArgs(position, button);
            if (RouteCapturedMouseInput(args, action))
                return;

            VisualTreeHelper.IterateVisualTree(control, args,
                (c, a) => c.BoundingRect.Contains(a.Position),
                action,
                null
            );
        }

        private DragOperation? _activeDrag;

        /// <summary>The running drag &amp; drop operation, if any.</summary>
        public DragOperation? ActiveDrag => _activeDrag;

        internal DragOperation? BeginDrag(Control source, object? payload, DragDropEffects allowedEffects, Control? dragVisual)
        {
            if (_activeDrag != null)
                CancelDrag();

            var operation = new DragOperation(source, payload, allowedEffects, dragVisual)
            {
                IsTouchDrag = MouseButtonStates[MouseButton.Left] != ButtonState.Pressed,
                IsActive = true
            };
            _capturedMouseControl = null;
            if (dragVisual != null)
            {
                dragVisual.Parent = this;
                dragVisual.Opacity = Math.Min(dragVisual.Opacity, 0.75);
            }

            _activeDrag = operation;
            return operation;
        }

        /// <summary>Cancels the running drag operation (also triggered by Esc / right-click / navigation).</summary>
        public void CancelDrag()
        {
            var drag = _activeDrag;
            if (drag == null)
                return;

            if (drag.CurrentTarget is { } target)
                target.OnDragLeave(new DragEventArgs(drag) { Position = LastMousePosition });
            FinishDrag(drag, canceled: true, target: null, LastMousePosition, DragDropEffects.None);
        }

        internal void UpdateDrag(DragOperation drag, PointF position, Control content)
        {
            Control? target = null;
            var probe = new DragEventArgs(drag) { Position = position };
            VisualTreeHelper.IterateVisualTree(content, probe,
                (c, a) => c.AllowDrop && c.BoundingRect.Contains(position),
                (c, a) =>
                {
                    // Descendants are visited before their parents, so the deepest AllowDrop wins.
                    target = c;
                    a.Handled = true;
                },
                (c, a) => c.BoundingRect.Contains(position));

            if (!ReferenceEquals(target, drag.CurrentTarget))
            {
                drag.CurrentTarget?.OnDragLeave(new DragEventArgs(drag) { Position = position });
                drag.CurrentTarget = target;
                drag.LastEffect = DragDropEffects.None;
                if (target != null)
                {
                    var enter = new DragEventArgs(drag) { Position = position };
                    target.OnDragEnter(enter);
                    drag.LastEffect = enter.Effect & drag.AllowedEffects;
                }
            }

            if (drag.CurrentTarget is { } current)
            {
                var over = new DragEventArgs(drag) { Position = position, Effect = drag.LastEffect };
                current.OnDragOver(over);
                drag.LastEffect = over.Effect & drag.AllowedEffects;
            }
            else
            {
                drag.LastEffect = DragDropEffects.None;
            }

            drag.RaiseMoved(new DragEventArgs(drag) { Position = position, Effect = drag.LastEffect });

            if (drag.DragVisual is { } ghost)
            {
                var size = ghost.MeasureLayout();
                ghost.UpdateLayout(new Rect(new PointF(position.X - drag.GrabOffset.X, position.Y - drag.GrabOffset.Y), size));
            }
        }

        internal void CompleteDrag(DragOperation drag, PointF position)
        {
            var target = drag.CurrentTarget;
            if (target != null && drag.LastEffect != DragDropEffects.None)
            {
                var args = new DragEventArgs(drag) { Position = position, Effect = drag.LastEffect };
                target.OnDrop(args);
                FinishDrag(drag, canceled: false, target, position, args.Effect & drag.AllowedEffects);
            }
            else
            {
                target?.OnDragLeave(new DragEventArgs(drag) { Position = position });
                FinishDrag(drag, canceled: true, target: null, position, DragDropEffects.None);
            }
        }

        private void FinishDrag(DragOperation drag, bool canceled, Control? target, PointF position, DragDropEffects effect)
        {
            if (drag.DragVisual is { } ghost && ReferenceEquals(ghost.Parent, this))
                ghost.Parent = null;
            drag.IsActive = false;
            drag.CurrentTarget = null;
            _activeDrag = null;
            drag.Source.ResetInputs();
            if (canceled)
                drag.RaiseCanceled();
            else
                drag.RaiseCompleted(target, effect, position);
        }

        internal Control? CapturedMouseControl => _capturedMouseControl;

        public void CaptureMouse(Control control)
        {
            if (control.Screen != this)
                return;

            _capturedMouseControl = control;
        }

        public void ReleaseMouse(Control control)
        {
            if (_capturedMouseControl == control)
                _capturedMouseControl = null;
        }

        internal bool RouteCapturedMouseMove(PointF position, List<MouseButton> buttons)
        {
            var args = new MouseEventArgs(position, buttons);
            return RouteCapturedMouseInput(args, (control, eventArgs) => control.OnMouseMove(eventArgs));
        }

        internal bool RouteCapturedMouseUp(PointF position, MouseButton button)
        {
            var args = new MouseEventArgs(position, button);
            return RouteCapturedMouseInput(args, (control, eventArgs) => control.OnMouseUp(eventArgs));
        }

        private bool RouteCapturedMouseInput(MouseEventArgs args, Action<Control, MouseEventArgs> action)
        {
            var captured = _capturedMouseControl;
            if (captured == null)
                return false;

            if (captured.IsGone || !captured.IsVisible || !captured.IsEnabled)
            {
                _capturedMouseControl = null;
                return false;
            }

            action(captured, args);
            return true;
        }

        public void ClearFlyOut()
        {
            if (_flyOut == null)
                return;

            var flyOut = _flyOut;
            var contextMenu = _activeContextMenu;
            var animationStyle = _activeFlyOutAnimationStyle;
            _flyOut = null;
            _activeContextMenu = null;
            _activeFlyOutAnimationStyle = FlyOutAnimationStyle.Popup;
            _dismissingFlyOut = flyOut;
            _dismissingContextMenu = contextMenu;
            contextMenu?.OnClosing();
            flyOut.NotifyDismissing();
            AnimateFlyOutOut(flyOut, animationStyle, () =>
            {
                if (_dismissingFlyOut != flyOut)
                    return;

                flyOut.Parent = null;
                flyOut.Dispose();
                flyOut.NotifyDismissed();
                contextMenu?.OnClosed();
                _dismissingFlyOut = null;
                if (_dismissingContextMenu == contextMenu)
                    _dismissingContextMenu = null;
            });
        }

        internal Control? FlyOutContent => FlyOut?.Content;

        private enum FlyOutAnimationStyle
        {
            Popup,
            DropDown
        }

        private static void AnimateFlyOutIn(FlyOut flyOut, FlyOutAnimationStyle style)
        {
            var animatedControl = flyOut.Content ?? flyOut;
            if (style == FlyOutAnimationStyle.DropDown)
                AnimateDropDownIn(animatedControl, 0.96f, TimeSpan.FromMilliseconds(120));
            else
                AnimatePopupIn(animatedControl, 0.96, new Vector2(0, 6), TimeSpan.FromMilliseconds(120));
        }

        private static void AnimateFlyOutOut(FlyOut flyOut, FlyOutAnimationStyle style, Action completed)
        {
            var animatedControl = flyOut.Content ?? flyOut;
            if (style == FlyOutAnimationStyle.DropDown)
                AnimateDropDownOut(animatedControl, 0.96f, TimeSpan.FromMilliseconds(90), completed);
            else
                AnimatePopupOut(animatedControl, 0.96, new Vector2(0, 6), TimeSpan.FromMilliseconds(90), completed);
        }

        private static void AnimatePopupIn(Control control, double startScale, Vector2 startTranslation, TimeSpan duration)
        {
            control.Opacity = 0;
            control.Scale = new Vector2((float)startScale, (float)startScale);
            control.Translation = startTranslation;
            control.Animate()
                .FadeTo(1)
                .Scale(1)
                .TranslateTo(Vector2.Zero)
                .Duration(duration)
                .Ease(Easings.CubicOut)
                .Start();
        }

        private static void AnimateDropDownIn(Control control, float startScaleY, TimeSpan duration)
        {
            control.Opacity = 0;
            control.Scale = new Vector2(1, startScaleY);
            control.Translation = GetTopAnchoredScaleTranslation(control, startScaleY);
            control.Animate()
                .FadeTo(1)
                .Scale(Vector2.One)
                .TranslateTo(Vector2.Zero)
                .Duration(duration)
                .Ease(Easings.CubicOut)
                .Start();
        }

        private static void AnimateDropDownOut(Control control, float targetScaleY, TimeSpan duration, Action completed)
        {
            control.Animate()
                .FadeTo(0)
                .Scale(new Vector2(1, targetScaleY))
                .TranslateTo(GetTopAnchoredScaleTranslation(control, targetScaleY))
                .Duration(duration)
                .Ease(Easings.CubicOut)
                .OnCompleted(completed)
                .Start();
        }

        private static Vector2 GetTopAnchoredScaleTranslation(Control control, float scaleY)
        {
            var height = control.ClippingRect.Height > 0 ? control.ClippingRect.Height : control.BoundingRect.Height;
            return new Vector2(0, -height * (1 - scaleY) / 2);
        }

        private static void AnimatePopupOut(Control control, double targetScale, Vector2 targetTranslation, TimeSpan duration, Action completed)
        {
            control.Animate()
                .FadeTo(0)
                .Scale(targetScale)
                .TranslateTo(targetTranslation)
                .Duration(duration)
                .Ease(Easings.CubicOut)
                .OnCompleted(completed)
                .Start();
        }

        private static void RemoveFlyOutNow(FlyOut flyOut, ContextMenu? contextMenu)
        {
            contextMenu?.OnClosing();
            flyOut.NotifyDismissing();
            flyOut.Parent = null;
            flyOut.Dispose();
            flyOut.NotifyDismissed();
            contextMenu?.OnClosed();
        }

        internal static Rect ClampPopupRect(Rect preferredRect, Rect screenRect, float padding)
        {
            if (screenRect.Width <= 0 || screenRect.Height <= 0)
                return preferredRect;

            var result = preferredRect;
            var availableWidth = Math.Max(0, screenRect.Width - padding * 2);
            var availableHeight = Math.Max(0, screenRect.Height - padding * 2);

            if (availableWidth > 0 && result.Width > availableWidth)
                result.Width = availableWidth;
            if (availableHeight > 0 && result.Height > availableHeight)
                result.Height = availableHeight;

            var minLeft = screenRect.Left + padding;
            var maxLeft = screenRect.Right - padding - result.Width;
            result.Left = maxLeft < minLeft ? minLeft : Math.Max(minLeft, Math.Min(result.Left, maxLeft));

            var minTop = screenRect.Top + padding;
            var maxTop = screenRect.Bottom - padding - result.Height;
            result.Top = maxTop < minTop ? minTop : Math.Max(minTop, Math.Min(result.Top, maxTop));

            return result;
        }

        public void ShowKeyboard()
        {
            var currentKeyboard = ScreenEngine?.CurrentKeyboard;
            if (currentKeyboard != null && !_mainGrid.Children.Contains(currentKeyboard.Control))
                _mainGrid.AddChild(currentKeyboard.Control, 1);
            _mainGrid.InvalidateLayout(true);
        }

        public void HideKeyboard()
        {
            var currentKeyboard = ScreenEngine?.CurrentKeyboard;
            if (currentKeyboard == null)
                return;
            _mainGrid.Children.Remove(currentKeyboard.Control);
            _mainGrid.InvalidateLayout(true);
        }
    }
}
