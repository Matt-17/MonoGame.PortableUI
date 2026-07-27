using System;
using System.Collections.Generic;
using System.Linq;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public static class VisualTreeHelper
    {
        internal static IEnumerable<Control> GetVisualTreeAsList(Control content, bool addTreeWhichIsGone = true)
        {
            if (content.IsGone && !addTreeWhichIsGone)
                yield break;
            var descendants = content.GetDescendants();
            foreach (var child in descendants.SelectMany(control => GetVisualTreeAsList(control, addTreeWhichIsGone)))
            {
                yield return child;
            }
            yield return content;
        }

        internal static void IterateVisualTree<T>(Control control, T args, Func<Control, T, bool> actionFunc, Action<Control, T> action, Func<Control, T, bool>? treeFunc) where T : BaseEventArgs
        {
            if (control.IsGone || !control.IsVisible || !control.IsEnabled)
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
