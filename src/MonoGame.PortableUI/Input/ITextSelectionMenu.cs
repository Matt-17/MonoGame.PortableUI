using System;
using System.Collections.Generic;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Input
{
    /// <summary>An entry of the touch text selection menu.</summary>
    public enum TextSelectionAction
    {
        Cut,
        Copy,
        Paste,
        SelectAll
    }

    /// <summary>What a <see cref="ITextSelectionMenu"/> shows, and where.</summary>
    public sealed class TextSelectionMenuRequest
    {
        /// <summary>The selection or caret (with its handles) the menu stands next to, in window pixels.</summary>
        public Rect ContentRect { get; init; }

        /// <summary>The text box's own entries, in Android's order.</summary>
        public IReadOnlyList<TextSelectionAction> Actions { get; init; } = Array.Empty<TextSelectionAction>();

        /// <summary>The selected text for platform entries (share, translate); null when there is none or it is secret.</summary>
        public string? SelectedText { get; init; }

        /// <summary>Runs an entry. Safe to call from the platform's UI thread.</summary>
        public Action<TextSelectionAction> Invoke { get; init; } = _ => { };

        /// <summary>The platform closed the menu on its own (back key, a platform entry). Safe from any thread.</summary>
        public Action Dismissed { get; init; } = () => { };
    }

    /// <summary>
    ///     A platform's own floating text menu (Android's floating action mode: native look, animation, overflow,
    ///     Share and the installed text actions such as Translate) for <see cref="ScreenEngineOptions.TextSelectionMenu"/>.
    ///     Without one, PortableUI draws its own bar.
    /// </summary>
    public interface ITextSelectionMenu
    {
        /// <summary>Opens the menu, or updates it (entries, position) while open. Called on the game thread.</summary>
        void Show(TextSelectionMenuRequest request);

        /// <summary>Closes the menu. Called on the game thread.</summary>
        void Hide();

        /// <summary>
        ///     A selection handle is being dragged: hide the menu for a moment without closing it (called again while
        ///     the drag goes on); the next <see cref="Show"/> brings it back. Called on the game thread.
        /// </summary>
        void Suspend();
    }
}
