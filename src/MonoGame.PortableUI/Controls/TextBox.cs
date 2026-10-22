using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Input;
using MonoGame.PortableUI.Media;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Controls
{
    public class TextBox : TextBlock
    {
        protected override bool UsesThemeMinimumSize => true;

        private protected override bool WrapsToArrangedWidth => false;

        private IKeyboard? _attachedKeyboard;
        private int _cursorPosition;
        private int _maxLength;
        private int _selectionAnchor;
        private bool _isMultiline;
        private bool _isPointerSelecting;
        private float? _desiredCursorX;
        private char? _passwordChar;
        private float _horizontalScrollOffset;
        private float _verticalScrollOffset;
        private LineMetricsCache? _lineMetricsCache;

        public int CursorPosition
        {
            get { return _cursorPosition; }
            set { MoveCursorTo(value, false); }
        }

        public int MaxLength
        {
            get { return _maxLength; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "MaxLength cannot be negative.");

                if (_maxLength == value)
                    return;

                _maxLength = value;
                if (_maxLength > 0 && Text.Length > _maxLength)
                    Text = Text.Substring(0, _maxLength);
            }
        }

        public bool IsReadOnly { get; set; }

        public char? PasswordChar
        {
            get { return _passwordChar; }
            set
            {
                if (_passwordChar == value)
                    return;

                _passwordChar = value;
                InvalidateLineMetrics();
                EnsureCursorVisible();
                InvalidateLayout(true);
            }
        }

        protected internal override bool HandlesDirection(FocusDirection direction)
        {
            return direction is FocusDirection.Left or FocusDirection.Right || IsMultiline;
        }

        public bool IsMultiline
        {
            get { return _isMultiline; }
            set
            {
                if (_isMultiline == value)
                    return;

                _isMultiline = value;
                InvalidateLineMetrics();
                Text = Text;
                EnsureCursorVisible();
                InvalidateLayout(true);
            }
        }

        public int SelectionStart => Math.Min(_selectionAnchor, _cursorPosition);
        public int SelectionLength => Math.Abs(_selectionAnchor - _cursorPosition);
        public bool HasSelection => SelectionLength > 0;
        public string SelectedText => HasSelection ? Text.Substring(SelectionStart, SelectionLength) : "";

        internal float HorizontalScrollOffset => _horizontalScrollOffset;
        internal float VerticalScrollOffset => _verticalScrollOffset;

        public Brush CursorColor { get; set; }

        /// <summary>Caret shape (bar, or the text-mode underline/block).</summary>
        public CaretStyle CaretStyle { get; set; }

        /// <summary>Half period of the caret blink.</summary>
        public TimeSpan CaretBlinkInterval { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>Typed characters replace the one at the caret (Insert key toggles).</summary>
        public bool IsOverwriteMode { get; set; }
        public Brush SelectionBrush { get; set; }

        /// <summary>
        ///     Colour of selected text; null keeps <see cref="TextBlock.TextColor"/> under a translucent
        ///     <see cref="SelectionBrush"/> and inverts it under an opaque one (see
        ///     <see cref="PortableTheme.TextBoxSelectionTextColor"/>).
        /// </summary>
        public Color? SelectionTextColor { get; set; }

        public event EventHandler? EnterPressed;

        public string? InputScope { get; set; }

        /// <summary>Tells the platform keyboard what is typed here (digits, e-mail, ...).</summary>
        public TextInputPurpose InputPurpose { get; set; }

        public new string Text
        {
            get { return base.Text; }
            set
            {
                var normalized = LimitText(NormalizeText(value));
                if (base.Text == normalized)
                {
                    ClampSelection();
                    return;
                }

                var oldText = base.Text;
                base.Text = normalized;
                Screen?.ExistingTextSelection?.Hide(this);
                ClearUndoHistory();
                InvalidateLineMetrics();
                ClampSelection();
                ResetDesiredCursorX();
                EnsureCursorVisible();
                OnTextChanged(new TextChangedEventArgs(normalized, oldText));
            }
        }

        public string HintText { get; set; } = "Hint text";
        public Color HintTextColor { get; set; } = Color.Silver;

        /// <summary>
        ///     The hint becomes a label (Material's floating label): it rests where the text goes while the box is
        ///     empty and glides up, smaller, once the box has focus or text. Give the box room above its text
        ///     (a larger <see cref="Padding"/>.Top) for the raised label.
        /// </summary>
        public bool FloatingHint { get; set; }

        /// <summary>The raised label's colour while the box has focus; null keeps <see cref="HintTextColor"/>.</summary>
        public Color? FocusedHintColor { get; set; }

        /// <summary>Size of the raised label relative to the text (default 0.75).</summary>
        public float FloatingHintScale { get; set; } = 0.75f;

        // 0 = resting in place of the text, 1 = raised; follows its target with an ease-out each frame.
        private float _hintLift;

        /// <summary>Where the floating hint is: 0 resting, 1 raised.</summary>
        internal float HintLift => _hintLift;
        private float _hintFocus;
        private TimeSpan _hintTime;

        public Thickness Padding { get; set; }
        public event TextChangedEventHandler? TextChanged;

        public TextBox()
        {
            var theme = PortableTheme.ResolveCurrent();

            IsFocusable = true; // TextBlock disables this; text input needs focus back
            SeedThemeTextColor(theme.TextBoxTextColor);
            CursorColor = theme.TextBoxCursorBrush;
            CaretStyle = theme.TextBoxCaretStyle;
            CaretBlinkInterval = theme.TextBoxCaretBlinkInterval;
            SelectionBrush = theme.TextBoxSelectionBrush;
            SelectionTextColor = theme.TextBoxSelectionTextColor;
            HintTextColor = theme.TextBoxHintTextColor;
            KeyPressed += HandleKeyPressed;
            Click += OnClick;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            TouchDown += OnTouchDown;
            TouchMove += OnTouchMove;
            TouchUp += OnTouchUp;
            TouchCancel += OnTouchCancel;
            Height = theme.TextBoxHeight;
            Padding = theme.TextBoxPadding;
            ShowFocusVisual = true;
        }

        // After a click the ring shows where typing goes; a tapped field shows that by its caret and
        // focused look alone, like Android's and iOS's.
        internal override bool ShowsFocusVisualForPointer => ScreenEngine.For(this)?.LastInputDevice != Haptics.UiInputDevice.Touch;

        protected override ControlStyle? GetThemeStyle(PortableTheme theme)
        {
            return theme.TextBox;
        }

        protected override Color GetThemeTextColor(PortableTheme theme) => theme.TextBoxTextColor;

        // The editor clips its text to the box and draws no outline, glow or shadow.
        protected internal override Thickness GetInkOverflow() => InkOverflow;

        protected override Brush? GetThemeBackgroundBrush(PortableTheme theme)
        {
            return theme.TextBoxBackgroundBrush;
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            // TextBlock re-seeds a theme-driven TextColor through GetThemeTextColor.
            base.OnThemeChanged(oldTheme, newTheme);

            if (ReferenceEquals(CursorColor, oldTheme.TextBoxCursorBrush))
                CursorColor = newTheme.TextBoxCursorBrush;
            if (CaretStyle == oldTheme.TextBoxCaretStyle)
                CaretStyle = newTheme.TextBoxCaretStyle;
            if (CaretBlinkInterval == oldTheme.TextBoxCaretBlinkInterval)
                CaretBlinkInterval = newTheme.TextBoxCaretBlinkInterval;
            if (ReferenceEquals(SelectionBrush, oldTheme.TextBoxSelectionBrush))
                SelectionBrush = newTheme.TextBoxSelectionBrush;
            if (Nullable.Equals(SelectionTextColor, oldTheme.TextBoxSelectionTextColor))
                SelectionTextColor = newTheme.TextBoxSelectionTextColor;
            if (HintTextColor.Equals(oldTheme.TextBoxHintTextColor))
                HintTextColor = newTheme.TextBoxHintTextColor;
            if (Height.Equals(oldTheme.TextBoxHeight))
                Height = newTheme.TextBoxHeight;
            if (Padding.Equals(oldTheme.TextBoxPadding))
                Padding = newTheme.TextBoxPadding;
        }

        public void Select(int start, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Selection length cannot be negative.");

            var selectionStart = ClampTextPosition(start);
            var selectionEnd = ClampTextPosition(selectionStart + length);
            _selectionAnchor = selectionStart;
            _cursorPosition = selectionEnd;
            ResetDesiredCursorX();
            EnsureCursorVisible();
        }

        public void SelectAll()
        {
            _selectionAnchor = 0;
            _cursorPosition = Text.Length;
            ResetDesiredCursorX();
            EnsureCursorVisible();
        }

        public void ClearSelection()
        {
            _selectionAnchor = _cursorPosition;
            ResetDesiredCursorX();
            EnsureCursorVisible();
        }

        public void Copy()
        {
            if (!HasSelection || PasswordChar.HasValue)
                return;

            TrySetClipboardText(SelectedText);
        }

        public void Cut()
        {
            if (IsReadOnly || !HasSelection || PasswordChar.HasValue)
                return;

            TrySetClipboardText(SelectedText);
            DeleteSelection();
        }

        public void Paste()
        {
            if (IsReadOnly)
                return;

            var text = TryGetClipboardText();
            if (!string.IsNullOrEmpty(text))
                InsertText(text);
        }

        public override Size MeasureLayout()
        {
            if (IsGoneShown)
                return Size.Empty;

            var cache = GetLineMetricsCache();
            var lineHeight = GetLineHeight();
            var measuredWidth = 0f;

            foreach (var lineMetric in cache.LineMetrics)
                measuredWidth = Math.Max(measuredWidth, lineMetric.Width);

            _measuredLineCount = cache.Lines.Count;
            var measuredHeight = Math.Max(lineHeight, cache.Lines.Count * lineHeight);
            var width = Width.IsFixed() ? Width : measuredWidth + Padding.Horizontal;
            var height = Height.IsFixed() ? Height : measuredHeight + Padding.Vertical;
            // A single-line box at the theme's default height is never shorter than its line, so
            // scaled-up text (TextScaling, larger fonts) still fits. An explicit Height is kept.
            if (!IsMultiline && (!Height.IsFixed() || Height.Equals(ResolveTheme().TextBoxHeight)))
                height = Math.Max(height, lineHeight + Padding.Vertical);

            // Min/Max constrain the content box only; margin is added afterwards (same as Control).
            return ApplyConstraints(new Size(width, height)) + Margin;
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            EnsureCursorVisible();

            // Auto-height wrapping box: the wrap width is only known after arranging, so a changed
            // soft-line count needs another measure.
            if (!Height.IsFixed() && GetWrapWidth().IsFixed())
            {
                var lineCount = GetLineMetricsCache().Lines.Count;
                if (lineCount != _measuredLineCount)
                {
                    _measuredLineCount = lineCount;
                    InvalidateLayout(true);
                }
            }
        }

        private int _measuredLineCount = -1;

        private void OnClick(object? sender, EventArgs eventArgs)
        {
            Focus();
        }

        private void OnMouseDown(object? sender, MouseEventArgs args)
        {
            if (!args.Buttons.Contains(MouseButton.Left))
                return;

            _isPointerSelecting = true;
            SetCursorFromPosition(args.Position, false);
        }

        private void OnMouseMove(object? sender, MouseEventArgs args)
        {
            if (!_isPointerSelecting || !args.Buttons.Contains(MouseButton.Left))
                return;

            SetCursorFromPosition(args.Position, true);
        }

        private void OnMouseUp(object? sender, MouseEventArgs args)
        {
            if (args.Buttons.Contains(MouseButton.Left))
                SetCursorFromPosition(args.Position, _isPointerSelecting);

            _isPointerSelecting = false;
        }

        private void OnTouchDown(object? sender, TouchEventArgs args)
        {
            if (TouchSelection)
            {
                BeginTouchGesture(args.Position);
                return;
            }
            _isPointerSelecting = true;
            SetCursorFromPosition(args.Position, false);
        }

        private void OnTouchMove(object? sender, TouchEventArgs args)
        {
            if (TouchSelection)
            {
                MoveTouchGesture(args.Position);
                return;
            }
            if (_isPointerSelecting)
                SetCursorFromPosition(args.Position, true);
        }

        private void OnTouchUp(object? sender, TouchEventArgs args)
        {
            if (TouchSelection)
            {
                EndTouchGesture(args.Position);
                return;
            }
            SetCursorFromPosition(args.Position, _isPointerSelecting);
            _isPointerSelecting = false;
            ShowKeyboardAgain();
        }

        private void OnTouchCancel(object? sender, TouchEventArgs args)
        {
            _touchGesture = TouchGesture.None;
            _isPointerSelecting = false;
        }

        #region Touch selection

        private enum TouchGesture
        {
            None,
            Pressed,
            Moved,
            LongPressed,
            DragSelecting
        }

        /// <summary>Android's long press timeout.</summary>
        internal static readonly TimeSpan LongPressDuration = TimeSpan.FromMilliseconds(400);

        /// <summary>Android's double tap timeout.</summary>
        internal static readonly TimeSpan DoubleTapTimeout = TimeSpan.FromMilliseconds(300);

        // Android's touch slop (8 dp): a finger that moved farther is no tap and no long press.
        private const float TouchSlop = 8;

        private TouchGesture _touchGesture;
        private PointF _touchDownPoint;
        private TimeSpan _touchDownAt;
        private TimeSpan? _lastTapAt;
        private PointF _lastTapPoint;
        private int _longPressWordStart;
        private int _longPressWordEnd;

        private bool TouchSelection => (Screen?.ScreenEngine ?? ScreenEngine.Instance)?.Options.TouchTextSelection ?? true;

        private TextSelectionLayer? SelectionLayer => Screen?.TextSelection;

        private void BeginTouchGesture(PointF point)
        {
            _touchGesture = TouchGesture.Pressed;
            _touchDownPoint = point;
            _touchDownAt = ScreenSystem.TotalTime;
        }

        private void MoveTouchGesture(PointF point)
        {
            switch (_touchGesture)
            {
                case TouchGesture.Pressed when Distance(point, _touchDownPoint) > TouchSlop:
                    _touchGesture = TouchGesture.Moved;
                    break;
                case TouchGesture.LongPressed when Distance(point, _touchDownPoint) > TouchSlop:
                case TouchGesture.DragSelecting:
                    if (_longPressWordEnd <= _longPressWordStart)
                        break;
                    // After a long press the finger drags the selection, which never shrinks below the word.
                    _touchGesture = TouchGesture.DragSelecting;
                    SelectionLayer?.HideToolbar();
                    var position = GetPositionFromPoint(point);
                    if (position >= _longPressWordStart)
                        SetSelectionEnds(_longPressWordStart, Math.Max(position, _longPressWordEnd));
                    else
                        SetSelectionEnds(_longPressWordEnd, position);
                    break;
            }
        }

        private void EndTouchGesture(PointF point)
        {
            var gesture = _touchGesture;
            _touchGesture = TouchGesture.None;
            if (gesture is TouchGesture.LongPressed or TouchGesture.DragSelecting)
            {
                // The bar appears when the finger lifts.
                if (HasSelection)
                    SelectionLayer?.ShowSelection(this, toolbar: true);
                return;
            }
            if (gesture != TouchGesture.Pressed)
                return;

            var now = ScreenSystem.TotalTime;
            if (_lastTapAt is { } lastTap && now - lastTap <= DoubleTapTimeout && Distance(point, _lastTapPoint) <= TouchSlop * 4)
            {
                _lastTapAt = null;
                if (SelectWordAt(point))
                {
                    SelectionLayer?.ShowSelection(this, toolbar: true);
                    return;
                }
            }
            _lastTapAt = now;
            _lastTapPoint = point;

            var layer = SelectionLayer;
            var position = GetPositionFromPoint(point);
            // Tapping the caret again (or an empty box) toggles Paste / Select all.
            var onCaret = !HasSelection && position == CursorPosition && IsFocused
                && (layer?.InsertionHandleShown == true || Text.Length == 0);
            MoveCursorTo(position, false);
            layer?.ShowInsertion(this, toolbar: onCaret && !layer.ToolbarShown);
            ShowKeyboardAgain();
        }

        internal override void OnFrameUpdate()
        {
            base.OnFrameUpdate();
            UpdateFloatingHint();
            SyncInputMethod();
            if (_touchGesture == TouchGesture.Pressed && TouchState == TouchStates.Touched
                && ScreenSystem.TotalTime - _touchDownAt >= LongPressDuration && TouchSelection)
                OnLongPress();
        }

        #region Input method (OnScreenKeyboardRequest.Editor)

        private ImeTarget? _imeTarget;
        // The word an input method is still composing (underlined), -1 when none.
        private int _composingStart = -1;
        private int _composingEnd = -1;
        // What the input method last knew of this field.
        private TextEditState _imeSynced;

        internal TextEditState EditState => new TextEditState(Text, SelectionStart, SelectionStart + SelectionLength, _composingStart, _composingEnd);

        /// <summary>The input method edited the text: taken over like typing (undoable, TextChanged).</summary>
        internal void ApplyImeEdit(TextEditState state)
        {
            var current = Text;
            var target = state.Text;
            if (current != target)
            {
                // Only the changed middle is replaced, so undo steps and the caret behave as for typing.
                var prefix = 0;
                var max = Math.Min(current.Length, target.Length);
                while (prefix < max && current[prefix] == target[prefix])
                    prefix++;
                var suffix = 0;
                while (suffix < max - prefix && current[current.Length - 1 - suffix] == target[target.Length - 1 - suffix])
                    suffix++;
                Screen?.ExistingTextSelection?.Hide(this);
                ReplaceRange(prefix, current.Length - prefix - suffix, target.Substring(prefix, target.Length - prefix - suffix));
            }
            _selectionAnchor = ClampTextPosition(state.SelectionStart);
            _cursorPosition = ClampTextPosition(state.SelectionEnd);
            _composingStart = state.ComposingStart < 0 ? -1 : ClampTextPosition(state.ComposingStart);
            _composingEnd = state.ComposingEnd < 0 ? -1 : ClampTextPosition(state.ComposingEnd);
            ResetDesiredCursorX();
            EnsureCursorVisible();
            // A field that refused part of it (MaxLength, a line break in one line) is told back on the next frame.
            _imeSynced = state;
            RequestRedraw();
        }

        // Changes that did not come from the input method (a tap moved the caret, paste, code) are handed to it.
        private void SyncInputMethod()
        {
            if (!IsFocused || _imeTarget == null || (Screen?.ScreenEngine ?? ScreenEngine.Instance)?.OnScreenKeyboard is not ITextEditKeyboard keyboard)
                return;
            var state = EditState;
            if (state.Equals(_imeSynced))
                return;
            if (state.Text != _imeSynced.Text || state.SelectionStart != _imeSynced.SelectionStart || state.SelectionEnd != _imeSynced.SelectionEnd)
            {
                // Edited here: the keyboard's word in progress is over.
                _composingStart = _composingEnd = -1;
                state = EditState;
            }
            _imeSynced = state;
            keyboard.TargetChanged(state);
        }

        private readonly SolidColorBrush _composingBrush = new SolidColorBrush(Color.White);

        private void DrawComposingUnderline(SpriteBatch spriteBatch, Rect textRect)
        {
            if (_composingStart < 0 || _composingEnd <= _composingStart || !IsFocused)
                return;
            var lines = GetLineMetricsCache().Lines;
            var lineIndex = GetLineIndexFromPosition(_composingStart, lines);
            if (GetLineIndexFromPosition(_composingEnd, lines) != lineIndex)
                return;
            var line = lines[lineIndex];
            var lineHeight = GetLineHeight() * RenderScale.Y;
            var top = GetLineTop(textRect, lineHeight, lineIndex);
            if (!IsLineVisible(textRect, top, lineHeight))
                return;
            // Same transform as text and caret.
            var left = Math.Max(textRect.Left, textRect.Left + (GetCursorX(_composingStart, line) - _horizontalScrollOffset) * RenderScale.X);
            var right = Math.Min(textRect.Right, textRect.Left + (GetCursorX(_composingEnd, line) - _horizontalScrollOffset) * RenderScale.X);
            var thickness = Math.Max(1, MathF.Round(1.5f * RenderScale.Y));
            _composingBrush.Color = TextColor * 0.8f;
            _composingBrush.Draw(spriteBatch, new Rect(left, top + lineHeight - thickness, Math.Max(0, right - left), thickness), RenderOpacity);
        }

        private sealed class ImeTarget : ITextEditTarget
        {
            private readonly TextBox _box;

            public ImeTarget(TextBox box) => _box = box;

            public TextEditState State => _box.EditState;

            public void Apply(TextEditState state) => _box.ApplyImeEdit(state);

            // Enter as from a keyboard; in one line it also closes the keyboard (Android's Done), the field keeps focus.
            public void Submit()
            {
                var engine = _box.Screen?.ScreenEngine ?? ScreenEngine.Instance;
                // Straight to the field: through the screen, a key command would switch on keyboard navigation (focus ring).
                _box.HandleKeyPressed(null, new KeyEventArgs(KeyboardCommand.Enter));
                if (!_box.IsMultiline && _box.IsFocused)
                    engine?.OnScreenKeyboard.Hide();
            }
        }

        #endregion

        private void UpdateFloatingHint()
        {
            var now = ScreenSystem.TotalTime;
            var first = _hintTime == TimeSpan.Zero;
            var dt = (float)Math.Clamp((now - _hintTime).TotalSeconds, 0, 0.1);
            _hintTime = now;
            if (!FloatingHint)
                return;
            var lift = IsFocused || Text.Length > 0 ? 1f : 0f;
            var focus = IsFocused ? 1f : 0f;
            if (first)
            {
                // A box that opens with text shows its label raised from the start.
                _hintLift = lift;
                _hintFocus = focus;
            }
            if (_hintLift == lift && _hintFocus == focus)
                return;
            // About 200 ms to settle, like Material's label.
            var step = 1 - MathF.Exp(-dt * 22);
            _hintLift = Math.Abs(lift - _hintLift) < 0.002f ? lift : _hintLift + (lift - _hintLift) * step;
            _hintFocus = Math.Abs(focus - _hintFocus) < 0.002f ? focus : _hintFocus + (focus - _hintFocus) * step;
            RequestRedraw();
        }

        private void DrawFloatingHint(SpriteBatch spriteBatch, Rect rect, Rect textRect)
        {
            var font = ActiveDynamicFont;
            if (font == null)
                return;
            // Drawn before its first update: already where it belongs.
            if (_hintTime == TimeSpan.Zero)
                _hintLift = IsFocused || Text.Length > 0 ? 1f : 0f;
            // Eased so the label speeds off and lands softly.
            var t = 1 - (1 - _hintLift) * (1 - _hintLift);
            var scale = 1 + (FloatingHintScale - 1) * t;
            var measured = MeasureText(HintText);
            var height = measured.Y * RenderScale.Y;
            // Resting: in the middle of the box (level with an icon beside it), like Material. Raised: centred in
            // the room above the text.
            var restY = IsMultiline ? textRect.Top : rect.Top + (rect.Height - height) / 2;
            var room = textRect.Top - rect.Top;
            var raisedY = rect.Top + Math.Max(0, (room - height * FloatingHintScale) / 2) + room * 0.08f;
            var position = new Vector2(textRect.Left, restY + (raisedY - restY) * t);
            if (SnapsDrawing)
                position = position.ToInts();
            var color = Color.Lerp(HintTextColor, FocusedHintColor ?? HintTextColor, _hintFocus);
            font.DrawString(spriteBatch, HintText, position, Brush.ApplyOpacity(color, RenderOpacity), DynamicPixelSize(font), RenderScale * scale);
        }

        private void OnLongPress()
        {
            _touchGesture = TouchGesture.LongPressed;
            // The finger now belongs to the selection: scroll viewers around must not take the drag.
            if (Screen is { } screen)
                screen.TouchScrollOwner = this;
            Focus();
            PlaySound(Audio.UiSound.Select);
            _longPressWordStart = _longPressWordEnd = 0;
            if (!PasswordChar.HasValue && SelectWordAt(_touchDownPoint))
            {
                _longPressWordStart = SelectionStart;
                _longPressWordEnd = SelectionStart + SelectionLength;
                SelectionLayer?.ShowSelection(this, toolbar: false);
                return;
            }
            // Nothing to select (empty box, a space): the caret goes there with Paste / Select all.
            MoveCursorTo(GetPositionFromPoint(_touchDownPoint), false);
            SelectionLayer?.ShowInsertion(this, toolbar: true);
        }

        private bool SelectWordAt(PointF point)
        {
            var (start, end) = WordAt(GetPositionFromPoint(point));
            if (end <= start)
                return false;
            Focus();
            SetSelectionEnds(start, end);
            return true;
        }

        /// <summary>The word (letters and digits, or a run of punctuation) at <paramref name="position"/>; empty on white space.</summary>
        internal (int Start, int End) WordAt(int position)
        {
            var text = Text;
            var i = ClampTextPosition(position);
            // A position just after a word belongs to it (the caret lands there when a word's right half is touched).
            if ((i == text.Length || CharClass(text[i]) == 0) && i > 0 && CharClass(text[i - 1]) != 0)
                i--;
            if (i >= text.Length || CharClass(text[i]) == 0)
                return (i, i);
            var wordClass = CharClass(text[i]);
            var start = i;
            while (start > 0 && CharClass(text[start - 1]) == wordClass)
                start--;
            var end = i;
            while (end < text.Length && CharClass(text[end]) == wordClass)
                end++;
            return (start, end);
        }

        /// <summary>Selects from <paramref name="anchor"/> to <paramref name="cursor"/>; the caret end is kept in view.</summary>
        internal void SetSelectionEnds(int anchor, int cursor)
        {
            _selectionAnchor = ClampTextPosition(anchor);
            _cursorPosition = ClampTextPosition(cursor);
            ResetDesiredCursorX();
            EnsureCursorVisible();
            InvalidateLayout(false);
        }

        internal void CollapseSelectionToEnd() => SetSelectionEnds(SelectionStart + SelectionLength, SelectionStart + SelectionLength);

        internal int GetTextPositionAt(PointF point) => GetPositionFromPoint(point);

        internal bool ClipboardHasText() => !string.IsNullOrEmpty(TryGetClipboardText());

        /// <summary>
        ///     Where the caret would stand at <paramref name="position"/>: the bottom of its line (layout units, screen
        ///     space) and the line height. False when that spot is scrolled out of the box.
        /// </summary>
        internal bool TryGetCaretPoint(int position, out PointF bottom, out float lineHeight)
        {
            var textRect = GetTextRect();
            var lines = GetLineMetricsCache().Lines;
            var lineIndex = GetLineIndexFromPosition(position, lines);
            lineHeight = GetLineHeight();
            var top = GetTextTop(textRect, lineHeight) + lineIndex * lineHeight - (IsMultiline ? _verticalScrollOffset : 0);
            var x = textRect.Left + GetCursorX(ClampTextPosition(position), lines[lineIndex]) - _horizontalScrollOffset;
            bottom = new PointF(x, top + lineHeight);
            const float tolerance = 0.5f;
            return x >= textRect.Left - tolerance && x <= textRect.Right + tolerance
                && top >= textRect.Top - lineHeight * 0.5f && top + lineHeight <= textRect.Bottom + lineHeight * 0.5f;
        }

        private static float Distance(PointF a, PointF b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        #endregion

        protected internal override void OnGotFocus(GotFocusEventArgs args)
        {
            base.OnGotFocus(args);
            ScreenEngine.Instance?.RequestKeyboard(InputScope);
            AttachKeyboard(ScreenEngine.Instance?.CurrentKeyboard);
            ShowOnScreenKeyboard();
        }

        // The focused field tapped again: bring back a keyboard closed meanwhile (Android back).
        private void ShowKeyboardAgain()
        {
            // Android's IsVisible only knows what was asked for; the inset tells whether the system closed it.
            if (IsFocused && (Screen?.ScreenEngine ?? ScreenEngine.Instance) is { } engine
                && (!engine.OnScreenKeyboard.IsVisible || engine.KeyboardInset <= 0))
                ShowOnScreenKeyboard();
        }

        private void ShowOnScreenKeyboard()
        {
            if (!IsReadOnly && (Screen?.ScreenEngine ?? ScreenEngine.Instance) is { } engine)
            {
                engine.OnScreenKeyboard.Show(new OnScreenKeyboardRequest
                {
                    Purpose = PasswordChar != default ? TextInputPurpose.Password : InputPurpose,
                    Text = PasswordChar != default ? "" : Text,
                    Hint = HintText,
                    IsMultiline = IsMultiline,
                    MaxLength = MaxLength,
                    FieldBounds = ClippingRect,
                    Commit = text => Text = text,
                    Editor = _imeTarget ??= new ImeTarget(this)
                });
                _composingStart = _composingEnd = -1;
                _imeSynced = EditState;
            }
        }

        protected internal override void OnLostFocus(LostFocusEventArgs args)
        {
            base.OnLostFocus(args);
            DetachKeyboard();
            ScreenEngine.Instance?.HideKeyboard();
            // Focus moving to another text field shows the keyboard again right away.
            (Screen?.ScreenEngine ?? ScreenEngine.Instance)?.OnScreenKeyboard.Hide();
            _isPointerSelecting = false;
            Screen?.ExistingTextSelection?.Hide(this);
        }

        protected internal virtual void HandleKeyPressed(object? sender, KeyEventArgs args)
        {
            var before = Text;
            // Typing ends the touch selection UI (the selection itself is replaced or kept by the key).
            Screen?.ExistingTextSelection?.Hide(this);
            DispatchKeyPressed(args);
            if (!ReferenceEquals(before, Text) && before != Text)
                PlaySound(Audio.UiSound.Type);
            // A typed character that did not fit: the box is full.
            else if (args.InputType == InputType.Char && !IsReadOnly && MaxLength > 0 && Text.Length >= MaxLength && !char.IsControl(args.Char))
                PlaySound(Audio.UiSound.Error);
        }

        private void DispatchKeyPressed(KeyEventArgs args)
        {
            switch (args.InputType)
            {
                case InputType.Char:
                    HandleCharPressed(args.Char);
                    break;
                case InputType.Command:
                    if (args.Modifiers == KeyboardModifiers.None)
                        HandleCommandPressed(args.Command);
                    else
                        HandleCommandPressed(args.Command, args.Modifiers);
                    break;
                case InputType.Function:
                    HandleFunctionPressed(args.Function ?? "");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected internal virtual void HandleCharPressed(char c)
        {
            InsertText(c.ToString());
        }

        protected internal virtual void HandleFunctionPressed(string function)
        {
        }

        protected internal virtual void HandleCommandPressed(KeyboardCommand command)
        {
            HandleCommandPressed(command, KeyboardModifiers.None);
        }

        protected internal virtual void HandleCommandPressed(KeyboardCommand command, KeyboardModifiers modifiers)
        {
            var shift = (modifiers & KeyboardModifiers.Shift) != 0;
            var control = (modifiers & KeyboardModifiers.Control) != 0;

            switch (command)
            {
                case KeyboardCommand.Backspace:
                    if (control && !HasSelection)
                        DeleteWordBackward();
                    else
                        Backspace();
                    break;
                case KeyboardCommand.Insert:
                    IsOverwriteMode = !IsOverwriteMode;
                    break;
                case KeyboardCommand.Delete:
                    if (control && !HasSelection)
                        DeleteWordForward();
                    else
                        Delete();
                    break;
                case KeyboardCommand.Undo:
                    Undo();
                    break;
                case KeyboardCommand.Redo:
                    Redo();
                    break;
                case KeyboardCommand.Enter:
                    if (IsMultiline && !control)
                        InsertText("\n");
                    else
                        EnterPressed?.Invoke(this, EventArgs.Empty);
                    break;
                case KeyboardCommand.CursorLeft when control:
                    MoveCursorTo(PreviousWordStart(CursorPosition), shift);
                    break;
                case KeyboardCommand.CursorRight when control:
                    MoveCursorTo(NextWordStart(CursorPosition), shift);
                    break;
                case KeyboardCommand.CursorLeft:
                    // Without Shift an existing selection collapses to its edge (standard editors).
                    if (!shift && HasSelection)
                        MoveCursorTo(SelectionStart, false);
                    else
                        MoveCursorTo(PreviousCaretStop(CursorPosition), shift);
                    break;
                case KeyboardCommand.CursorRight:
                    if (!shift && HasSelection)
                        MoveCursorTo(SelectionStart + SelectionLength, false);
                    else
                        MoveCursorTo(NextCaretStop(CursorPosition), shift);
                    break;
                case KeyboardCommand.CursorUp:
                    MoveCursorVertically(-1, shift);
                    break;
                case KeyboardCommand.CursorDown:
                    MoveCursorVertically(1, shift);
                    break;
                case KeyboardCommand.Home:
                    MoveCursorTo(control ? 0 : GetCurrentLine().Start, shift);
                    break;
                case KeyboardCommand.End:
                    var line = GetCurrentLine();
                    MoveCursorTo(control ? Text.Length : line.Start + line.Length, shift);
                    break;
                case KeyboardCommand.SelectAll:
                    SelectAll();
                    break;
                case KeyboardCommand.Copy:
                    Copy();
                    break;
                case KeyboardCommand.Cut:
                    Cut();
                    break;
                case KeyboardCommand.Paste:
                    Paste();
                    break;
                default:
                    // Commands a text box has no use for (PageUp/PageDown, ...) are ignored.
                    break;
            }
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            EnsureCursorVisible();
            // Theme chrome (corner radius, border, inset shadows), not just the fill.
            DrawChrome(spriteBatch, rect);
            // rect is in render space; scale the layout padding with it (LayoutScale, popup zoom).
            var textRect = rect - new Thickness(Padding.Left * RenderScale.X, Padding.Top * RenderScale.Y, Padding.Right * RenderScale.X, Padding.Bottom * RenderScale.Y);

            if (FloatingHint && !string.IsNullOrEmpty(HintText))
                DrawFloatingHint(spriteBatch, rect, textRect);
            else if (Text.Length == 0 && !string.IsNullOrEmpty(HintText) && HasDrawableFont)
            {
                var measuredHint = MeasureText(HintText);
                var scaledHint = new Vector2(measuredHint.X * RenderScale.X, measuredHint.Y * RenderScale.Y);
                var offset = textRect.Offset;
                if (!IsMultiline)
                    offset.Y += (textRect.Height - scaledHint.Y) / 2;
                if (SnapsDrawing)
                    offset = offset.ToInts();
                DrawText(spriteBatch, HintText, offset, Brush.ApplyOpacity(HintTextColor, RenderOpacity));
            }

            DrawSelection(spriteBatch, textRect);
            DrawText(spriteBatch, textRect);
            DrawComposingUnderline(spriteBatch, textRect);
            DrawCursor(spriteBatch, textRect);
        }

        protected virtual void OnTextChanged(TextChangedEventArgs args)
        {
            TextChanged?.Invoke(this, args);
        }

        private void AttachKeyboard(IKeyboard? keyboard)
        {
            if (_attachedKeyboard == keyboard)
                return;
            DetachKeyboard();
            _attachedKeyboard = keyboard;
            if (_attachedKeyboard != null)
                _attachedKeyboard.KeyPressed += HandleKeyPressed;
        }

        private void DetachKeyboard()
        {
            if (_attachedKeyboard != null)
                _attachedKeyboard.KeyPressed -= HandleKeyPressed;
            _attachedKeyboard = null;
        }

        private void InsertText(string text)
        {
            if (IsReadOnly)
                return;

            // Overwrite mode: a typed character replaces the one at the caret (not a line break).
            if (IsOverwriteMode && !HasSelection && text != "\n" && CursorPosition < Text.Length && Text[CursorPosition] != '\n')
            {
                var next = NextCaretStop(CursorPosition);
                ReplaceRange(CursorPosition, next - CursorPosition, text);
                return;
            }

            ReplaceSelection(text);
        }

        private void Backspace()
        {
            if (IsReadOnly)
                return;

            if (HasSelection)
            {
                DeleteSelection();
                return;
            }

            if (CursorPosition > 0)
            {
                var start = PreviousCaretStop(CursorPosition);
                ReplaceRange(start, CursorPosition - start, "");
            }
        }

        // Caret stops never fall between the two halves of a surrogate pair (emoji, rare CJK),
        // so arrows and deletion treat such a character as one unit.
        private int PreviousCaretStop(int position)
        {
            if (position <= 0)
                return 0;
            var previous = position - 1;
            if (previous > 0 && char.IsLowSurrogate(Text[previous]) && char.IsHighSurrogate(Text[previous - 1]))
                previous--;
            return previous;
        }

        private int NextCaretStop(int position)
        {
            if (position >= Text.Length)
                return Text.Length;
            var next = position + 1;
            if (next < Text.Length && char.IsLowSurrogate(Text[next]) && char.IsHighSurrogate(Text[next - 1]))
                next++;
            return next;
        }

        private void Delete()
        {
            if (IsReadOnly)
                return;

            if (HasSelection)
            {
                DeleteSelection();
                return;
            }

            if (CursorPosition < Text.Length)
                ReplaceRange(CursorPosition, NextCaretStop(CursorPosition) - CursorPosition, "");
        }

        private void DeleteSelection()
        {
            if (HasSelection)
                ReplaceRange(SelectionStart, SelectionLength, "");
        }

        private void ReplaceSelection(string replacement)
        {
            ReplaceRange(SelectionStart, SelectionLength, replacement);
        }

        private void ReplaceRange(int start, int length, string replacement)
        {
            var rangeStart = ClampTextPosition(start);
            var rangeEnd = ClampTextPosition(rangeStart + Math.Max(0, length));
            var normalizedReplacement = NormalizeText(replacement);
            var availableLength = MaxLength == 0 ? normalizedReplacement.Length : Math.Max(0, MaxLength - (Text.Length - (rangeEnd - rangeStart)));
            if (MaxLength > 0 && normalizedReplacement.Length > availableLength)
                normalizedReplacement = normalizedReplacement.Substring(0, availableLength);

            var newText = Text.Substring(0, rangeStart) + normalizedReplacement + Text.Substring(rangeEnd);
            var newCursorPosition = rangeStart + normalizedReplacement.Length;

            if (base.Text != newText)
            {
                RecordUndo(rangeStart, rangeEnd - rangeStart, normalizedReplacement);
                var oldText = base.Text;
                base.Text = newText;
                InvalidateLineMetrics();
                OnTextChanged(new TextChangedEventArgs(newText, oldText));
            }

            _cursorPosition = ClampTextPosition(newCursorPosition);
            _selectionAnchor = _cursorPosition;
            ResetDesiredCursorX();
            EnsureCursorVisible();
        }

        // ---- Undo / redo -------------------------------------------------------------------
        // Snapshots of (text, caret, anchor) before each user edit. Consecutive typed characters
        // merge into one step, like common editors; setting Text in code clears the history.

        private readonly struct EditSnapshot
        {
            public EditSnapshot(string text, int cursor, int anchor)
            {
                Text = text;
                Cursor = cursor;
                Anchor = anchor;
            }

            public string Text { get; }
            public int Cursor { get; }
            public int Anchor { get; }
        }

        private const int MaxUndoSteps = 100;
        private readonly List<EditSnapshot> _undoStack = new List<EditSnapshot>();
        private readonly List<EditSnapshot> _redoStack = new List<EditSnapshot>();
        private bool _lastEditWasTyping;
        private int _lastTypingEnd = -1;

        public bool CanUndo => _undoStack.Count > 0;

        public bool CanRedo => _redoStack.Count > 0;

        private void RecordUndo(int start, int removedLength, string inserted)
        {
            var isTyping = removedLength == 0 && inserted.Length == 1 && !char.IsWhiteSpace(inserted[0]);
            var continuesTyping = isTyping && _lastEditWasTyping && start == _lastTypingEnd;
            if (!continuesTyping)
            {
                _undoStack.Add(new EditSnapshot(base.Text, _cursorPosition, _selectionAnchor));
                if (_undoStack.Count > MaxUndoSteps)
                    _undoStack.RemoveAt(0);
            }

            _redoStack.Clear();
            _lastEditWasTyping = isTyping;
            _lastTypingEnd = start + inserted.Length;
        }

        private void ClearUndoHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _lastEditWasTyping = false;
        }

        public void Undo()
        {
            if (IsReadOnly || _undoStack.Count == 0)
                return;
            _redoStack.Add(new EditSnapshot(base.Text, _cursorPosition, _selectionAnchor));
            var snapshot = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            RestoreSnapshot(snapshot);
        }

        public void Redo()
        {
            if (IsReadOnly || _redoStack.Count == 0)
                return;
            _undoStack.Add(new EditSnapshot(base.Text, _cursorPosition, _selectionAnchor));
            var snapshot = _redoStack[_redoStack.Count - 1];
            _redoStack.RemoveAt(_redoStack.Count - 1);
            RestoreSnapshot(snapshot);
        }

        private void RestoreSnapshot(EditSnapshot snapshot)
        {
            var oldText = base.Text;
            base.Text = snapshot.Text;
            InvalidateLineMetrics();
            _cursorPosition = ClampTextPosition(snapshot.Cursor);
            _selectionAnchor = ClampTextPosition(snapshot.Anchor);
            _lastEditWasTyping = false;
            ResetDesiredCursorX();
            EnsureCursorVisible();
            if (oldText != snapshot.Text)
                OnTextChanged(new TextChangedEventArgs(snapshot.Text, oldText));
        }

        // ---- Word navigation ----------------------------------------------------------------

        private static int CharClass(char c) => char.IsWhiteSpace(c) ? 0 : char.IsLetterOrDigit(c) || c == '_' ? 1 : 2;

        /// <summary>Start of the word left of <paramref name="position"/> (Ctrl+Left).</summary>
        internal int PreviousWordStart(int position)
        {
            var i = ClampTextPosition(position);
            while (i > 0 && CharClass(Text[i - 1]) == 0)
                i--;
            if (i == 0)
                return 0;
            var wordClass = CharClass(Text[i - 1]);
            while (i > 0 && CharClass(Text[i - 1]) == wordClass)
                i--;
            return i;
        }

        /// <summary>Start of the next word right of <paramref name="position"/> (Ctrl+Right).</summary>
        internal int NextWordStart(int position)
        {
            var i = ClampTextPosition(position);
            if (i < Text.Length && CharClass(Text[i]) != 0)
            {
                var wordClass = CharClass(Text[i]);
                while (i < Text.Length && CharClass(Text[i]) == wordClass)
                    i++;
            }
            while (i < Text.Length && CharClass(Text[i]) == 0 && Text[i] != '\n')
                i++;
            return i;
        }

        private void DeleteWordBackward()
        {
            if (IsReadOnly || CursorPosition == 0)
                return;
            var start = PreviousWordStart(CursorPosition);
            ReplaceRange(start, CursorPosition - start, "");
        }

        private void DeleteWordForward()
        {
            if (IsReadOnly || CursorPosition >= Text.Length)
                return;
            var end = NextWordStart(CursorPosition);
            ReplaceRange(CursorPosition, end - CursorPosition, "");
        }

        private void MoveCursorTo(int position, bool extendSelection)
        {
            _cursorPosition = ClampTextPosition(position);
            if (!extendSelection)
                _selectionAnchor = _cursorPosition;
            ResetDesiredCursorX();
            EnsureCursorVisible();
        }

        private void MoveCursorVertically(int direction, bool extendSelection)
        {
            if (!IsMultiline)
                return;

            var lines = GetLineMetricsCache().Lines;
            if (lines.Count <= 1)
                return;

            var currentLineIndex = GetLineIndexFromPosition(CursorPosition, lines);
            var targetLineIndex = Math.Max(0, Math.Min(lines.Count - 1, currentLineIndex + direction));
            if (targetLineIndex == currentLineIndex)
                return;

            var desiredX = _desiredCursorX ?? GetCursorX(CursorPosition, lines[currentLineIndex]);
            _desiredCursorX = desiredX;
            var targetPosition = GetPositionForX(lines[targetLineIndex], desiredX);
            _cursorPosition = ClampTextPosition(targetPosition);
            if (!extendSelection)
                _selectionAnchor = _cursorPosition;
            EnsureCursorVisible();
        }

        private void SetCursorFromPosition(PointF position, bool extendSelection)
        {
            MoveCursorTo(GetPositionFromPoint(position), extendSelection);
        }

        private int GetPositionFromPoint(PointF position)
        {
            var textRect = GetTextRect();
            var lines = GetLineMetricsCache().Lines;
            var lineHeight = GetLineHeight();
            var lineIndex = 0;

            if (IsMultiline)
            {
                var y = Math.Max(0, position.Y - textRect.Top + _verticalScrollOffset);
                lineIndex = Math.Max(0, Math.Min(lines.Count - 1, (int)(y / lineHeight)));
            }

            var x = Math.Max(0, position.X - textRect.Left + _horizontalScrollOffset);
            return GetPositionForX(lines[lineIndex], x);
        }

        private TextLine GetCurrentLine()
        {
            var lines = GetLineMetricsCache().Lines;
            return lines[GetLineIndexFromPosition(CursorPosition, lines)];
        }

        private float GetCursorX(int position, TextLine line)
        {
            var offsetInLine = Math.Max(0, Math.Min(line.Length, position - line.Start));
            return GetLineMetric(line).GetWidth(offsetInLine);
        }

        private int GetPositionForX(TextLine line, float x)
        {
            var lineMetric = GetLineMetric(line);
            var closestIndex = 0;
            var closestDistance = float.MaxValue;

            for (var i = 0; i <= line.Length; i++)
            {
                var measured = lineMetric.GetWidth(i);
                var distance = Math.Abs(measured - x);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestIndex = i;
            }

            return line.Start + closestIndex;
        }

        private int GetLineIndexFromPosition(int position, List<TextLine> lines)
        {
            var textPosition = ClampTextPosition(position);
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (textPosition <= line.Start + line.Length)
                    return i;
            }

            return lines.Count - 1;
        }

        private void DrawSelection(SpriteBatch spriteBatch, Rect textRect)
        {
            if (!HasSelection)
                return;

            var cache = GetLineMetricsCache();
            var lines = cache.Lines;
            var lineHeight = GetLineHeight() * RenderScale.Y;
            var selectionStart = SelectionStart;
            var selectionEnd = selectionStart + SelectionLength;

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var lineStart = line.Start;
                var lineEnd = line.Start + line.Length;
                var rangeStart = Math.Max(selectionStart, lineStart);
                var rangeEnd = Math.Min(selectionEnd, lineEnd);

                if (rangeStart >= rangeEnd)
                    continue;

                var top = GetLineTop(textRect, lineHeight, i);
                if (!IsLineVisible(textRect, top, lineHeight))
                    continue;

                var lineMetric = GetLineMetric(line);
                var selectionLeft = lineMetric.GetWidth(rangeStart - line.Start);
                var selectionRight = lineMetric.GetWidth(rangeEnd - line.Start);
                var rawLeft = textRect.Left + (selectionLeft - _horizontalScrollOffset) * RenderScale.X;
                var rawRight = rawLeft + Math.Max(1, (selectionRight - selectionLeft) * RenderScale.X);
                var left = Math.Max(textRect.Left, rawLeft);
                var right = Math.Min(textRect.Right, rawRight);
                if (right <= left)
                    continue;

                SelectionBrush.Draw(spriteBatch, new Rect(left, top, right - left, lineHeight), RenderOpacity);
            }
        }

        // Reused per visible line so drawing does not allocate a substring each frame.
        private readonly System.Text.StringBuilder _drawBuffer = new System.Text.StringBuilder();

        private void DrawText(SpriteBatch spriteBatch, Rect textRect)
        {
            if (!HasDrawableFont || Text.Length == 0)
                return;

            var cache = GetLineMetricsCache();
            var lines = cache.Lines;
            var displayText = cache.DisplayText;
            var lineHeight = GetLineHeight() * RenderScale.Y;
            var color = Brush.ApplyOpacity(DrawnTextColor, RenderOpacity);
            var selectedColor = HasSelection ? GetSelectedTextColor() : null;
            var selectionStart = SelectionStart;
            var selectionEnd = selectionStart + SelectionLength;

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var lineTop = GetLineTop(textRect, lineHeight, i);
                if (!IsLineVisible(textRect, lineTop, lineHeight))
                    continue;

                var visibleRange = GetVisibleTextRange(line, textRect.Width / Math.Max(0.001f, RenderScale.X));
                if (visibleRange.Length <= 0)
                    continue;

                var start = line.Start + visibleRange.Start;
                var end = start + visibleRange.Length;
                var metric = GetLineMetric(line);
                // The run starts where the whole line would (snapped once); later runs are placed by
                // their unsnapped width from it, so glyphs land exactly where an undivided line puts them.
                var origin = new PointF(textRect.Left + (metric.GetWidth(visibleRange.Start) - _horizontalScrollOffset) * RenderScale.X, lineTop);
                if (SnapsDrawing)
                    origin = origin.ToInts();
                if (selectedColor is not { } selected || selectionEnd <= start || selectionStart >= end)
                {
                    DrawTextSegment(spriteBatch, metric, line, displayText, start, start, end, origin, color);
                    continue;
                }

                // Selected glyphs in their own colour: up to three runs, so nothing is drawn twice
                // (no halo of the normal colour).
                var selectedStart = Math.Max(start, selectionStart);
                var selectedEnd = Math.Min(end, selectionEnd);
                DrawTextSegment(spriteBatch, metric, line, displayText, start, start, selectedStart, origin, color);
                DrawTextSegment(spriteBatch, metric, line, displayText, start, selectedStart, selectedEnd, origin, Brush.ApplyOpacity(selected, RenderOpacity));
                DrawTextSegment(spriteBatch, metric, line, displayText, start, selectedEnd, end, origin, color);
            }
        }

        private void DrawTextSegment(SpriteBatch spriteBatch, LineMetric metric, in TextLine line, string displayText, int runStart, int start, int end, PointF origin, Color color)
        {
            if (end <= start)
                return;
            var offset = start == runStart
                ? origin
                : new PointF(origin.X + (metric.GetWidth(start - line.Start) - metric.GetWidth(runStart - line.Start)) * RenderScale.X, origin.Y);
            _drawBuffer.Clear().Append(displayText, start, end - start);
            DrawText(spriteBatch, _drawBuffer, offset, color);
        }

        /// <summary>The colour selected glyphs are drawn in, or null to keep <see cref="TextBlock.TextColor"/>.</summary>
        internal Color? GetSelectedTextColor()
        {
            if (SelectionTextColor is { } explicitColor)
                return explicitColor;
            // A translucent highlight shows the text through it; only an opaque one needs inverting.
            if (SelectionBrush is not SolidColorBrush { Color.A: 255 } highlight)
                return null;
            // Classic inversion: the box's own background colour, when it reads on the highlight.
            if (FaceColor(BackgroundBrush) is { A: 255 } face && Contrast(face, highlight.Color) >= 3)
                return face;
            return PortableTheme.ContrastColor(highlight.Color);
        }

        /// <summary>The colour the text sits on: a solid fill or a chrome brush's face.</summary>
        private static Color? FaceColor(Brush? brush) => brush switch
        {
            SolidColorBrush solid => solid.Color,
            FrameBrush frame => frame.Face,
            BevelBrush bevel => bevel.Face,
            _ => null
        };

        /// <summary>WCAG contrast ratio of two colours (1 = none, 21 = black on white).</summary>
        private static double Contrast(Color a, Color b)
        {
            var la = Luminance(a);
            var lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        private static double Luminance(Color c) => 0.2126 * Linear(c.R) + 0.7152 * Linear(c.G) + 0.0722 * Linear(c.B);

        private static double Linear(byte value)
        {
            var s = value / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        /// <summary>Glyph scale for DrawString. Caret, selection and hit-test metrics come from
        /// MeasureText, which includes <see cref="TextBlock.FontScale"/>, so the glyphs must be
        /// drawn with it too or they drift from the caret whenever TextSize differs from the
        /// font's baked size.</summary>
        internal Vector2 TextDrawScale => RenderScale * FontScale;

        private void DrawCursor(SpriteBatch spriteBatch, Rect textRect)
        {
            if (!IsFocused)
                return;
            // A touch selection shows its handles instead of a caret (Android).
            if (HasSelection && ReferenceEquals(Screen?.ExistingTextSelection?.Owner, this))
                return;

            // The caret blinks (on for one interval, off for one): ask for the frame that flips it.
            var half = Math.Max(1, CaretBlinkInterval.TotalMilliseconds);
            var phase = ScreenSystem.TotalTime.TotalMilliseconds % (2 * half);
            ScreenEngine.RequestAnimationFrameAt(ScreenSystem.TotalTime + TimeSpan.FromMilliseconds(half - phase % half));
            if (phase >= half)
                return;

            var cursorRect = GetCursorRect(textRect);
            if (cursorRect == Rect.Empty)
                return;

            if (CaretStyle == CaretStyle.TextMode)
            {
                // The cell of the character at the caret: a 2-of-16-scanline underline when inserting,
                // the whole cell when overwriting.
                // One character measured without a one-character string (drawn every frame while focused).
                var cellWidth = MeasureCharWidth(CursorPosition < Text.Length && Text[CursorPosition] != '\n' ? Text[CursorPosition] : 'M') * RenderScale.X;
                var cell = new Rect(cursorRect.Left, cursorRect.Top, Math.Max(1, cellWidth), cursorRect.Height);
                if (!IsOverwriteMode)
                {
                    var thickness = Math.Max(1, MathF.Round(cell.Height * 2f / 16f));
                    cell = new Rect(cell.Left, cell.Bottom - thickness, cell.Width, thickness);
                }
                CursorColor.Draw(spriteBatch, cell, RenderOpacity);
                return;
            }

            cursorRect.Width = Math.Max(1, ResolveTheme().TextBoxCaretWidth * RenderScale.X);
            if (SnapsDrawing)
                cursorRect.Width = MathF.Round(cursorRect.Width);
            CursorColor.Draw(spriteBatch, cursorRect, RenderOpacity);
        }

        internal Rect GetCursorRect(Rect textRect)
        {
            var lines = GetLineMetricsCache().Lines;
            var lineIndex = GetLineIndexFromPosition(CursorPosition, lines);
            var line = lines[lineIndex];
            var lineHeight = GetLineHeight() * RenderScale.Y;
            var top = GetLineTop(textRect, lineHeight, lineIndex);
            if (!IsLineVisible(textRect, top, lineHeight))
                return Rect.Empty;

            // Same transform as text and selection, so the caret stays on the glyphs while scaled.
            var x = textRect.Left + (GetCursorX(CursorPosition, line) - _horizontalScrollOffset) * RenderScale.X;
            if (x < textRect.Left || x > textRect.Right)
                return Rect.Empty;

            return new Rect(x, top, 1, lineHeight);
        }

        private float GetTextTop(Rect textRect, float lineHeight)
        {
            if (IsMultiline)
                return textRect.Top;

            return textRect.Top + (textRect.Height - lineHeight) / 2;
        }

        private float GetLineTop(Rect textRect, float lineHeight, int lineIndex)
        {
            var top = GetTextTop(textRect, lineHeight) + lineIndex * lineHeight;
            return IsMultiline ? top - _verticalScrollOffset * RenderScale.Y : top;
        }

        private bool IsLineVisible(Rect textRect, float top, float lineHeight)
        {
            // Overlap test: a line that only partially fits must still draw (the control scissor
            // clips it) — otherwise a TextBox slightly shorter than the font's line height
            // renders no text at all.
            return top + lineHeight >= textRect.Top && top <= textRect.Bottom;
        }

        private float GetLineHeight() => GetLineMetricsCache().LineHeight;

        private string GetDisplayText()
        {
            if (!PasswordChar.HasValue)
                return Text;

            var chars = Text.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] != '\n')
                    chars[i] = PasswordChar.Value;
            }

            return new string(chars);
        }

        private string NormalizeText(string? value)
        {
            var normalized = (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
            return IsMultiline ? normalized : normalized.Replace('\n', ' ');
        }

        private string LimitText(string value)
        {
            if (MaxLength <= 0 || value.Length <= MaxLength)
                return value;

            return value.Substring(0, MaxLength);
        }

        private void ClampSelection()
        {
            _cursorPosition = ClampTextPosition(_cursorPosition);
            _selectionAnchor = ClampTextPosition(_selectionAnchor);
        }

        private int ClampTextPosition(int position)
        {
            return Math.Max(0, Math.Min(Text.Length, position));
        }

        private void ResetDesiredCursorX()
        {
            _desiredCursorX = null;
        }

        private Rect GetTextRect()
        {
            return BoundingRect - Margin - Padding;
        }

        private void EnsureCursorVisible()
        {
            var textRect = GetTextRect();
            if (textRect.Width <= 0 || textRect.Height <= 0)
            {
                _horizontalScrollOffset = 0;
                _verticalScrollOffset = 0;
                return;
            }

            var lines = GetLineMetricsCache().Lines;
            var lineIndex = GetLineIndexFromPosition(CursorPosition, lines);
            var line = lines[lineIndex];
            var lineHeight = GetLineHeight();
            var cursorX = GetCursorX(CursorPosition, line);
            const float cursorWidth = 1;

            if (cursorX < _horizontalScrollOffset)
                _horizontalScrollOffset = cursorX;
            else if (cursorX + cursorWidth > _horizontalScrollOffset + textRect.Width)
                _horizontalScrollOffset = cursorX + cursorWidth - textRect.Width;

            if (!IsMultiline)
            {
                _verticalScrollOffset = 0;
            }
            else
            {
                var visibleLineCount = GetVisibleLineCount(textRect.Height, lineHeight);
                var firstVisibleLine = GetFirstVisibleLine(lineHeight);

                if (lineIndex < firstVisibleLine)
                    firstVisibleLine = lineIndex;
                else if (lineIndex >= firstVisibleLine + visibleLineCount)
                    firstVisibleLine = lineIndex - visibleLineCount + 1;

                _verticalScrollOffset = firstVisibleLine * lineHeight;
            }

            ClampScrollOffsets(textRect, lineHeight);
        }

        private void ClampScrollOffsets(Rect textRect, float lineHeight)
        {
            var maxHorizontalScrollOffset = Math.Max(0, GetMaxLineWidth() - textRect.Width + 1);
            _horizontalScrollOffset = Math.Max(0, Math.Min(_horizontalScrollOffset, maxHorizontalScrollOffset));

            if (!IsMultiline)
            {
                _verticalScrollOffset = 0;
                return;
            }

            var visibleLineCount = GetVisibleLineCount(textRect.Height, lineHeight);
            var maxFirstVisibleLine = Math.Max(0, GetLineMetricsCache().Lines.Count - visibleLineCount);
            _verticalScrollOffset = Math.Max(0, Math.Min(_verticalScrollOffset, maxFirstVisibleLine * lineHeight));
        }

        private int GetFirstVisibleLine(float lineHeight)
        {
            if (lineHeight <= 0)
                return 0;

            return Math.Max(0, (int)(_verticalScrollOffset / lineHeight));
        }

        private static int GetVisibleLineCount(float height, float lineHeight)
        {
            if (lineHeight <= 0)
                return 1;

            return Math.Max(1, (int)Math.Floor(height / lineHeight));
        }

        private float GetMaxLineWidth()
        {
            var maxWidth = 0f;

            foreach (var lineMetric in GetLineMetricsCache().LineMetrics)
                maxWidth = Math.Max(maxWidth, lineMetric.Width);

            return maxWidth;
        }

        private TextRange GetVisibleTextRange(TextLine line, float visibleWidth)
        {
            if (line.Length == 0 || visibleWidth <= 0)
                return new TextRange(0, 0);

            var lineMetric = GetLineMetric(line);
            var leftEdge = _horizontalScrollOffset;
            var rightEdge = _horizontalScrollOffset + visibleWidth;
            var start = -1;
            var end = -1;

            for (var i = 0; i < line.Length; i++)
            {
                var charLeft = lineMetric.GetWidth(i);
                var charRight = lineMetric.GetWidth(i + 1);

                if (charRight <= leftEdge)
                    continue;
                if (charLeft >= rightEdge)
                    break;

                if (start < 0)
                    start = i;
                end = i + 1;
            }

            if (start < 0 || end <= start)
                return new TextRange(0, 0);

            return new TextRange(start, end - start);
        }

        private IClipboardService GetClipboardService()
        {
            return ScreenEngine.Instance?.Options.ClipboardService ?? NullClipboardService.Instance;
        }

        private string? TryGetClipboardText()
        {
            try
            {
                return GetClipboardService().GetText();
            }
            catch
            {
                return null;
            }
        }

        private void TrySetClipboardText(string text)
        {
            try
            {
                GetClipboardService().SetText(text);
            }
            catch
            {
            }
        }

        // Read many times per frame (draw, caret, selection, hit-test): validate with cheap field
        // compares and only build the display string and per-character metrics on a change.
        private LineMetricsCache GetLineMetricsCache()
        {
            var cache = _lineMetricsCache;
            var fontScale = FontScale;
            var wrapWidth = GetWrapWidth();
            if (cache != null
                && ReferenceEquals(cache.Text, Text)
                && cache.PasswordChar == PasswordChar
                && ReferenceEquals(cache.Font, Font)
                && ReferenceEquals(cache.TextMeasurer, TextMeasurer)
                && cache.FontScale.Equals(fontScale)
                && cache.WrapWidth.Equals(wrapWidth))
            {
                return cache;
            }

            var displayText = GetDisplayText();
            var lines = GetTextLines(Text);
            if (wrapWidth.IsFixed())
                lines = WrapLines(displayText, lines, wrapWidth);
            var lineMetrics = new LineMetric[lines.Count];
            for (var i = 0; i < lines.Count; i++)
                lineMetrics[i] = CreateLineMetric(displayText, lines[i]);

            var lineHeight = Math.Max(1, MeasureText("|").Y);
            _lineMetricsCache = new LineMetricsCache(Text, displayText, PasswordChar, Font, TextMeasurer, fontScale, lineHeight, lines, lineMetrics)
            {
                WrapWidth = wrapWidth
            };
            return _lineMetricsCache;
        }

        private LineMetric GetLineMetric(TextLine line)
        {
            // Lines are stored in text order, so look the metric up by start offset (binary search)
            // instead of scanning every line for every drawn line (O(lines²) per frame).
            var cache = GetLineMetricsCache();
            var metrics = cache.LineMetrics;
            int low = 0, high = metrics.Length - 1;
            while (low <= high)
            {
                var mid = (low + high) >> 1;
                var start = metrics[mid].Line.Start;
                if (start == line.Start)
                    return metrics[mid].Line.Length == line.Length ? metrics[mid] : CreateLineMetric(cache.DisplayText, line);
                if (start < line.Start)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            return CreateLineMetric(cache.DisplayText, line);
        }

        private readonly System.Text.StringBuilder _charBuffer = new System.Text.StringBuilder(2);

        private LineMetric CreateLineMetric(string displayText, TextLine line)
        {
            var prefixWidths = new float[line.Length + 1];
            for (var i = 0; i < line.Length; i++)
                prefixWidths[i + 1] = prefixWidths[i] + MeasureCharWidth(displayText[line.Start + i]);

            return new LineMetric(line, prefixWidths);
        }

        private float MeasureCharWidth(char character)
        {
            // SpriteFont measures a StringBuilder without allocating a one-char string per glyph.
            if (ActiveDynamicFont is { } dynamicFont)
                return dynamicFont.MeasureString(_charBuffer.Clear().Append(character), GlyphSize(dynamicFont, out var scale)).X * scale;
            if (Font != null)
                return Font.MeasureString(_charBuffer.Clear().Append(character)).X * FontScale;
            return TextMeasurer.MeasureString(character.ToString()).X * TextScaling.Factor;
        }

        internal override void OnTextScaleChanged()
        {
            InvalidateLineMetrics();
            base.OnTextScaleChanged();
        }

        private void InvalidateLineMetrics()
        {
            _lineMetricsCache = null;
        }

        /// <summary>Width soft lines wrap at: multiline and wrapping (every wrap mode wraps like <see cref="TextWrapping.Wrap"/> while editing), from
        /// the fixed width or else the arranged text area; NaN = no wrapping.</summary>
        private float GetWrapWidth()
        {
            if (!IsMultiline || TextWrapping == TextWrapping.NoWrap)
                return float.NaN;
            var width = Width.IsFixed() ? Width - Padding.Horizontal : GetTextRect().Width;
            if (RenderScale.X > 0)
                width /= RenderScale.X;
            return width > 0 ? width : float.NaN;
        }

        /// <summary>Splits hard lines into soft lines no wider than <paramref name="wrapWidth"/>,
        /// breaking after the last space that fits, or inside a word that is wider than a line.</summary>
        private List<TextLine> WrapLines(string displayText, List<TextLine> hardLines, float wrapWidth)
        {
            var result = new List<TextLine>(hardLines.Count);
            foreach (var hard in hardLines)
            {
                var lineStart = hard.Start;
                var end = hard.Start + hard.Length;
                var width = 0f;
                var lastBreak = -1;
                for (var i = hard.Start; i < end; i++)
                {
                    var charWidth = MeasureCharWidth(displayText[i]);
                    if (width + charWidth > wrapWidth && i > lineStart)
                    {
                        var breakAt = lastBreak > lineStart ? lastBreak : i;
                        result.Add(new TextLine(lineStart, breakAt - lineStart));
                        lineStart = breakAt;
                        lastBreak = -1;
                        width = 0;
                        for (var j = lineStart; j < i; j++)
                            width += MeasureCharWidth(displayText[j]);
                    }

                    width += charWidth;
                    if (char.IsWhiteSpace(displayText[i]))
                        lastBreak = i + 1;
                }

                result.Add(new TextLine(lineStart, end - lineStart));
            }

            return result;
        }

        private static List<TextLine> GetTextLines(string text)
        {
            var lines = new List<TextLine>();
            var start = 0;

            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != '\n')
                    continue;

                lines.Add(new TextLine(start, i - start));
                start = i + 1;
            }

            lines.Add(new TextLine(start, text.Length - start));
            return lines;
        }

        private readonly struct TextLine
        {
            public TextLine(int start, int length)
            {
                Start = start;
                Length = length;
            }

            public int Start { get; }
            public int Length { get; }
        }

        private sealed class LineMetricsCache
        {
            public LineMetricsCache(
                string text,
                string displayText,
                char? passwordChar,
                SpriteFont? font,
                ITextMeasurer textMeasurer,
                float fontScale,
                float lineHeight,
                List<TextLine> lines,
                LineMetric[] lineMetrics)
            {
                Text = text;
                DisplayText = displayText;
                PasswordChar = passwordChar;
                Font = font;
                TextMeasurer = textMeasurer;
                FontScale = fontScale;
                LineHeight = lineHeight;
                Lines = lines;
                LineMetrics = lineMetrics;
            }

            public char? PasswordChar { get; }

            public float WrapWidth { get; init; } = float.NaN;

            public float FontScale { get; }

            public float LineHeight { get; }

            public string Text { get; }

            public string DisplayText { get; }

            public SpriteFont? Font { get; }

            public ITextMeasurer TextMeasurer { get; }

            public List<TextLine> Lines { get; }

            public LineMetric[] LineMetrics { get; }
        }

        private readonly struct LineMetric
        {
            public LineMetric(TextLine line, float[] prefixWidths)
            {
                Line = line;
                _prefixWidths = prefixWidths;
            }

            private readonly float[] _prefixWidths;

            public TextLine Line { get; }

            public float Width => GetWidth(Line.Length);

            public float GetWidth(int length)
            {
                var clamped = Math.Max(0, Math.Min(length, _prefixWidths.Length - 1));
                return _prefixWidths[clamped];
            }
        }

        private readonly struct TextRange
        {
            public TextRange(int start, int length)
            {
                Start = start;
                Length = length;
            }

            public int Start { get; }
            public int Length { get; }
        }
    }
}
