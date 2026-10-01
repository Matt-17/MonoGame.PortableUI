using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Media;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Controls
{
    public class TextBox : TextBlock
    {
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
        public Brush SelectionBrush { get; set; }

        public event EventHandler? EnterPressed;

        public string? InputScope { get; set; }

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

        public Thickness Padding { get; set; }
        public event TextChangedEventHandler? TextChanged;

        public TextBox()
        {
            var theme = PortableTheme.ResolveCurrent();

            IsFocusable = true; // TextBlock disables this; text input needs focus back
            TextColor = theme.TextBoxTextColor;
            CursorColor = theme.TextBoxCursorBrush;
            SelectionBrush = theme.TextBoxSelectionBrush;
            HintTextColor = theme.TextBoxHintTextColor;
            KeyPressed += HandleKeyPressed;
            Click += OnClick;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            TouchDown += OnTouchDown;
            TouchMove += OnTouchMove;
            TouchUp += OnTouchUp;
            Height = theme.TextBoxHeight;
            Padding = theme.TextBoxPadding;
            ShowFocusVisual = true;
        }

        protected override ControlStyle? GetThemeStyle(PortableTheme theme)
        {
            return theme.TextBox;
        }

        protected override Brush? GetThemeBackgroundBrush(PortableTheme theme)
        {
            return theme.TextBoxBackgroundBrush;
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            // TextBlock re-seeds TextColor from the generic text color first; capture whether it was
            // the TextBox slot before that, or the comparison below never matches.
            var textWasThemeDefault = TextColor.Equals(oldTheme.TextBoxTextColor);

            base.OnThemeChanged(oldTheme, newTheme);

            if (textWasThemeDefault)
                TextColor = newTheme.TextBoxTextColor;
            if (ReferenceEquals(CursorColor, oldTheme.TextBoxCursorBrush))
                CursorColor = newTheme.TextBoxCursorBrush;
            if (ReferenceEquals(SelectionBrush, oldTheme.TextBoxSelectionBrush))
                SelectionBrush = newTheme.TextBoxSelectionBrush;
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
            if (IsGone)
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
            _isPointerSelecting = true;
            SetCursorFromPosition(args.Position, false);
        }

        private void OnTouchMove(object? sender, TouchEventArgs args)
        {
            if (_isPointerSelecting)
                SetCursorFromPosition(args.Position, true);
        }

        private void OnTouchUp(object? sender, TouchEventArgs args)
        {
            SetCursorFromPosition(args.Position, _isPointerSelecting);
            _isPointerSelecting = false;
        }

        protected internal override void OnGotFocus(GotFocusEventArgs args)
        {
            base.OnGotFocus(args);
            ScreenEngine.Instance?.RequestKeyboard(InputScope);
            AttachKeyboard(ScreenEngine.Instance?.CurrentKeyboard);
        }

        protected internal override void OnLostFocus(LostFocusEventArgs args)
        {
            base.OnLostFocus(args);
            DetachKeyboard();
            ScreenEngine.Instance?.HideKeyboard();
            _isPointerSelecting = false;
        }

        protected internal virtual void HandleKeyPressed(object? sender, KeyEventArgs args)
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
            BackgroundBrush?.Draw(spriteBatch, rect, RenderOpacity);
            var textRect = rect - Padding;

            if (Text.Length == 0 && !string.IsNullOrEmpty(HintText) && Font != null)
            {
                var measuredHint = MeasureText(HintText);
                var scaledHint = new Vector2(measuredHint.X * RenderScale.X, measuredHint.Y * RenderScale.Y);
                var offset = textRect.Offset;
                if (!IsMultiline)
                    offset.Y += (textRect.Height - scaledHint.Y) / 2;
                if (SnapToPixel)
                    offset = offset.ToInts();
                spriteBatch.DrawString(Font, HintText, offset, Brush.ApplyOpacity(HintTextColor, RenderOpacity), 0, Vector2.Zero, TextDrawScale, SpriteEffects.None, 0);
            }

            DrawSelection(spriteBatch, textRect);
            DrawText(spriteBatch, textRect);
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
            var lineHeight = GetLineHeight();
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
            if (Font == null || Text.Length == 0)
                return;

            var cache = GetLineMetricsCache();
            var lines = cache.Lines;
            var displayText = cache.DisplayText;
            var lineHeight = GetLineHeight();

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var lineTop = GetLineTop(textRect, lineHeight, i);
                if (!IsLineVisible(textRect, lineTop, lineHeight))
                    continue;

                var visibleRange = GetVisibleTextRange(line, textRect.Width / Math.Max(0.001f, RenderScale.X));
                if (visibleRange.Length <= 0)
                    continue;

                var offset = new PointF(textRect.Left + (GetLineMetric(line).GetWidth(visibleRange.Start) - _horizontalScrollOffset) * RenderScale.X, lineTop);
                if (SnapToPixel)
                    offset = offset.ToInts();
                _drawBuffer.Clear().Append(displayText, line.Start + visibleRange.Start, visibleRange.Length);
                spriteBatch.DrawString(Font, _drawBuffer, offset, Brush.ApplyOpacity(TextColor, RenderOpacity), 0, Vector2.Zero, TextDrawScale, SpriteEffects.None, 0);
            }
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

            if (ScreenSystem.TotalTime.TotalMilliseconds % 1000 >= 500)
                return;

            var cursorRect = GetCursorRect(textRect);
            if (cursorRect == Rect.Empty)
                return;

            cursorRect.Width = Math.Max(1, cursorRect.Width * RenderScale.X);
            cursorRect.Height *= RenderScale.Y;
            CursorColor.Draw(spriteBatch, cursorRect, RenderOpacity);
        }

        internal Rect GetCursorRect(Rect textRect)
        {
            var lines = GetLineMetricsCache().Lines;
            var lineIndex = GetLineIndexFromPosition(CursorPosition, lines);
            var line = lines[lineIndex];
            var lineHeight = GetLineHeight();
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
            return IsMultiline ? top - _verticalScrollOffset : top;
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

        /// <summary>Width soft lines wrap at: multiline + <see cref="TextWrapping.Wrap"/> only, from
        /// the fixed width or else the arranged text area; NaN = no wrapping.</summary>
        private float GetWrapWidth()
        {
            if (!IsMultiline || TextWrapping != TextWrapping.Wrap)
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
