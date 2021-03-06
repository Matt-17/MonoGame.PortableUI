﻿using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Animation
{
    public static class AnimationExtension
    {
        public static Animation Animate(this Control _control)
        {
            return new Animation(_control);
        }
    }
}