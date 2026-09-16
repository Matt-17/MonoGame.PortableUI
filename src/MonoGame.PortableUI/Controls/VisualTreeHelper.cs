using System;
using System.Collections.Generic;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public static class VisualTreeHelper
    {
        internal static List<Control> GetVisualTreeAsList(Control content, bool addTreeWhichIsGone = true)
        {
            var result = new List<Control>();
            AppendVisualTree(content, result, addTreeWhichIsGone);
            return result;
        }

        internal static void AppendVisualTree(Control content, IList<Control> result, bool addTreeWhichIsGone = true)
        {
            if (content.IsGone && !addTreeWhichIsGone)
                return;

            var count = content.VisualChildCount;
            for (var i = 0; i < count; i++)
                AppendVisualTree(content.GetVisualChild(i), result, addTreeWhichIsGone);

            result.Add(content);
        }

        /// <summary>True when <paramref name="predicate"/> matches any control in the subtree
        /// (gone subtrees skipped). Allocation-free with a static lambda; stops at the first match.</summary>
        internal static bool Any<TState>(Control control, TState state, Func<Control, TState, bool> predicate)
        {
            if (control.IsGone)
                return false;
            if (predicate(control, state))
                return true;

            var count = control.VisualChildCount;
            for (var i = 0; i < count; i++)
            {
                if (Any(control.GetVisualChild(i), state, predicate))
                    return true;
            }
            return false;
        }

        /// <summary>Tunneling pre-pass for touch-down: notifies every routable control under the finger, ancestors first.</summary>
        internal static void PreviewTouchDown(Control control, TouchEventArgs args)
        {
            if (control.IsGone || !control.IsVisible || !control.IsEnabled || !control.IsHitTestVisible)
                return;
            if (!control.ClippingRect.Contains(args.Position))
                return;

            control.OnPreviewTouchDown(args);
            var count = control.VisualChildCount;
            for (var i = 0; i < count; i++)
                PreviewTouchDown(control.GetVisualChild(i), args);
        }

        /// <summary>Cancels pending touch presses in the subtree below <paramref name="control"/>.</summary>
        internal static void CancelDescendantTouches(Control control, TouchEventArgs args)
        {
            var count = control.VisualChildCount;
            for (var i = 0; i < count; i++)
            {
                var descendant = control.GetVisualChild(i);
                descendant.CancelPendingTouch(args);
                CancelDescendantTouches(descendant, args);
            }
        }

        /// <summary>Input-routing walk, topmost child first: skips subtrees that are gone, invisible, disabled or hit-test invisible.</summary>
        internal static void IterateVisualTree<T>(Control control, T args, Func<Control, T, bool> actionFunc, Action<Control, T> action, Func<Control, T, bool>? treeFunc) where T : BaseEventArgs
        {
            if (control.IsGone || !control.IsVisible || !control.IsEnabled || !control.IsHitTestVisible)
                return;
            var actionAppliesToControl = actionFunc(control, args);
            var goIntoTree = treeFunc?.Invoke(control, args) ?? actionAppliesToControl;
            if (!goIntoTree)
                return;
            if (control.CapturesInputBeforeDescendants(args) && actionAppliesToControl)
            {
                action(control, args);
                if (args.Handled)
                    return;
            }
            // Children are drawn in order, so the last one is topmost: hit-test back to front so
            // an overlapping sibling on top gets the event before the one below.
            for (var i = control.VisualChildCount - 1; i >= 0; i--)
            {
                // A handler may have removed children while we were routing.
                if (i >= control.VisualChildCount)
                    continue;
                IterateVisualTree(control.GetVisualChild(i), args, actionFunc, action, treeFunc);
                if (args.Handled)
                    return;
            }
            if (actionAppliesToControl)
                action(control, args);
        }

    }
}
