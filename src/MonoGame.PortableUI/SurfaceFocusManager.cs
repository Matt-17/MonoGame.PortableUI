namespace MonoGame.PortableUI
{
    public sealed class SurfaceFocusManager
    {
        public UISurface? ActiveSurface { get; private set; }

        public void Activate(UISurface? surface)
        {
            if (ActiveSurface == surface)
                return;

            if (ActiveSurface != null)
                ActiveSurface.HasKeyboardFocus = false;

            ActiveSurface = surface;

            if (ActiveSurface != null)
                ActiveSurface.HasKeyboardFocus = true;
        }

        public void RouteTextInput(char character)
        {
            if (ActiveSurface?.HasKeyboardFocus == true)
                ActiveSurface.Screen.HandleTextInput(character);
        }
    }
}
