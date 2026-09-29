using System;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Controls
{
    public class MenuItem
    {
        public MenuItem(string text, Action? action)
        {
            Text = text;
            Action = action;
        }

        public MenuItem()
        {
        }

        /// <summary>A thin divider line between groups of items.</summary>
        public static MenuItem Separator() => new MenuItem { IsSeparator = true };

        public string Text { get; set; } = "";
        public Action? Action { get; set; }

        public bool IsSeparator { get; set; }

        /// <summary>Optional icon drawn left of the text (about 16×16).</summary>
        public Texture2D? Icon { get; set; }

        /// <summary>Hint shown right-aligned, e.g. "Ctrl+C". Display only; it does not bind the key.</summary>
        public string? ShortcutText { get; set; }

        /// <summary>Disabled items are shown dimmed and cannot be invoked.</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>Invoking a checkable item toggles <see cref="IsChecked"/> before running <see cref="Action"/>.</summary>
        public bool IsCheckable { get; set; }

        public bool IsChecked { get; set; }

        /// <summary>Child items: invoking this item opens them in place (drill-down) with a back row.</summary>
        public MenuItemList Items { get; } = new MenuItemList();

        public bool HasSubmenu => Items.Count > 0;
    }
}
