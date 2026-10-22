#if ANDROID
using System;
using Android.Content;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI
{
    /// <summary>
    ///     <see cref="IOnScreenKeyboard"/> backed by the Android input method (IME) on the game view.
    ///     Typed characters reach the focused field through MonoGame's text input like a hardware
    ///     keyboard. The covered height comes from <see cref="AndroidWindowInsets"/>, so attach that too.
    /// </summary>
    /// <remarks>
    ///     A field that offers <see cref="OnScreenKeyboardRequest.Editor"/> (TextBox does) is edited through a real
    ///     text input connection on a small invisible view next to the game view, as an EditText would be: gesture
    ///     typing, suggestions, autocorrect and the purpose's layout (number, e-mail, ...) work. MonoGame's own view
    ///     offers no such connection, so the IME falls back to plain key presses there.
    /// </remarks>
    public sealed class AndroidOnScreenKeyboard : IOnScreenKeyboard, ITextEditKeyboard
    {
        private readonly View _view;
        private readonly ScreenEngine? _engine;
        private ImeView? _imeView;
        // UI thread only: the text as the IME edits it, and what the field last heard of it.
        private readonly SpannableStringBuilder _editable = new SpannableStringBuilder();
        private TextEditState _pushed;
        private bool _checkPosted;
        private OnScreenKeyboardRequest? _request;
        // UI thread: whether the IME window was last seen on screen (window insets).
        private bool _imeShown;

        /// <param name="gameView">MonoGame's game view.</param>
        /// <param name="engine">Receives the typed characters (MonoGame has no TextInput event on
        /// Android). Without it, the host must call <see cref="ScreenEngine.HandleTextInput"/> itself.</param>
        public AndroidOnScreenKeyboard(View gameView, ScreenEngine? engine = null)
        {
            _view = gameView ?? throw new ArgumentNullException(nameof(gameView));
            _engine = engine;
            if (engine != null)
            {
                _view.KeyPress += OnKeyPress;
                AndroidInputBridge.EditingKeysRoutedAsCommands = true;
            }
        }

        // Printable characters become text input; control keys (backspace, arrows, enter) are left
        // to MonoGame's keyboard state, so the event is never marked handled.
        private void OnKeyPress(object? sender, View.KeyEventArgs e)
        {
            e.Handled = false;
            var keyEvent = e.Event;
            if (keyEvent == null || _engine == null)
                return;

            // Editing keys from the soft keyboard are a single short press; polled keyboard state can
            // miss them, so route them as commands and keep MonoGame from handling them again.
            KeyboardCommand? command = keyEvent.KeyCode switch
            {
                Keycode.Del => KeyboardCommand.Backspace,
                Keycode.ForwardDel => KeyboardCommand.Delete,
                Keycode.Enter or Keycode.NumpadEnter => KeyboardCommand.Enter,
                _ => null
            };
            if (command is { } editCommand)
            {
                e.Handled = true;
                if (keyEvent.Action == KeyEventActions.Down)
                    _engine.InvokeOnGameThread(() => _engine.HandleKeyCommand(editCommand));
                return;
            }

            string? text = null;
#pragma warning disable CA1422 // ACTION_MULTIPLE character strings are still sent by some IMEs
            if (keyEvent.Action == KeyEventActions.Multiple && keyEvent.KeyCode == Keycode.Unknown)
                text = keyEvent.Characters;
#pragma warning restore CA1422
            else if (keyEvent.Action == KeyEventActions.Down)
            {
                var unicode = keyEvent.UnicodeChar;
                if (unicode > 0 && !char.IsControl((char)unicode))
                    text = ((char)unicode).ToString();
            }

            if (string.IsNullOrEmpty(text))
                return;
            var engine = _engine;
            engine.InvokeOnGameThread(() =>
            {
                foreach (var character in text)
                    engine.HandleTextInput(character);
            });
        }

        public bool IsVisible => _isVisible;

        private volatile bool _isVisible;

        public event EventHandler<OnScreenKeyboardEventArgs>? VisibilityChanged;

        public void Show(OnScreenKeyboardRequest request)
        {
            _view.Post(() =>
            {
                _request = request;
                if (request.Editor != null && _engine != null && EnsureImeView() is { } ime)
                {
                    var state = request.Editor.State;
                    _editable.Replace(0, _editable.Length(), state.Text);
                    BaseInputConnection.RemoveComposingSpans(_editable);
                    Selection.SetSelection(_editable, Clamp(state.SelectionStart), Clamp(state.SelectionEnd));
                    _pushed = new TextEditState(state.Text, state.SelectionStart, state.SelectionEnd);
                    ime.RequestFocus();
                    InputMethodManager()?.RestartInput(ime);
                    InputMethodManager()?.ShowSoftInput(ime, ShowFlags.Implicit);
                    return;
                }
                _view.FocusableInTouchMode = true;
                _view.RequestFocus();
                InputMethodManager()?.ShowSoftInput(_view, ShowFlags.Implicit);
            });
            if (_isVisible)
                return;
            _isVisible = true;
            VisibilityChanged?.Invoke(this, new OnScreenKeyboardEventArgs(true, 0));
        }

        public void Hide()
        {
            _view.Post(() =>
            {
                _request = null;
                InputMethodManager()?.HideSoftInputFromWindow((_imeView ?? _view).WindowToken, HideSoftInputFlags.None);
                // Hardware keys go to the game view again.
                if (_imeView is { IsFocused: true })
                    _view.RequestFocus();
            });
            if (!_isVisible)
                return;
            _isVisible = false;
            VisibilityChanged?.Invoke(this, new OnScreenKeyboardEventArgs(false, 0));
        }

        // UI thread. The system closed the keyboard (back, the IME's own hide key): as for a native EditText the field
        // keeps its focus, but the invisible editor gives the view focus back, so the next back reaches the app instead
        // of the input method (which showed itself again). Tapping the field shows the keyboard again.
        private void OnImeVisibility(bool shown)
        {
            var wasShown = _imeShown;
            _imeShown = shown;
            if (shown || !wasShown || _imeView is not { IsFocused: true })
                return;
            _view.RequestFocus();
            if (_engine != null)
                _engine.InvokeOnGameThread(SystemHidden);
            else
                SystemHidden();
        }

        private void SystemHidden()
        {
            if (!_isVisible)
                return;
            _isVisible = false;
            VisibilityChanged?.Invoke(this, new OnScreenKeyboardEventArgs(false, 0));
        }

        /// <inheritdoc />
        public void TargetChanged(TextEditState state)
        {
            _view.Post(() =>
            {
                if (_imeView == null || _request?.Editor == null)
                    return;
                if (_editable.ToString() != state.Text)
                {
                    _editable.Replace(0, _editable.Length(), state.Text);
                    BaseInputConnection.RemoveComposingSpans(_editable);
                }
                Selection.SetSelection(_editable, Clamp(state.SelectionStart), Clamp(state.SelectionEnd));
                _pushed = state;
                InputMethodManager()?.UpdateSelection(_imeView, state.SelectionStart, state.SelectionEnd, state.ComposingStart, state.ComposingEnd);
            });
        }

        private int Clamp(int position) => Math.Clamp(position, 0, _editable.Length());

        private ImeView? EnsureImeView()
        {
            if (_imeView != null)
                return _imeView;
            if (_view.Parent is not ViewGroup parent || _view.Context is not { } context)
                return null;
            _imeView = new ImeView(context, this) { Alpha = 0, Focusable = true, FocusableInTouchMode = true };
            parent.AddView(_imeView, new ViewGroup.LayoutParams(1, 1));
            if (System.OperatingSystem.IsAndroidVersionAtLeast(30))
                _imeView.SetOnApplyWindowInsetsListener(new ImeInsetsListener(this));
            // With a text editor focused, Android would pan or resize the window for the keyboard; PortableUI lifts
            // the field itself from the keyboard inset (AndroidWindowInsets), so the window stays as it is.
            (context as Android.App.Activity)?.Window?.SetSoftInputMode(SoftInput.AdjustNothing);
            return _imeView;
        }

        // The connection reaches the editable for every IME call; one check after them hands the result over.
        private SpannableStringBuilder EditableTouched()
        {
            if (!_checkPosted)
            {
                _checkPosted = true;
                _view.Post(PushIfChanged);
            }
            return _editable;
        }

        private void PushIfChanged()
        {
            _checkPosted = false;
            if (_request?.Editor is not { } editor || _engine == null)
                return;
            var state = new TextEditState(_editable.ToString(), Selection.GetSelectionStart(_editable), Selection.GetSelectionEnd(_editable),
                BaseInputConnection.GetComposingSpanStart(_editable), BaseInputConnection.GetComposingSpanEnd(_editable));
            if (state.Equals(_pushed))
                return;
            _pushed = state;
            _engine.InvokeOnGameThread(() => editor.Apply(state));
        }

        private void EditorAction()
        {
            if (_request?.Editor is { } editor && _engine != null)
                _engine.InvokeOnGameThread(editor.Submit);
        }

        private void Describe(EditorInfo info)
        {
            var request = _request;
            var type = request?.Purpose switch
            {
                TextInputPurpose.Number => InputTypes.ClassNumber | InputTypes.NumberFlagDecimal | InputTypes.NumberFlagSigned,
                TextInputPurpose.Email => InputTypes.ClassText | InputTypes.TextVariationEmailAddress,
                TextInputPurpose.Password => InputTypes.ClassText | InputTypes.TextVariationPassword,
                TextInputPurpose.Url => InputTypes.ClassText | InputTypes.TextVariationUri,
                _ => InputTypes.ClassText | InputTypes.TextFlagAutoCorrect | InputTypes.TextFlagCapSentences
            };
            if (request?.IsMultiline == true)
                type |= InputTypes.TextFlagMultiLine;
            info.InputType = type;
            var action = request?.IsMultiline == true ? ImeAction.None : request?.Purpose == TextInputPurpose.Search ? ImeAction.Search : ImeAction.Done;
            info.ImeOptions = (ImeFlags)((int)action | (int)ImeFlags.NoExtractUi | (int)ImeFlags.NoFullscreen);
            info.InitialSelStart = Selection.GetSelectionStart(_editable);
            info.InitialSelEnd = Selection.GetSelectionEnd(_editable);
            if (request?.Hint is { } hint)
                info.HintText = new Java.Lang.String(hint);
        }

        // Tells the keyboard when the IME window appears and disappears, whoever closed it.
        private sealed class ImeInsetsListener : Java.Lang.Object, View.IOnApplyWindowInsetsListener
        {
            private readonly AndroidOnScreenKeyboard _keyboard;

            public ImeInsetsListener(AndroidOnScreenKeyboard keyboard) => _keyboard = keyboard;

            public WindowInsets OnApplyWindowInsets(View view, WindowInsets insets)
            {
                if (System.OperatingSystem.IsAndroidVersionAtLeast(30))
                    _keyboard.OnImeVisibility(insets.IsVisible(WindowInsets.Type.Ime()));
                return view.OnApplyWindowInsets(insets) ?? insets;
            }
        }

        /// <summary>The view the IME types into: invisible, 1 px, next to the game view.</summary>
        private sealed class ImeView : View
        {
            private readonly AndroidOnScreenKeyboard _keyboard;

            public ImeView(Context context, AndroidOnScreenKeyboard keyboard) : base(context) => _keyboard = keyboard;

            public override bool OnCheckIsTextEditor() => true;

            public override IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
            {
                if (outAttrs != null)
                    _keyboard.Describe(outAttrs);
                return new ImeConnection(this, _keyboard);
            }

            // Key presses (a hardware keyboard, the IME's Backspace) reach the game view's handlers as before. The
            // IME's Enter is its Done: as a key command it would switch on keyboard navigation (a focus ring).
            public override bool DispatchKeyEvent(KeyEvent? e)
            {
                if (e == null)
                    return false;
                if (e.KeyCode is Keycode.Enter or Keycode.NumpadEnter && (e.Flags & KeyEventFlags.SoftKeyboard) != 0
                    && _keyboard._request is { IsMultiline: false })
                {
                    if (e.Action == KeyEventActions.Down)
                        _keyboard.EditorAction();
                    return true;
                }
                return _keyboard._view.DispatchKeyEvent(e);
            }
        }

        private sealed class ImeConnection : BaseInputConnection
        {
            private readonly AndroidOnScreenKeyboard _keyboard;

            public ImeConnection(View view, AndroidOnScreenKeyboard keyboard) : base(view, true) => _keyboard = keyboard;

            // Every edit the IME makes goes through the editable.
            public override IEditable? Editable => _keyboard.EditableTouched();

            public override bool PerformEditorAction(ImeAction actionCode)
            {
                _keyboard.EditorAction();
                return true;
            }
        }

        private InputMethodManager? InputMethodManager()
            => _view.Context?.GetSystemService(Context.InputMethodService) as InputMethodManager;
    }
}
#endif
