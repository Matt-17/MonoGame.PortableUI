using System;
using System.Collections.Generic;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public static class VisualTreeHelper
    {
        internal static IEnumerable<Control> GetVisualTreeAsList(Control content, bool addTreeWhichIsGone = true)
        {
            var stack = new Stack<VisualTreeFrame>();
            stack.Push(new VisualTreeFrame(content, false));

            while (stack.Count > 0)
            {
                var frame = stack.Pop();
                var control = frame.Control;
                if (control.IsGone && !addTreeWhichIsGone)
                    continue;

                if (frame.IsExpanded)
                {
                    yield return control;
                    continue;
                }

                stack.Push(new VisualTreeFrame(control, true));
                var descendants = new List<Control>(control.GetDescendants());
                for (var i = descendants.Count - 1; i >= 0; i--)
                    stack.Push(new VisualTreeFrame(descendants[i], false));
            }
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

        private readonly struct VisualTreeFrame
        {
            public VisualTreeFrame(Control control, bool isExpanded)
            {
                Control = control;
                IsExpanded = isExpanded;
            }

            public Control Control { get; }

            public bool IsExpanded { get; }
        }
    }
}
