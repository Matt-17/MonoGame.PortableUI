namespace MonoGame.PortableUI.Controls
{
    public abstract class UIElement : FrameworkElement
    {
        private bool _isGone;
        private bool _isVisible;

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                if (!_isVisible && this is Control control && MonoGame.PortableUI.ScreenEngine.For(control) is { } engine && engine.FocusedControl == control)
                    engine.FocusedControl = null;
                InvalidateLayout(false);
            }
        }

        public bool IsGone
        {
            get { return _isGone; }
            set
            {
                _isGone = value;
                if (_isGone && this is Control control && MonoGame.PortableUI.ScreenEngine.For(control) is { } engine && engine.FocusedControl == control)
                    engine.FocusedControl = null;
                InvalidateLayout(true);
            }
        }
    }
}
