using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Media;

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
                EnsureCursorVisible();
                InvalidateLayout(false);
            }
        }

        public bool IsMultiline
        {
            get { return _isMultiline; }
            set
            {
                if (_isMultiline == value)
                    return;

                _isMultiline = value;
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

            BackgroundBrush = theme.TextBoxBackgroundBrush;
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

            var lines = GetTextLines(Text);
            var displayText = GetDisplayText();
            var lineHeight = GetLineHeight();
            var measuredWidth = 0f;

            foreach (var line in lines)
            {
                var lineText = displayText.Substring(line.Start, line.Length);
                measuredWidth = Math.Max(measuredWidth, MeasureText(lineText).X);
            }

            var measuredHeight = Math.Max(lineHeight, lines.Count * lineHeight);
            var width = Width.IsFixed() ? Width : measuredWidth + Padding.Horizontal;
            var height = Height.IsFixed() ? Height : measuredHeight + Padding.Vertical;

            return ApplyConstraints(new Size(width, height) + Margin);
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            EnsureCursorVisible();
        }

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
                    Backspace();
                    break;
                case KeyboardCommand.Delete:
                    Delete();
                    break;
                case KeyboardCommand.Enter:
                    if (IsMultiline && !control)
                        InsertText("\n");
                    else
                        EnterPressed?.Invoke(this, EventArgs.Empty);
                    break;
                case KeyboardCommand.CursorLeft:
                    MoveCursorTo(Math.Max(0, CursorPosition - 1), shift);
                    break;
                case KeyboardCommand.CursorRight:
                    MoveCursorTo(Math.Min(Text.Length, CursorPosition + 1), shift);
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
                    throw new ArgumentOutOfRangeException();
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
                spriteBatch.DrawString(Font, HintText, offset, Brush.ApplyOpacity(HintTextColor, RenderOpacity), 0, Vector2.Zero, RenderScale, SpriteEffects.None, 0);
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
                ReplaceRange(CursorPosition - 1, 1, "");
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
                ReplaceRange(CursorPosition, 1, "");
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
                var oldText = base.Text;
                base.Text = newText;
                OnTextChanged(new TextChangedEventArgs(newText, oldText));
            }

            _cursorPosition = ClampTextPosition(newCursorPosition);
            _selectionAnchor = _cursorPosition;
            ResetDesiredCursorX();
            EnsureCursorVisible();
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

            var lines = GetTextLines(Text);
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
            var lines = GetTextLines(Text);
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
            var lines = GetTextLines(Text);
            return lines[GetLineIndexFromPosition(CursorPosition, lines)];
        }

        private float GetCursorX(int position, TextLine line)
        {
            var displayText = GetDisplayText();
            var offsetInLine = Math.Max(0, Math.Min(line.Length, position - line.Start));
            return MeasureText(displayText.Substring(line.Start, offsetInLine)).X;
        }

        private int GetPositionForX(TextLine line, float x)
        {
            var displayText = GetDisplayText();
            var closestIndex = 0;
            var closestDistance = float.MaxValue;

            for (var i = 0; i <= line.Length; i++)
            {
                var measured = MeasureText(displayText.Substring(line.Start, i)).X;
                var distance = Math.Abs(measured - x);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestIndex = i;
            }

            return line.Start + closestIndex;
        }

        private int GetLineIndexFromPosition(int position, IReadOnlyList<TextLine> lines)
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

            var lines = GetTextLines(Text);
            var displayText = GetDisplayText();
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

                var top = GetLineTop(textRect, lineHeight, lines.Count, i);
                if (!IsLineVisible(textRect, top, lineHeight))
                    continue;

                var beforeSelection = displayText.Substring(line.Start, rangeStart - line.Start);
                var selected = displayText.Substring(rangeStart, rangeEnd - rangeStart);
                var rawLeft = textRect.Left + (MeasureText(beforeSelection).X - _horizontalScrollOffset) * RenderScale.X;
                var rawRight = rawLeft + Math.Max(1, MeasureText(selected).X * RenderScale.X);
                var left = Math.Max(textRect.Left, rawLeft);
                var right = Math.Min(textRect.Right, rawRight);
                if (right <= left)
                    continue;

                SelectionBrush.Draw(spriteBatch, new Rect(left, top, right - left, lineHeight), RenderOpacity);
            }
        }

        private void DrawText(SpriteBatch spriteBatch, Rect textRect)
        {
            if (Font == null || Text.Length == 0)
                return;

            var lines = GetTextLines(Text);
            var displayText = GetDisplayText();
            var lineHeight = GetLineHeight();

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var lineTop = GetLineTop(textRect, lineHeight, lines.Count, i);
                if (!IsLineVisible(textRect, lineTop, lineHeight))
                    continue;

                var visibleRange = GetVisibleTextRange(displayText, line, textRect.Width / Math.Max(0.001f, RenderScale.X));
                if (visibleRange.Length <= 0)
                    continue;

                var beforeVisible = displayText.Substring(line.Start, visibleRange.Start);
                var offset = new PointF(textRect.Left + (MeasureText(beforeVisible).X - _horizontalScrollOffset) * RenderScale.X, lineTop);
                if (SnapToPixel)
                    offset = offset.ToInts();
                spriteBatch.DrawString(Font, displayText.Substring(line.Start + visibleRange.Start, visibleRange.Length), offset, Brush.ApplyOpacity(TextColor, RenderOpacity), 0, Vector2.Zero, RenderScale, SpriteEffects.None, 0);
            }
        }

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
            var lines = GetTextLines(Text);
            var lineIndex = GetLineIndexFromPosition(CursorPosition, lines);
            var line = lines[lineIndex];
            var lineHeight = GetLineHeight();
            var top = GetLineTop(textRect, lineHeight, lines.Count, lineIndex);
            if (!IsLineVisible(textRect, top, lineHeight))
                return Rect.Empty;

            var x = textRect.Left + GetCursorX(CursorPosition, line) - _horizontalScrollOffset;
            if (x < textRect.Left || x > textRect.Right)
                return Rect.Empty;

            return new Rect(x, top, 1, lineHeight);
        }

        private float GetTextTop(Rect textRect, float lineHeight, int lineCount)
        {
            if (IsMultiline)
                return textRect.Top;

            return textRect.Top + (textRect.Height - lineHeight) / 2;
        }

        private float GetLineTop(Rect textRect, float lineHeight, int lineCount, int lineIndex)
        {
            var top = GetTextTop(textRect, lineHeight, lineCount) + lineIndex * lineHeight;
            return IsMultiline ? top - _verticalScrollOffset : top;
        }

        private bool IsLineVisible(Rect textRect, float top, float lineHeight)
        {
            return top >= textRect.Top && top + lineHeight <= textRect.Bottom;
        }

        private float GetLineHeight()
        {
            return Math.Max(1, MeasureText("|").Y);
        }

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

            var lines = GetTextLines(Text);
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

            ClampScrollOffsets(textRect, lines, lineHeight);
        }

        private void ClampScrollOffsets(Rect textRect, IReadOnlyList<TextLine> lines, float lineHeight)
        {
            var maxHorizontalScrollOffset = Math.Max(0, GetMaxLineWidth(lines) - textRect.Width + 1);
            _horizontalScrollOffset = Math.Max(0, Math.Min(_horizontalScrollOffset, maxHorizontalScrollOffset));

            if (!IsMultiline)
            {
                _verticalScrollOffset = 0;
                return;
            }

            var visibleLineCount = GetVisibleLineCount(textRect.Height, lineHeight);
            var maxFirstVisibleLine = Math.Max(0, lines.Count - visibleLineCount);
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

        private float GetMaxLineWidth(IReadOnlyList<TextLine> lines)
        {
            var displayText = GetDisplayText();
            var maxWidth = 0f;

            foreach (var line in lines)
                maxWidth = Math.Max(maxWidth, MeasureText(displayText.Substring(line.Start, line.Length)).X);

            return maxWidth;
        }

        private TextRange GetVisibleTextRange(string displayText, TextLine line, float visibleWidth)
        {
            if (line.Length == 0 || visibleWidth <= 0)
                return new TextRange(0, 0);

            var leftEdge = _horizontalScrollOffset;
            var rightEdge = _horizontalScrollOffset + visibleWidth;
            var start = -1;
            var end = -1;

            for (var i = 0; i < line.Length; i++)
            {
                var charLeft = MeasureText(displayText.Substring(line.Start, i)).X;
                var charRight = MeasureText(displayText.Substring(line.Start, i + 1)).X;

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
