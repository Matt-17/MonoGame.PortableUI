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

        // One scratch list per nesting level of the reverse walk; reused so routing stays allocation-free.
        [ThreadStatic]
        private static Stack<List<Control>>? _bufferPool;

        private static List<Control> RentBuffer()
        {
            var pool = _bufferPool ??= new Stack<List<Control>>();
            return pool.Count > 0 ? pool.Pop() : new List<Control>();
        }

        private static void ReturnBuffer(List<Control> buffer)
        {
            buffer.Clear();
            (_bufferPool ??= new Stack<List<Control>>()).Push(buffer);
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
            // Children are drawn in GetDescendants order, so the last one is topmost: hit-test
            // back to front so an overlapping sibling on top gets the event before the one below.
            var descendants = control.GetDescendants();
            if (descendants is IList<Control> list)
            {
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    // A handler may have removed children while we were routing.
                    if (i >= list.Count)
                        continue;
                    IterateVisualTree(list[i], args, actionFunc, action, treeFunc);
                    if (args.Handled)
                        return;
                }
            }
            else
            {
                var buffer = RentBuffer();
                try
                {
                    foreach (var descendant in descendants)
                        buffer.Add(descendant);
                    for (var i = buffer.Count - 1; i >= 0; i--)
                    {
                        IterateVisualTree(buffer[i], args, actionFunc, action, treeFunc);
                        if (args.Handled)
                            return;
                    }
                }
                finally
                {
                    ReturnBuffer(buffer);
                }
            }
            if (actionAppliesToControl)
                action(control, args);
        }

    }
}
