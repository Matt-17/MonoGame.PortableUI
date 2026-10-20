using System;

namespace MonoGame.PortableUI.Input
{
    /// <summary>What a text field expects, so the platform keyboard can pick a fitting layout.</summary>
    public enum TextInputPurpose
    {
        Text,
        Number,
        Email,
        Password,
        Url,
        Search
    }

    /// <summary>Everything a platform keyboard may need to know about the field asking for it.</summary>
    public sealed class OnScreenKeyboardRequest
    {
        public TextInputPurpose Purpose { get; init; }
        public string Text { get; init; } = "";
        public string? Hint { get; init; }
        public bool IsMultiline { get; init; }
        public int MaxLength { get; init; }

        /// <summary>The field's screen rectangle in layout units (where a floating keyboard should not go).</summary>
        public Common.Rect FieldBounds { get; init; }

        /// <summary>
        ///     For keyboards that hand back the finished text at once instead of typing into the field
        ///     (Steam's gamepad text input, modal dialogs): call with the text to replace the field's
        ///     content. Must be invoked on the game thread.
        /// </summary>
        public Action<string>? Commit { get; init; }

        /// <summary>
        ///     For keyboards that edit the field like a text editor (Android's input methods: gesture typing,
        ///     suggestions, autocorrect): reads and replaces the field's text, selection and composing word.
        ///     Game thread only.
        /// </summary>
        public ITextEditTarget? Editor { get; init; }
    }

    /// <summary>A text field's content as an input method sees it: text, selection and the word being composed.</summary>
    public readonly struct TextEditState : IEquatable<TextEditState>
    {
        public TextEditState(string text, int selectionStart, int selectionEnd, int composingStart = -1, int composingEnd = -1)
        {
            Text = text ?? "";
            SelectionStart = selectionStart;
            SelectionEnd = selectionEnd;
            ComposingStart = composingStart;
            ComposingEnd = composingEnd;
        }

        public string Text { get; }
        public int SelectionStart { get; }
        public int SelectionEnd { get; }

        /// <summary>The word the keyboard is still working on (underlined), or -1.</summary>
        public int ComposingStart { get; }
        public int ComposingEnd { get; }

        public bool Equals(TextEditState other) => Text == other.Text && SelectionStart == other.SelectionStart && SelectionEnd == other.SelectionEnd
            && ComposingStart == other.ComposingStart && ComposingEnd == other.ComposingEnd;

        public override bool Equals(object? obj) => obj is TextEditState other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Text, SelectionStart, SelectionEnd, ComposingStart, ComposingEnd);
    }

    /// <summary>The field side of <see cref="OnScreenKeyboardRequest.Editor"/>. Game thread only.</summary>
    public interface ITextEditTarget
    {
        TextEditState State { get; }

        /// <summary>The keyboard edited the text: take <paramref name="state"/> over (as a user edit, undoable).</summary>
        void Apply(TextEditState state);

        /// <summary>Enter, Done, Go or Search on the keyboard.</summary>
        void Submit();
    }

    /// <summary>
    ///     A keyboard that mirrors the field it edits (<see cref="OnScreenKeyboardRequest.Editor"/>): told when the field
    ///     changed by other means (a tap moved the caret, paste, code), so it does not edit stale text. Game thread.
    /// </summary>
    public interface ITextEditKeyboard
    {
        void TargetChanged(TextEditState state);
    }

    public sealed class OnScreenKeyboardEventArgs : EventArgs
    {
        public OnScreenKeyboardEventArgs(bool isVisible, float coveredHeightPixels)
        {
            IsVisible = isVisible;
            CoveredHeightPixels = coveredHeightPixels;
        }

        public bool IsVisible { get; }

        /// <summary>Height covered at the bottom of the window, in window pixels (0 for floating keyboards).</summary>
        public float CoveredHeightPixels { get; }
    }

    /// <summary>
    ///     The platform's software keyboard. Text fields call <see cref="Show"/> when they gain focus and
    ///     <see cref="Hide"/> when they lose it. Set one via <see cref="ScreenEngineOptions.OnScreenKeyboard"/>
    ///     or <see cref="ScreenEngine.OnScreenKeyboard"/>; the default does nothing (desktop).
    /// </summary>
    public interface IOnScreenKeyboard
    {
        bool IsVisible { get; }

        void Show(OnScreenKeyboardRequest request);

        void Hide();

        /// <summary>Raised when the keyboard appears, disappears or changes height. May come from any thread.</summary>
        event EventHandler<OnScreenKeyboardEventArgs>? VisibilityChanged;
    }

    /// <summary>Desktop: a physical keyboard is present, nothing to show.</summary>
    public sealed class NullOnScreenKeyboard : IOnScreenKeyboard
    {
        public static NullOnScreenKeyboard Instance { get; } = new NullOnScreenKeyboard();

        public bool IsVisible => false;

        public void Show(OnScreenKeyboardRequest request)
        {
        }

        public void Hide()
        {
        }

        public event EventHandler<OnScreenKeyboardEventArgs>? VisibilityChanged
        {
            add { }
            remove { }
        }
    }

    /// <summary>
    ///     Registration hook for keyboards the toolkit does not reference, such as Steam's floating
    ///     gamepad keyboard, without a dependency on their SDK:
    ///     <code>
    ///     engine.OnScreenKeyboard = new DelegateOnScreenKeyboard(
    ///         show: r => SteamUtils.ShowFloatingGamepadTextInput(EFloatingGamepadTextInputMode.k_EFloatingGamepadTextInputModeModeSingleLine,
    ///                    (int)r.FieldBounds.Left, (int)r.FieldBounds.Top, (int)r.FieldBounds.Width, (int)r.FieldBounds.Height),
    ///         hide: () => SteamUtils.DismissFloatingGamepadTextInput());
    ///     </code>
    ///     The floating Steam keyboard types into the focused field as ordinary key/text input.
    /// </summary>
    public sealed class DelegateOnScreenKeyboard : IOnScreenKeyboard
    {
        private readonly Func<OnScreenKeyboardRequest, bool> _show;
        private readonly Action? _hide;

        /// <param name="show">Shows the keyboard; returns whether it actually appeared.</param>
        /// <param name="hide">Hides it, if the platform supports that.</param>
        public DelegateOnScreenKeyboard(Func<OnScreenKeyboardRequest, bool> show, Action? hide = null)
        {
            _show = show ?? throw new ArgumentNullException(nameof(show));
            _hide = hide;
        }

        public bool IsVisible { get; private set; }

        public event EventHandler<OnScreenKeyboardEventArgs>? VisibilityChanged;

        public void Show(OnScreenKeyboardRequest request)
        {
            if (!_show(request) || IsVisible)
                return;
            IsVisible = true;
            VisibilityChanged?.Invoke(this, new OnScreenKeyboardEventArgs(true, 0));
        }

        public void Hide()
        {
            if (!IsVisible)
                return;
            _hide?.Invoke();
            IsVisible = false;
            VisibilityChanged?.Invoke(this, new OnScreenKeyboardEventArgs(false, 0));
        }

        /// <summary>Call when the platform reports the keyboard closed by itself (e.g. Steam's dismissed callback).</summary>
        public void NotifyClosed()
        {
            if (!IsVisible)
                return;
            IsVisible = false;
            VisibilityChanged?.Invoke(this, new OnScreenKeyboardEventArgs(false, 0));
        }
    }
}
