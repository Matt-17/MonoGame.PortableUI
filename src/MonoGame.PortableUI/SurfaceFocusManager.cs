using System;

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

        /// <summary>
        ///     For platforms without window text input (Android IME, custom keyboards): forwards one
        ///     character to the active surface. On desktop the window's text input already reaches
        ///     the focused surface by itself, so this does nothing there (it used to type twice).
        /// </summary>
        public void RouteTextInput(char character)
        {
#if !ANDROID
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                return;
#endif
            if (ActiveSurface?.HasKeyboardFocus == true)
                ActiveSurface.Engine.HandleTextInput(character);
        }
    }
}
