namespace MonoGame.PortableUI.Controls
{
    public sealed class ThemeIsland : ContentControl
    {
        private PortableTheme? _theme;

        public PortableTheme? Theme
        {
            get { return _theme; }
            set
            {
                if (ReferenceEquals(_theme, value))
                    return;

                _theme = value;
                ThemeVersion.Next();
                InvalidateLayout(true);
            }
        }
    }
}
