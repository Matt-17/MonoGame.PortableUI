using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Input;
using MonoGame.PortableUI.Localization;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     Android-style touch selection of one <see cref="TextBox"/>, drawn above a screen's content: the
    ///     teardrop handle under the caret, the two handles at the selection ends and the floating
    ///     Cut / Copy / Paste / Select all bar. Not modal: touches that miss it reach the page.
    /// </summary>
    internal sealed class TextSelectionLayer : Adorner
    {
        private enum Handle
        {
            None,
            Insertion,
            Start,
            End
        }

        /// <summary>Android hides an untouched caret handle after a while.</summary>
        internal static readonly TimeSpan InsertionHandleTimeout = TimeSpan.FromSeconds(4);

        private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(150);

        // Gap between the toolbar and the text, and from the toolbar to the screen edge.
        private const float ToolbarGap = 8;
        private const float ToolbarScreenMargin = 16;

        private readonly Border _toolbar;
        private readonly StackPanel _items;
        private TextBox? _owner;
        private bool _insertion;
        private bool _toolbarShown;
        private Handle _dragging;
        private PointF _dragOffset;
        private TimeSpan _insertionShownAt;
        private long _toolbarThemeVersion = -1;

        public TextSelectionLayer()
        {
            _items = new StackPanel { Orientation = Orientation.Horizontal };
            _toolbar = new Border { Content = _items, IsVisible = false };
            Content = _toolbar;
            TouchDown += OnLayerTouchDown;
            TouchMove += OnLayerTouchMove;
            TouchUp += OnLayerTouchUp;
            TouchCancel += (_, _) => EndDrag();
        }

        /// <summary>The text box whose selection is shown, or null.</summary>
        internal TextBox? Owner => _owner;

        /// <summary>Handles or the toolbar are showing.</summary>
        internal bool IsShown => _owner != null && (HandlesShown || _toolbarShown);

        internal bool ToolbarShown => _owner != null && _toolbarShown;

        internal bool InsertionHandleShown => _owner != null && _insertion && !_owner.HasSelection;

        private bool HandlesShown => _owner != null && (_owner.HasSelection || _insertion);

        /// <summary>The caret handle (after a tap); <paramref name="toolbar"/> adds Paste / Select all.</summary>
        internal void ShowInsertion(TextBox owner, bool toolbar)
        {
            _owner = owner;
            _insertion = owner.Text.Length > 0;
            _insertionShownAt = ScreenSystem.TotalTime;
            SetToolbar(toolbar);
            RequestRedraw();
        }

        /// <summary>The selection handles; <paramref name="toolbar"/> shows the bar now (otherwise on <see cref="ShowToolbar"/>).</summary>
        internal void ShowSelection(TextBox owner, bool toolbar)
        {
            _owner = owner;
            _insertion = false;
            SetToolbar(toolbar);
            RequestRedraw();
        }

        internal void ShowToolbar() => SetToolbar(true);

        internal void HideToolbar() => SetToolbar(false);

        /// <summary>Removes handles and toolbar (the selection itself stays).</summary>
        internal void Hide(TextBox? owner = null)
        {
            if (owner != null && !ReferenceEquals(owner, _owner))
                return;
            _owner = null;
            _insertion = false;
            _dragging = Handle.None;
            SetToolbar(false);
            RequestRedraw();
        }

        /// <summary>Whether a touch at <paramref name="point"/> (layout units) belongs to a handle or the toolbar.</summary>
        protected internal override bool HitTest(PointF point)
        {
            if (!IsShown || _owner is not { } owner)
                return false;
            if (_toolbarShown && _toolbar.IsVisible && _toolbar.ClippingRect.Contains(point))
                return true;
            return HandleAt(owner, point) != Handle.None;
        }

        /// <summary>A touch went down elsewhere on the page: outside the text box the selection UI closes.</summary>
        protected internal override void OnPressOutside(PointF point)
        {
            if (_owner is not { } owner)
                return;
            if (owner.ClippingRect.Contains(point))
                return;
            if (owner.HasSelection)
                owner.CollapseSelectionToEnd();
            Hide();
        }

        /// <summary>Per update: follows the owner, times out the caret handle.</summary>
        protected internal override void OnUpdate()
        {
            if (_owner is not { } owner)
                return;
            if (!owner.IsFocused || owner.Screen == null || !owner.IsEffectivelyInteractive)
            {
                Hide();
                return;
            }
            if (_insertion && !_toolbarShown && _dragging == Handle.None && !owner.HasSelection
                && ScreenSystem.TotalTime - _insertionShownAt >= InsertionHandleTimeout + FadeDuration)
            {
                Hide();
                return;
            }
            if (_toolbarShown && _platformMenuShown)
            {
                if (!_platformMenuSuspended)
                    MovePlatformMenu(owner);
            }
            else if (_toolbarShown)
                PlaceToolbar(owner);
            // The owner may scroll, move or animate under the handles.
            RequestRedraw();
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            if (_toolbarShown && !_platformMenuShown && _owner is { } owner)
                PlaceToolbar(owner);
        }

        private void SetToolbar(bool shown)
        {
            if (!shown)
                _overflowOpen = false;
            var owner = _owner;
            if (shown && owner != null && PlatformMenu is { } menu)
            {
                var actions = Actions(owner);
                _toolbarShown = actions.Count > 0;
                _toolbar.IsVisible = false;
                if (_toolbarShown)
                    ShowPlatformMenu(menu, owner, actions);
                else
                    HidePlatformMenu();
                InvalidateLayout(false);
                return;
            }
            HidePlatformMenu();
            if (shown && owner != null)
                shown = BuildToolbar(owner);
            _toolbarShown = shown;
            _toolbar.IsVisible = shown;
            if (shown && owner != null)
                PlaceToolbar(owner);
            InvalidateLayout(false);
        }

        private bool BuildToolbar(TextBox owner)
        {
            var theme = ResolveTheme();
            _items.Children.Clear();
            var entries = new List<(string Key, Action Action)>();
            foreach (var entry in Actions(owner))
                entries.Add((entry.ToString(), () => Run(owner, entry)));
            if (entries.Count == 0)
                return false;

            var height = theme.TextSelectionToolbarHeight;
            var color = theme.TextSelectionToolbarTextColor;
            _toolbar.BackgroundBrush = theme.TextSelectionToolbarBrush;
            _toolbar.Shadow = theme.TextSelectionToolbarShadow;
            _toolbarThemeVersion = ThemeVersion.Current;

            // What does not fit across the screen goes behind a ⋮ button, as on Android.
            var screen = Screen?.ScreenRect ?? ClippingRect;
            var available = screen.Width > 0 ? screen.Width - ToolbarScreenMargin * 2 : float.MaxValue;
            var buttons = new List<Button>(entries.Count);
            foreach (var (key, action) in entries)
            {
                var button = ToolbarButton(new TextBlock { Text = Label(key), VerticalAlignment = VerticalAlignment.Center }, height, color);
                button.Click += (_, _) => action();
                buttons.Add(button);
            }
            var inBar = buttons.Count;
            if (TotalWidth(buttons, buttons.Count) > available)
            {
                inBar = buttons.Count - 1;
                while (inBar > 1 && TotalWidth(buttons, inBar) + OverflowButtonWidth > available)
                    inBar--;
            }

            if (_overflowOpen && inBar < buttons.Count)
            {
                // The overflow: a column of the remaining entries under a back arrow.
                _items.Orientation = Orientation.Vertical;
                _toolbar.Height = float.NaN;
                _toolbar.CornerRadius = new CornerRadius(Math.Min(height / 2, 16));
                var back = ToolbarButton(Icon(BackArrow(), color), height, color);
                back.HorizontalAlignment = HorizontalAlignment.Left;
                back.Click += (_, _) => { _overflowOpen = false; SetToolbar(true); };
                _items.Children.Add(back);
                for (var i = inBar; i < buttons.Count; i++)
                {
                    var row = buttons[i];
                    row.Padding = new Thickness(18, 0, 18, 0);
                    row.HorizontalAlignment = HorizontalAlignment.Stretch;
                    if (row.Content is TextBlock label)
                        label.HorizontalAlignment = HorizontalAlignment.Left;
                    _items.Children.Add(row);
                }
                return true;
            }

            _overflowOpen = false;
            _items.Orientation = Orientation.Horizontal;
            _toolbar.Height = height;
            _toolbar.CornerRadius = new CornerRadius(height / 2);
            for (var i = 0; i < inBar; i++)
                _items.Children.Add(buttons[i]);
            if (inBar < buttons.Count)
            {
                var more = ToolbarButton(Icon(MoreDots(), color), height, color);
                more.Width = OverflowButtonWidth;
                more.Click += (_, _) =>
                {
                    // The menu unfolds from the ⋮: it keeps the bar's right edge.
                    _overflowRight = _toolbar.ClippingRect.Right;
                    _overflowOpen = true;
                    SetToolbar(true);
                };
                _items.Children.Add(more);
            }
            // Android: 11 dp around each label, 18 dp at the ends of the bar; the ends follow its round corners.
            var count = _items.Children.Count;
            for (var i = 0; i < count; i++)
            {
                var button = (Button)_items.Children[i];
                var first = i == 0;
                var last = i == count - 1;
                var radius = height / 2;
                if (button.Content is TextBlock)
                    button.Padding = new Thickness(first ? 18 : 11, 0, last ? 18 : 11, 0);
                button.CornerRadius = new CornerRadius(first ? radius : 0, last ? radius : 0, last ? radius : 0, first ? radius : 0);
            }
            return true;
        }

        private const float OverflowButtonWidth = 48;

        private bool _overflowOpen;
        private float _overflowRight;

        // The label color goes on the button: it re-colors its label on every press and hover.
        private static Button ToolbarButton(Control content, float height, Color textColor) => new Button
        {
            UseThemeStyle = false,
            Content = content,
            TextColor = textColor,
            HoverTextColor = null,
            PressedTextColor = null,
            BackgroundBrush = Color.Transparent,
            HoverColor = new Color(0, 0, 0, 20),
            PressedColor = new Color(0, 0, 0, 31),
            Padding = new Thickness(11, 0, 11, 0),
            Height = height,
            MinWidth = 48
        };

        // The bar's width with the first `count` buttons at their bar padding (18 dp at the ends).
        private static float TotalWidth(List<Button> buttons, int count)
        {
            var width = 0f;
            for (var i = 0; i < count; i++)
                width += buttons[i].Content!.Measure().Width + 22;
            return width + 14;
        }

        private static PathShape Icon(PathGeometry geometry, Color color) => new PathShape
        {
            Geometry = geometry,
            Fill = color,
            Width = 24,
            Height = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        /// <summary>Material more_vert (24 dp viewport).</summary>
        private static PathGeometry MoreDots() => new PathGeometry(24, 24)
            .AddEllipse(12, 6, 2, 2)
            .AddEllipse(12, 12, 2, 2)
            .AddEllipse(12, 18, 2, 2);

        /// <summary>Material arrow_back (24 dp viewport).</summary>
        private static PathGeometry BackArrow() => new PathGeometry(24, 24)
            .MoveTo(20, 11).LineTo(7.83f, 11).LineTo(13.42f, 5.41f).LineTo(12, 4).LineTo(4, 12)
            .LineTo(12, 20).LineTo(13.41f, 18.59f).LineTo(7.83f, 13).LineTo(20, 13).Close();

        /// <summary>
        ///     A toolbar label: the <see cref="Localizer.Default"/> string "PortableUI.Cut" (".Copy", ".Paste",
        ///     ".SelectAll") when an app supplies one, otherwise built-in English or German.
        /// </summary>
        internal static string Label(string key)
        {
            if (Localizer.Default.TryGet("PortableUI." + key, out var localized))
                return localized;
            var german = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de";
            return key switch
            {
                "Cut" => german ? "Ausschneiden" : "Cut",
                "Copy" => german ? "Kopieren" : "Copy",
                "Paste" => german ? "Einfügen" : "Paste",
                _ => german ? "Alles auswählen" : "Select all"
            };
        }

        /// <summary>The entries the text box offers now, in Android's order.</summary>
        private static List<TextSelectionAction> Actions(TextBox owner)
        {
            var actions = new List<TextSelectionAction>(4);
            var secret = owner.PasswordChar.HasValue;
            if (owner.HasSelection)
            {
                if (!owner.IsReadOnly && !secret)
                    actions.Add(TextSelectionAction.Cut);
                if (!secret)
                    actions.Add(TextSelectionAction.Copy);
            }
            if (!owner.IsReadOnly && owner.ClipboardHasText())
                actions.Add(TextSelectionAction.Paste);
            if (owner.Text.Length > 0 && owner.SelectionLength < owner.Text.Length)
                actions.Add(TextSelectionAction.SelectAll);
            return actions;
        }

        private void Run(TextBox owner, TextSelectionAction action)
        {
            switch (action)
            {
                case TextSelectionAction.Cut:
                    owner.Cut();
                    Hide();
                    break;
                case TextSelectionAction.Copy:
                    owner.Copy();
                    owner.CollapseSelectionToEnd();
                    Hide();
                    break;
                case TextSelectionAction.Paste:
                    owner.Paste();
                    Hide();
                    break;
                case TextSelectionAction.SelectAll:
                    owner.SelectAll();
                    ShowSelection(owner, true);
                    break;
            }
        }

        // The platform's own menu ---------------------------------------------------------------------

        private ITextSelectionMenu? PlatformMenu => (Screen?.ScreenEngine ?? ScreenEngine.Instance)?.Options.TextSelectionMenu;

        private bool _platformMenuShown;
        private bool _platformMenuSuspended;
        private int _platformMenuToken;
        private Rect _platformMenuRect;
        private List<TextSelectionAction>? _platformMenuActions;

        private void ShowPlatformMenu(ITextSelectionMenu menu, TextBox owner, List<TextSelectionAction> actions)
        {
            _platformMenuActions = actions;
            _platformMenuToken++;
            _platformMenuShown = true;
            SendPlatformMenu(menu, owner);
        }

        private void SendPlatformMenu(ITextSelectionMenu menu, TextBox owner)
        {
            var engine = Screen?.ScreenEngine ?? ScreenEngine.Instance;
            if (engine == null || _platformMenuActions == null)
                return;
            _platformMenuRect = engine.LayoutToWindow(ToolbarAnchor(owner));
            var token = _platformMenuToken;
            menu.Show(new TextSelectionMenuRequest
            {
                ContentRect = _platformMenuRect,
                Actions = _platformMenuActions,
                SelectedText = owner.HasSelection && !owner.PasswordChar.HasValue ? owner.SelectedText : null,
                Invoke = action => engine.InvokeOnGameThread(() =>
                {
                    if (ReferenceEquals(_owner, owner) && token == _platformMenuToken)
                        Run(owner, action);
                }),
                Dismissed = () => engine.InvokeOnGameThread(() =>
                {
                    if (token != _platformMenuToken || !_platformMenuShown)
                        return;
                    _platformMenuShown = false;
                    _toolbarShown = false;
                })
            });
        }

        // The selection moved (scrolling, a handle): the menu follows.
        private void MovePlatformMenu(TextBox owner)
        {
            if (PlatformMenu is not { } menu || (Screen?.ScreenEngine ?? ScreenEngine.Instance) is not { } engine)
                return;
            var rect = engine.LayoutToWindow(ToolbarAnchor(owner));
            if (Math.Abs(rect.Left - _platformMenuRect.Left) < 0.5f && Math.Abs(rect.Top - _platformMenuRect.Top) < 0.5f
                && Math.Abs(rect.Width - _platformMenuRect.Width) < 0.5f && Math.Abs(rect.Height - _platformMenuRect.Height) < 0.5f)
                return;
            SendPlatformMenu(menu, owner);
        }

        private void HidePlatformMenu()
        {
            if (!_platformMenuShown)
                return;
            _platformMenuShown = false;
            _platformMenuToken++;
            PlatformMenu?.Hide();
        }

        /// <summary>What the bar stands next to (layout units): the selection or caret line, down to its handles.</summary>
        private Rect ToolbarAnchor(TextBox owner)
        {
            var handleSize = ResolveTheme().TextSelectionHandleSize;
            float left, right, top, bottom;
            if (owner.HasSelection)
            {
                var startShown = owner.TryGetCaretPoint(owner.SelectionStart, out var start, out var lineHeight);
                var endShown = owner.TryGetCaretPoint(owner.SelectionStart + owner.SelectionLength, out var end, out _);
                var text = owner.ClippingRect;
                if (!startShown && !endShown)
                {
                    left = text.Left;
                    right = text.Right;
                    top = text.Top;
                    bottom = text.Bottom;
                }
                else
                {
                    var a = startShown ? start : end;
                    var b = endShown ? end : start;
                    // Over the selection; spanning lines it is centred on the box.
                    var sameLine = Math.Abs(a.Y - b.Y) < 0.5f;
                    left = sameLine ? Math.Min(a.X, b.X) : text.Left;
                    right = sameLine ? Math.Max(a.X, b.X) : text.Right;
                    top = Math.Min(a.Y, b.Y) - lineHeight;
                    bottom = Math.Max(a.Y, b.Y) + handleSize;
                }
            }
            else if (owner.TryGetCaretPoint(owner.CursorPosition, out var caret, out var lineHeight))
            {
                left = right = caret.X;
                top = caret.Y - lineHeight;
                bottom = caret.Y + (InsertionHandleShown ? handleSize : 0);
            }
            else
            {
                var text = owner.ClippingRect;
                left = text.Left;
                right = text.Right;
                top = text.Top;
                bottom = text.Bottom;
            }
            return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        }

        private void PlaceToolbar(TextBox owner)
        {
            if (_toolbarThemeVersion != ThemeVersion.Current)
                BuildToolbar(owner);
            var size = _toolbar.Measure();
            var screen = Screen?.ScreenRect ?? ClippingRect;
            var anchor = ToolbarAnchor(owner);
            var left = anchor.Left;
            var right = anchor.Right;
            var top = anchor.Top;
            var bottom = anchor.Bottom;

            var x = _overflowOpen ? _overflowRight - size.Width : (left + right) / 2 - size.Width / 2;
            x = Math.Max(screen.Left + ToolbarScreenMargin, Math.Min(x, screen.Right - ToolbarScreenMargin - size.Width));
            var y = top - ToolbarGap - size.Height;
            if (y < screen.Top + ToolbarScreenMargin)
                y = bottom + ToolbarGap;
            _toolbar.UpdateLayout(new Rect(x, y, size.Width, size.Height));
        }

        // Handles -----------------------------------------------------------------------------------

        private Handle HandleAt(TextBox owner, PointF point)
        {
            // Touch targets a little larger than the drawn teardrops.
            var slop = ResolveTheme().TextSelectionHandleSize * 0.5f;
            if (owner.HasSelection)
            {
                if (HandleRect(owner, Handle.End) is { } end && Inflate(end, slop).Contains(point))
                    return Handle.End;
                if (HandleRect(owner, Handle.Start) is { } start && Inflate(start, slop).Contains(point))
                    return Handle.Start;
                return Handle.None;
            }
            if (InsertionHandleShown && HandleRect(owner, Handle.Insertion) is { } caret && Inflate(caret, slop).Contains(point))
                return Handle.Insertion;
            return Handle.None;
        }

        private static Rect Inflate(Rect rect, float by) => new Rect(rect.Left - by, rect.Top - by / 2, rect.Width + by * 2, rect.Height + by * 1.5f);

        /// <summary>A handle's box in layout units, null when its end of the text is scrolled out of view.</summary>
        private Rect? HandleRect(TextBox owner, Handle handle)
        {
            var size = ResolveTheme().TextSelectionHandleSize;
            switch (handle)
            {
                case Handle.Start when owner.TryGetCaretPoint(owner.SelectionStart, out var start, out _):
                    return new Rect(start.X - size, start.Y, size, size);
                case Handle.End when owner.TryGetCaretPoint(owner.SelectionStart + owner.SelectionLength, out var end, out _):
                    return new Rect(end.X, end.Y, size, size);
                case Handle.Insertion when owner.TryGetCaretPoint(owner.CursorPosition, out var caret, out _):
                    // Material's caret handle: a 20 dp circle (for a 22 dp handle size) with a point 45° up.
                    var radius = size / 2.2f;
                    return new Rect(caret.X - radius, caret.Y, radius * 2, radius * (1 + MathF.Sqrt(2)));
                default:
                    return null;
            }
        }

        private PointF HandleTip(TextBox owner, Handle handle)
        {
            var position = handle switch
            {
                Handle.Start => owner.SelectionStart,
                Handle.End => owner.SelectionStart + owner.SelectionLength,
                _ => owner.CursorPosition
            };
            owner.TryGetCaretPoint(position, out var tip, out _);
            return tip;
        }

        private void OnLayerTouchDown(object? sender, TouchEventArgs args)
        {
            if (_owner is not { } owner || (_toolbarShown && _toolbar.IsVisible && _toolbar.ClippingRect.Contains(args.Position)))
                return;
            _dragging = HandleAt(owner, args.Position);
            if (_dragging == Handle.None)
                return;
            var tip = HandleTip(owner, _dragging);
            _dragOffset = new PointF(args.Position.X - tip.X, args.Position.Y - tip.Y);
            // Android hides its menu while a handle moves and brings it back afterwards (it is not closed).
            if (_platformMenuShown)
            {
                _platformMenuSuspended = true;
                PlatformMenu?.Suspend();
            }
            else
                SetToolbar(false);
            args.Handled = true;
        }

        private void OnLayerTouchMove(object? sender, TouchEventArgs args)
        {
            if (_dragging == Handle.None || _owner is not { } owner)
                return;
            if (_platformMenuSuspended)
                PlatformMenu?.Suspend();
            owner.TryGetCaretPoint(owner.CursorPosition, out _, out var lineHeight);
            // The tip follows the finger; the line above the tip is the one the handle points at.
            var target = new PointF(args.Position.X - _dragOffset.X, args.Position.Y - _dragOffset.Y - lineHeight / 2);
            var position = owner.GetTextPositionAt(target);
            var start = owner.SelectionStart;
            var end = start + owner.SelectionLength;
            switch (_dragging)
            {
                case Handle.Start:
                    // The ends never cross: at least one character stays selected.
                    owner.SetSelectionEnds(end, Math.Min(position, end - 1));
                    break;
                case Handle.End:
                    owner.SetSelectionEnds(start, Math.Max(position, start + 1));
                    break;
                case Handle.Insertion:
                    owner.SetSelectionEnds(position, position);
                    _insertionShownAt = ScreenSystem.TotalTime;
                    break;
            }
            RequestRedraw();
        }

        private void OnLayerTouchUp(object? sender, TouchEventArgs args)
        {
            var dragged = _dragging;
            EndDrag();
            if (dragged == Handle.None || _owner is not { } owner)
                return;
            if (owner.HasSelection || _toolbarShown)
                SetToolbar(true);
            _insertionShownAt = ScreenSystem.TotalTime;
        }

        private void EndDrag()
        {
            _dragging = Handle.None;
            _platformMenuSuspended = false;
        }

        // Drawing -----------------------------------------------------------------------------------

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            if (_owner is not { } owner || !HandlesShown)
                return;
            var theme = ResolveTheme();
            var color = theme.TextSelectionHandleColor
                ?? (owner.CursorColor is SolidColorBrush caret ? caret.Color : Color.Black);
            var layout = ClippingRect;
            var scale = layout.Width > 0 ? rect.Width / layout.Width : 1f;
            Rect ToRenderRect(Rect r) => new Rect(rect.Left + (r.Left - layout.Left) * scale, rect.Top + (r.Top - layout.Top) * scale, r.Width * scale, r.Height * scale);

            if (owner.HasSelection)
            {
                var half = theme.TextSelectionHandleSize * scale / 2;
                // A circle with the corner at the text squared off: the start handle's top right, the end's top left.
                if (HandleRect(owner, Handle.Start) is { } start)
                    RoundedRectRenderer.DrawSolid(spriteBatch, ToRenderRect(start), new CornerRadius(half, 0, half, half), Brush.ApplyOpacity(color, RenderOpacity));
                if (HandleRect(owner, Handle.End) is { } end)
                    RoundedRectRenderer.DrawSolid(spriteBatch, ToRenderRect(end), new CornerRadius(0, half, half, half), Brush.ApplyOpacity(color, RenderOpacity));
                return;
            }

            if (!_insertion || HandleRect(owner, Handle.Insertion) is not { } box)
                return;
            var opacity = 1f;
            if (!_toolbarShown && _dragging == Handle.None)
            {
                var hideAt = _insertionShownAt + InsertionHandleTimeout;
                var now = ScreenSystem.TotalTime;
                if (now >= hideAt)
                    opacity = 1 - (float)Math.Min(1, (now - hideAt).TotalMilliseconds / FadeDuration.TotalMilliseconds);
                else
                    ScreenEngine.RequestAnimationFrameAt(hideAt);
                if (opacity < 1)
                    ScreenEngine.RequestAnimationFrame();
            }
            DrawTeardrop(spriteBatch, ToRenderRect(box), Brush.ApplyOpacity(color, RenderOpacity * opacity));
        }

        // The caret handle: a circle with a point straight up, rasterised once per pixel size and color.
        private static readonly Dictionary<(int Width, int Height, uint Color), Texture2D> Teardrops = new();
        private static GraphicsDevice? _teardropDevice;
        private static int _teardropReset = -1;

        private static void DrawTeardrop(SpriteBatch spriteBatch, Rect box, Color color)
        {
            var device = spriteBatch.GraphicsDevice;
            var width = (int)MathF.Ceiling(box.Width);
            var height = (int)MathF.Ceiling(box.Height);
            if (width <= 0 || height <= 0 || color.A == 0)
                return;
            var reset = PathRasterizer.GetResetGeneration(device);
            if (!ReferenceEquals(device, _teardropDevice) || reset != _teardropReset)
            {
                foreach (var stale in Teardrops.Values)
                    stale.Dispose();
                Teardrops.Clear();
                _teardropDevice = device;
                _teardropReset = reset;
            }
            // Drawn white and tinted, so a fading handle reuses its texture.
            if (!Teardrops.TryGetValue((width, height, 0), out var texture) || texture.IsDisposed)
            {
                var r = width / 2f;
                var cy = height - r;
                var quarter = MathF.PI / 4;
                var geometry = new PathGeometry(width, height)
                    .MoveTo(r, 0)
                    .LineTo(r + r * MathF.Cos(-quarter), cy + r * MathF.Sin(-quarter))
                    .ArcTo(r, cy, r, -quarter, 1.5f * MathF.PI)
                    .Close();
                texture = PathRasterizer.CreateTexture(device, geometry, width, height, Color.White);
                Teardrops[(width, height, 0)] = texture;
            }
            spriteBatch.Draw(texture, new Vector2(MathF.Round(box.Left), MathF.Round(box.Top)), color);
        }
    }
}
