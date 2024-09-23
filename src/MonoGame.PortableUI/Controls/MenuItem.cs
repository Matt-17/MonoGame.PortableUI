using System;

namespace MonoGame.PortableUI.Controls
{
    public class MenuItem
    {
        public MenuItem(string text, Action action)
        {
            Text = text;
            Action = action;
        }

        public MenuItem()
        {
        }

        public string Text { get; set; }
        public Action Action { get; set; }
    }
}