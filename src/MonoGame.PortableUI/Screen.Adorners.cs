using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI
{
    public abstract partial class Screen
    {
        private AdornerCollection? _adorners;
        private TextSelectionLayer? _textSelection;

        // The adorner that took the current press; it gets the gesture until the pointer lifts.
        private Adorner? _pointerAdorner;

        /// <summary>
        ///     Non-modal layers drawn above the content and below popups, bottom to top (see <see cref="Adorner"/>).
        /// </summary>
        public AdornerCollection Adorners => _adorners ??= new AdornerCollection(this);

        /// <summary>The touch text selection (handles and toolbar), created on first use; always on top of the adorners.</summary>
        internal TextSelectionLayer TextSelection
        {
            get
            {
                if (_textSelection == null)
                {
                    _textSelection = new TextSelectionLayer();
                    AttachAdorner(_textSelection);
                }
                return _textSelection;
            }
        }

        /// <summary>The text selection layer if one was ever needed (does not create it).</summary>
        internal TextSelectionLayer? ExistingTextSelection => _textSelection;

        internal void AttachAdorner(Adorner adorner)
        {
            adorner.Parent = this;
            adorner.UpdateLayout(ScreenRect);
            InvalidateLayout(false);
        }

        internal void DetachAdorner(Adorner adorner)
        {
            if (ReferenceEquals(_pointerAdorner, adorner))
                _pointerAdorner = null;
            adorner.Parent = null;
            InvalidateLayout(false);
        }

        private int AdornerCount => (_adorners?.Count ?? 0) + (_textSelection != null ? 1 : 0);

        private Adorner GetAdorner(int index)
            => _adorners != null && index < _adorners.Count ? _adorners[index] : _textSelection!;

        private void LayoutAdorners()
        {
            for (var i = 0; i < AdornerCount; i++)
                GetAdorner(i).UpdateLayout(ScreenRect);
        }

        private void UpdateAdorners()
        {
            for (var i = 0; i < AdornerCount; i++)
            {
                var adorner = GetAdorner(i);
                adorner.OnUpdate();
                UpdateTimersForTree(adorner);
            }
        }

        private void DrawAdorners(SpriteBatch spriteBatch)
        {
            for (var i = 0; i < AdornerCount; i++)
            {
                var adorner = GetAdorner(i);
                if (adorner.IsVisible)
                    DrawControlTree(spriteBatch, adorner, GetOverlayScissor(adorner));
            }
        }

        private void RefreshAdorners(bool theme)
        {
            for (var i = 0; i < AdornerCount; i++)
            {
                if (theme)
                    RefreshThemeForTree(GetAdorner(i));
                else
                    RefreshTextScaleForTree(GetAdorner(i));
            }
        }

        /// <summary>
        ///     Decides at each new press whether an adorner takes it (topmost first); the others hear about a
        ///     press outside. Returns the adorner that owns the current gesture, or null for the page.
        /// </summary>
        private Adorner? RouteAdornerPress(bool hasTouch, TouchLocationState touchState, PointF touchPosition, PointF mousePosition, System.Collections.Generic.IReadOnlyCollection<MouseButton> pressedMouseButtons)
        {
            if (AdornerCount == 0)
                return null;
            var leftDown = GetButtonState(pressedMouseButtons, MouseButton.Left) == ButtonState.Pressed;
            var touchPressed = hasTouch && touchState == TouchLocationState.Pressed;
            var mousePressed = leftDown && MouseButtonStates[MouseButton.Left] == ButtonState.Released;
            if (touchPressed || mousePressed)
            {
                var point = touchPressed ? touchPosition : mousePosition;
                _pointerAdorner = null;
                for (var i = AdornerCount - 1; i >= 0; i--)
                {
                    var adorner = GetAdorner(i);
                    if (adorner.IsVisible && adorner.HitTest(point))
                    {
                        _pointerAdorner = adorner;
                        break;
                    }
                }
                for (var i = 0; i < AdornerCount; i++)
                {
                    var adorner = GetAdorner(i);
                    if (!ReferenceEquals(adorner, _pointerAdorner))
                        adorner.OnPressOutside(point);
                }
            }
            else if (!hasTouch && !leftDown && MouseButtonStates[MouseButton.Left] == ButtonState.Released)
            {
                // The release frame still went to the adorner; the gesture is over now.
                _pointerAdorner = null;
            }
            return _pointerAdorner;
        }
    }
}
