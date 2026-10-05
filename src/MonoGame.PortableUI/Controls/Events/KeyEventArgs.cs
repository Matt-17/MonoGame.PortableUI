using Microsoft.Xna.Framework.Input;

namespace MonoGame.PortableUI.Controls.Events
{
    public class KeyEventArgs
    {
        public KeyEventArgs(string function)
            : this(function, KeyboardModifiers.None)
        {
        }

        public KeyEventArgs(string function, KeyboardModifiers modifiers)
        {
            InputType = InputType.Function;
            Function = function;
            Modifiers = modifiers;
        }

        public KeyEventArgs(KeyboardCommand command)
            : this(command, KeyboardModifiers.None)
        {
        }

        public KeyEventArgs(KeyboardCommand command, KeyboardModifiers modifiers)
        {
            InputType = InputType.Command;
            Command = command;
            Modifiers = modifiers;
        }


        public KeyEventArgs(char key)
            : this(key, KeyboardModifiers.None)
        {
        }

        public KeyEventArgs(char key, KeyboardModifiers modifiers)
        {
            InputType = InputType.Char;
            Char = key;
            Modifiers = modifiers;
        }

        /// <summary>A physical key went down (<see cref="Control.KeyDown"/>, <see cref="Screen.KeyDown"/>)
        /// or up (<see cref="Control.KeyUp"/>): any key, F1-F12 and Alt/Ctrl chords included.</summary>
        public KeyEventArgs(Keys key, KeyboardModifiers modifiers, bool isRepeat = false)
        {
            InputType = InputType.Key;
            Key = key;
            Modifiers = modifiers;
            IsRepeat = isRepeat;
        }

        public InputType InputType { get; }
        public string? Function { get; }
        public char Char { get; }
        public KeyboardCommand Command { get; }
        public KeyboardModifiers Modifiers { get; }

        /// <summary>The physical key, for <see cref="InputType.Key"/>.</summary>
        public Keys Key { get; }

        /// <summary>True for the typematic repeats of a held key (after a 500 ms pause, every 45 ms).</summary>
        public bool IsRepeat { get; }

        /// <summary>
        ///     Set by a handler that consumed the key: it stops bubbling to the parents and the screen,
        ///     and the screen's own handling (Tab and arrow navigation, Escape, editing commands, the
        ///     debug overlay key) skips it.
        /// </summary>
        public bool Handled { get; set; }
    }
}
