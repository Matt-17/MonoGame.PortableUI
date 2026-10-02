#if ANDROID
using System;
using Android.Content;
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
    ///     The input purpose is not forwarded: MonoGame's game view owns the input connection, so the
    ///     IME shows its default text layout. Number or e-mail layouts would need a custom view.
    /// </remarks>
    public sealed class AndroidOnScreenKeyboard : IOnScreenKeyboard
    {
        private readonly View _view;
        private readonly ScreenEngine? _engine;

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

        public bool IsVisible { get; private set; }

        public event EventHandler<OnScreenKeyboardEventArgs>? VisibilityChanged;

        public void Show(OnScreenKeyboardRequest request)
        {
            _view.Post(() =>
            {
                _view.FocusableInTouchMode = true;
                _view.RequestFocus();
                InputMethodManager()?.ShowSoftInput(_view, ShowFlags.Implicit);
            });
            if (IsVisible)
                return;
            IsVisible = true;
            VisibilityChanged?.Invoke(this, new OnScreenKeyboardEventArgs(true, 0));
        }

        public void Hide()
        {
            _view.Post(() => InputMethodManager()?.HideSoftInputFromWindow(_view.WindowToken, HideSoftInputFlags.None));
            if (!IsVisible)
                return;
            IsVisible = false;
            VisibilityChanged?.Invoke(this, new OnScreenKeyboardEventArgs(false, 0));
        }

        private InputMethodManager? InputMethodManager()
            => _view.Context?.GetSystemService(Context.InputMethodService) as InputMethodManager;
    }
}
#endif
