#if ANDROID
using System;
using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
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

        public AndroidOnScreenKeyboard(View gameView)
        {
            _view = gameView ?? throw new ArgumentNullException(nameof(gameView));
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
