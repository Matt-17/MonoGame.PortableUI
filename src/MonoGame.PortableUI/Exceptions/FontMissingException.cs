using System;

namespace MonoGame.PortableUI.Exceptions
{
    public class FontMissingException : Exception
    {
        public FontMissingException(string message) : base($"Could not load font: {message}")
        {
        }
    }
}