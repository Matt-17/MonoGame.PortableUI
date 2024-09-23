using System;

namespace MonoGame.PortableUI.Exceptions
{
    public class DefaultFontMissingException : Exception
    {
        public DefaultFontMissingException() : base("Default font is not set")
        {
            
        }
    }
}