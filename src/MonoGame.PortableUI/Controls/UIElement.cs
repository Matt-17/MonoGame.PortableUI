namespace MonoGame.PortableUI.Controls
{
    public abstract class UIElement : FrameworkElement
    {
        private bool _isGone;
        private bool _isVisible;
        // While an exit transition plays (Control.VisibilityTransition), layout and drawing keep the
        // control as it was; the properties already return the new state.
        private bool _exitHolds;

        /// <summary>
        ///     False hides the control (it keeps its space). With a <see cref="Control.VisibilityTransition"/>, hiding
        ///     plays the exit first and showing plays the enter; the property changes at once and input stops at once.
        /// </summary>
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                // Unchanged: nothing to do (hosts sync visibility every frame).
                if (_isVisible == value)
                    return;
                var wasShown = IsShownTarget;
                _isVisible = value;
                if (!_isVisible && this is Control control && MonoGame.PortableUI.ScreenEngine.For(control) is { } engine && engine.FocusedControl == control)
                    engine.FocusedControl = null;
                OnShownTargetChanged(wasShown);
                InvalidateLayout(false);
            }
        }

        /// <summary>
        ///     True removes the control from layout. With a <see cref="Control.VisibilityTransition"/>, the exit plays
        ///     first (the control keeps its space meanwhile) and showing plays the enter.
        /// </summary>
        public bool IsGone
        {
            get { return _isGone; }
            set
            {
                if (_isGone == value)
                    return;
                var wasShown = IsShownTarget;
                _isGone = value;
                if (_isGone && this is Control control && MonoGame.PortableUI.ScreenEngine.For(control) is { } engine && engine.FocusedControl == control)
                    engine.FocusedControl = null;
                OnShownTargetChanged(wasShown);
                InvalidateLayout(true);
            }
        }

        private bool IsShownTarget => _isVisible && !_isGone;

        /// <summary>What layout uses: gone, unless an exit transition still plays.</summary>
        internal bool IsGoneShown => _isGone && !_exitHolds;

        /// <summary>What drawing uses: visible, or still playing an exit transition.</summary>
        internal bool IsVisibleShown => _isVisible || _exitHolds;

        /// <summary>True while an exit transition keeps a hidden or gone control on screen.</summary>
        internal bool ExitHolds
        {
            get => _exitHolds;
            set
            {
                if (_exitHolds == value)
                    return;
                var collapses = _isGone;
                _exitHolds = value;
                InvalidateLayout(collapses);
            }
        }

        private void OnShownTargetChanged(bool wasShown)
        {
            var shown = IsShownTarget;
            if (shown != wasShown)
                OnShownChanged(shown);
        }

        /// <summary>The control turned shown (visible and not gone) or stopped being shown.</summary>
        internal virtual void OnShownChanged(bool shown)
        {
        }
    }
}
