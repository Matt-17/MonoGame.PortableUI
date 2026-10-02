using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public enum ModalPlacement
    {
        /// <summary>A panel centred on the screen.</summary>
        Center,
        /// <summary>A sheet anchored to the bottom edge, <see cref="ModalOptions.SheetHeight"/> tall.</summary>
        BottomSheet
    }

    /// <summary>How a modal is presented and how it may be dismissed.</summary>
    public sealed class ModalOptions
    {
        public ModalPlacement Placement { get; set; } = ModalPlacement.Center;

        /// <summary>Height of a <see cref="ModalPlacement.BottomSheet"/>; NaN sizes it to its content.</summary>
        public float SheetHeight { get; set; } = float.NaN;

        /// <summary>Width of a centred panel; NaN sizes it to its content.</summary>
        public float PanelWidth { get; set; } = float.NaN;

        /// <summary>When false, back/Escape/B and scrim taps do not close the modal; only <see cref="ModalScreen.Close"/> does.</summary>
        public bool Cancelable { get; set; } = true;

        /// <summary>Close (as cancelled) when the scrim outside the panel is tapped. Requires <see cref="Cancelable"/>.</summary>
        public bool DismissOnScrimTap { get; set; } = true;

        /// <summary>Dims the screen behind the modal; null leaves it undimmed.</summary>
        public Brush? ScrimBrush { get; set; } = new SolidColorBrush(new Color(0, 0, 0, 140));

        /// <summary>Panel background; null uses the theme's panel background.</summary>
        public Brush? PanelBackground { get; set; }

        public float CornerRadius { get; set; } = 12;

        public Thickness Padding { get; set; } = new Thickness(16);

        /// <summary>Null picks Fade for panels and SlideFromBottom for sheets.</summary>
        public ScreenTransition? Transition { get; set; }
    }

    public sealed class ModalClosedEventArgs : EventArgs
    {
        public ModalClosedEventArgs(bool cancelled)
        {
            Cancelled = cancelled;
        }

        /// <summary>True when closed by back/Escape/B or a scrim tap rather than by <see cref="ModalScreen.Close"/>.</summary>
        public bool Cancelled { get; }
    }

    /// <summary>
    ///     A modal pushed as an overlay screen: everything behind it stays visible under a scrim but
    ///     receives no input, keyboard/gamepad focus cannot leave it, and modals opened from a modal
    ///     stack on top. Open one with <see cref="ScreenEngine.ShowModal"/>.
    /// </summary>
    /// <remarks>
    ///     Close order: closing a modal first closes every modal stacked above it (top first), so
    ///     the stack never has holes. Each raises <see cref="Closed"/>.
    /// </remarks>
    public class ModalScreen : Screen
    {
        private readonly Border _scrim;
        private bool _closed;

        public ModalScreen(Control content, ModalOptions? options = null)
        {
            Options = options ?? new ModalOptions();
            var sheet = Options.Placement == ModalPlacement.BottomSheet;
            var theme = PortableTheme.ResolveCurrent();

            var background = Options.PanelBackground ?? theme.ModalBackgroundBrush ?? theme.Panel.Normal.Background ?? theme.ContextMenuBackgroundBrush;
            Panel = new Border
            {
                Content = content,
                Padding = Options.Padding,
                BackgroundBrush = background,
                Shadow = theme.ModalShadow,
                // A sheet keeps square bottom corners: it sits on the screen edge.
                CornerRadius = sheet
                    ? new CornerRadius(Options.CornerRadius, Options.CornerRadius, 0, 0)
                    : new CornerRadius(Options.CornerRadius),
                // Glass brushes draw their own rounded shape and must sample the backdrop at screen
                // coordinates — an offscreen clip layer would break both.
                ClipToCornerRadius = !background.RequiresBackdrop,
                HorizontalAlignment = sheet ? HorizontalAlignment.Stretch : HorizontalAlignment.Center,
                VerticalAlignment = sheet ? VerticalAlignment.Bottom : VerticalAlignment.Center,
                Width = sheet ? float.NaN : Options.PanelWidth,
                Height = sheet ? Options.SheetHeight : float.NaN
            };

            _scrim = new Border
            {
                Content = Panel,
                BackgroundBrush = Options.ScrimBrush,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _scrim.MouseDown += (_, args) => ScrimPressed(args.Position);
            _scrim.TouchDown += (_, args) => ScrimPressed(args.Position);
            Content = _scrim;

            BackRequested += (_, args) =>
            {
                args.Handled = true;
                if (Options.Cancelable)
                    CloseCore(cancelled: true);
            };
        }

        public ModalOptions Options { get; }

        /// <summary>The panel/sheet that hosts the content (rounded, clipped to its outline).</summary>
        public Border Panel { get; }

        public bool IsOpen => ScreenEngine != null && !_closed;

        public event EventHandler<ModalClosedEventArgs>? Closed;

        internal ScreenTransition PresentationTransition =>
            Options.Transition ?? (Options.Placement == ModalPlacement.BottomSheet ? ScreenTransition.SlideFromBottom : ScreenTransition.Fade);

        /// <summary>Closes this modal (and any modal stacked above it).</summary>
        public void Close() => CloseCore(cancelled: false);

        private void ScrimPressed(PointF position)
        {
            if (!Options.Cancelable || !Options.DismissOnScrimTap)
                return;
            if (Panel.ClippingRect.Contains(position))
                return;
            CloseCore(cancelled: true);
        }

        private void CloseCore(bool cancelled)
        {
            var engine = ScreenEngine;
            if (_closed || engine == null)
                return;

            // Modals above this one close first (top down), then this one.
            while (engine.ActiveScreen is { } top && !ReferenceEquals(top, this))
            {
                if (top is ModalScreen above)
                    above.CloseCore(cancelled);
                else
                    engine.NavigateBack();
            }

            if (!ReferenceEquals(engine.ActiveScreen, this))
                return;

            _closed = true;
            engine.NavigateBack(PresentationTransition);
            Closed?.Invoke(this, new ModalClosedEventArgs(cancelled));
        }

        protected override void OnNavigatedTo()
        {
            base.OnNavigatedTo();
            // Keyboard/gamepad users start inside the modal; focus can't leave it because only the
            // top screen's tree takes part in tab and spatial navigation.
            if (ScreenEngine?.KeyboardNavigationActive == true && ScreenEngine.FocusedControl == null)
                FocusNextTabStop();
        }
    }
}
