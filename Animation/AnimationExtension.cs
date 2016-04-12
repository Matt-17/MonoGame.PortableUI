using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Animation
{
    public static class AnimationExtension
    {
        public static Animation Animate(this UIControl uiControl)
        {
            return new Animation(uiControl);
        }
    }
}