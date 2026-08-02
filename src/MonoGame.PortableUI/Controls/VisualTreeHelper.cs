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

            foreach (var descendant in content.GetDescendants())
                AppendVisualTree(descendant, result, addTreeWhichIsGone);

            result.Add(content);
        }

        /// <summary>Input-routing walk: skips subtrees that are gone, invisible, disabled or hit-test invisible.</summary>
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
            foreach (var descendant in control.GetDescendants())
            {
                IterateVisualTree(descendant, args, actionFunc, action, treeFunc);
                if (args.Handled)
                    return;
            }
            if (actionAppliesToControl)
                action(control, args);
        }

    }
}
