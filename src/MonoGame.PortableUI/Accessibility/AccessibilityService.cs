using System;
using System.Collections.Generic;

namespace MonoGame.PortableUI.Accessibility
{
    /// <summary>
    ///     A platform screen-reader integration (Android: <c>AndroidAccessibilityBridge</c>). Set it on
    ///     <see cref="ScreenEngine.AccessibilityBridge"/>. Callbacks come on the game thread with an
    ///     immutable snapshot the platform may read from its own thread.
    /// </summary>
    public interface IAccessibilityBridge
    {
        /// <summary>
        ///     Whether a screen reader is running. While false the engine builds no tree at all, so the
        ///     bridge costs one property read per frame.
        /// </summary>
        bool IsScreenReaderActive { get; }

        /// <summary>The set of nodes, their text, state or bounds changed.</summary>
        void OnTreeChanged(IReadOnlyList<AccessibilityNode> nodes);

        /// <summary>Keyboard/gamepad focus moved to <paramref name="node"/> (null: no focused node).</summary>
        void OnFocusChanged(AccessibilityNode? node);

        /// <summary>A live-region node's text changed and should be announced.</summary>
        void OnLiveRegionChanged(AccessibilityNode node);
    }

    /// <summary>Diffs the active screen's accessibility tree once per update while a reader is active.</summary>
    internal sealed class AccessibilityService
    {
        private IReadOnlyList<AccessibilityNode> _nodes = Array.Empty<AccessibilityNode>();
        private readonly Dictionary<int, string> _liveText = new Dictionary<int, string>();
        private int _focusedId;
        private Screen? _screen;

        public IReadOnlyList<AccessibilityNode> Nodes => _nodes;

        public void Update(Screen? screen, IAccessibilityBridge bridge)
        {
            var nodes = screen != null ? AccessibilityTree.Build(screen) : Array.Empty<AccessibilityNode>();
            var screenChanged = !ReferenceEquals(screen, _screen);
            _screen = screen;

            if (screenChanged || !Same(nodes, _nodes))
            {
                // Live regions announce after the first snapshot only.
                foreach (var node in nodes)
                {
                    if (!node.IsLiveRegion)
                        continue;
                    var text = node.Label + node.Value;
                    if (!screenChanged && _liveText.TryGetValue(node.Id, out var previous) && previous != text)
                        bridge.OnLiveRegionChanged(node);
                    _liveText[node.Id] = text;
                }
                _nodes = nodes;
                bridge.OnTreeChanged(nodes);
            }

            AccessibilityNode? focused = null;
            foreach (var node in nodes)
            {
                if (node.Has(AccessibilityStates.Focused))
                {
                    focused = node;
                    break;
                }
            }
            var focusedId = focused?.Id ?? 0;
            if (focusedId != _focusedId)
            {
                _focusedId = focusedId;
                bridge.OnFocusChanged(focused);
            }
        }

        public void Reset()
        {
            _nodes = Array.Empty<AccessibilityNode>();
            _liveText.Clear();
            _focusedId = 0;
            _screen = null;
        }

        private static bool Same(IReadOnlyList<AccessibilityNode> a, IReadOnlyList<AccessibilityNode> b)
        {
            if (a.Count != b.Count)
                return false;
            for (var i = 0; i < a.Count; i++)
            {
                if (!a[i].SameAs(b[i]))
                    return false;
            }
            return true;
        }
    }
}
