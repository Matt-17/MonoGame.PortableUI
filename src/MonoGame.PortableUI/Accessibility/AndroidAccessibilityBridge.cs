#if ANDROID
using System;
using System.Collections.Generic;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Accessibility
{
    /// <summary>
    ///     Exposes the PortableUI control tree to TalkBack: one virtual view per
    ///     <see cref="AccessibilityNode"/>, served by a platform <see cref="AccessibilityNodeProvider"/>
    ///     on the game view (no AndroidX dependency). Explore-by-touch, swipe navigation, double-tap
    ///     activation, slider steps and live-region announcements are mapped.
    /// </summary>
    /// <remarks>
    ///     <code>engine.AccessibilityBridge = new AndroidAccessibilityBridge(gameView, engine);</code>
    ///     The game view is assumed to fill the window (window pixels = view pixels).
    /// </remarks>
    public sealed class AndroidAccessibilityBridge : View.AccessibilityDelegate, IAccessibilityBridge
    {
        private const int HostId = -1; // View.NO_ID: the host view itself

        private readonly View _view;
        private readonly ScreenEngine _engine;
        private readonly Provider _provider;
        private volatile bool _active;
        private volatile IReadOnlyList<AccessibilityNode> _nodes = Array.Empty<AccessibilityNode>();
        private int _accessibilityFocusId = int.MinValue;
        private int _hoverId = int.MinValue;

        public AndroidAccessibilityBridge(View gameView, ScreenEngine engine)
        {
            _view = gameView ?? throw new ArgumentNullException(nameof(gameView));
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _provider = new Provider(this);

            var manager = _view.Context?.GetSystemService(Context.AccessibilityService) as AccessibilityManager;
            if (manager != null)
            {
                _active = manager.IsEnabled && manager.IsTouchExplorationEnabled;
                manager.AddTouchExplorationStateChangeListener(new TouchExplorationListener(this));
            }

            _view.SetAccessibilityDelegate(this);
            _view.SetOnHoverListener(new HoverListener(this));
        }

        public bool IsScreenReaderActive => _active;

        public override AccessibilityNodeProvider? GetAccessibilityNodeProvider(View host) => _provider;

        void IAccessibilityBridge.OnTreeChanged(IReadOnlyList<AccessibilityNode> nodes)
        {
            _nodes = nodes;
            _view.Post(() => SendEvent(HostId, EventTypes.WindowContentChanged, null));
        }

        void IAccessibilityBridge.OnFocusChanged(AccessibilityNode? node)
        {
            if (node == null)
                return;
            var id = node.Id;
            var text = Speak(node);
            _view.Post(() =>
            {
                _accessibilityFocusId = id;
                SendEvent(id, EventTypes.ViewAccessibilityFocused, text);
            });
        }

        void IAccessibilityBridge.OnLiveRegionChanged(AccessibilityNode node)
        {
            var text = Speak(node);
            _view.Post(() => SendEvent(node.Id, EventTypes.Announcement, text));
        }

        private AccessibilityNode? Find(int id)
        {
            foreach (var node in _nodes)
            {
                if (node.Id == id)
                    return node;
            }
            return null;
        }

        /// <summary>The smallest exposed node under a view-local pixel position.</summary>
        private AccessibilityNode? HitTest(float x, float y)
        {
            var point = _engine.WindowToLayout(new PointF(x, y));
            AccessibilityNode? best = null;
            foreach (var node in _nodes)
            {
                var b = node.Bounds;
                if (!b.Contains(point))
                    continue;
                if (best == null || b.Width * b.Height <= best.Bounds.Width * best.Bounds.Height)
                    best = node;
            }
            return best;
        }

        private static string Speak(AccessibilityNode node)
            => string.IsNullOrEmpty(node.Value) ? node.Label : node.Label + ", " + node.Value;

        private void SendEvent(int id, EventTypes type, string? text)
        {
            if (!_active || _view.Parent == null)
                return;
            var ev = CreateEvent(type);
            ev.PackageName = _view.Context?.PackageName;
            if (id == HostId)
            {
                ev.SetSource(_view);
            }
            else
            {
                ev.SetSource(_view, id);
                if (Find(id) is { } node)
                    ev.ClassName = ClassName(node.Role);
            }
            if (!string.IsNullOrEmpty(text))
                ev.Text?.Add(new Java.Lang.String(text));
            _view.Parent.RequestSendAccessibilityEvent(_view, ev);
        }

        private static AccessibilityEvent CreateEvent(EventTypes type)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
                return new AccessibilityEvent((int)type);
#pragma warning disable CA1422 // obtain() is the only constructor before API 30
            return AccessibilityEvent.Obtain(type)!;
#pragma warning restore CA1422
        }

        private static string ClassName(AccessibilityRole role) => role switch
        {
            AccessibilityRole.Button or AccessibilityRole.Link or AccessibilityRole.Tab or AccessibilityRole.ListItem => "android.widget.Button",
            AccessibilityRole.CheckBox => "android.widget.CheckBox",
            AccessibilityRole.RadioButton => "android.widget.RadioButton",
            AccessibilityRole.ToggleButton => "android.widget.ToggleButton",
            AccessibilityRole.Switch => "android.widget.Switch",
            AccessibilityRole.Slider => "android.widget.SeekBar",
            AccessibilityRole.ProgressBar => "android.widget.ProgressBar",
            AccessibilityRole.TextField => "android.widget.EditText",
            AccessibilityRole.ComboBox => "android.widget.Spinner",
            AccessibilityRole.List => "android.widget.ListView",
            AccessibilityRole.TabList => "android.widget.TabWidget",
            AccessibilityRole.Image => "android.widget.ImageView",
            _ => "android.widget.TextView"
        };

        private sealed class Provider : AccessibilityNodeProvider
        {
            private readonly AndroidAccessibilityBridge _bridge;

            public Provider(AndroidAccessibilityBridge bridge)
            {
                _bridge = bridge;
            }

            public override AccessibilityNodeInfo? CreateAccessibilityNodeInfo(int virtualViewId)
            {
                var view = _bridge._view;
                if (virtualViewId == HostId)
                {
                    var host = Obtain(view, null);
                    view.OnInitializeAccessibilityNodeInfo(host);
                    foreach (var node in _bridge._nodes)
                        host.AddChild(view, node.Id);
                    return host;
                }

                var source = _bridge.Find(virtualViewId);
                if (source == null)
                    return null;

                var info = Obtain(view, virtualViewId);
                info.SetSource(view, virtualViewId);
                info.SetParent(view);
                info.PackageName = view.Context?.PackageName;
                info.ClassName = ClassName(source.Role);
                info.Text = Speak(source);
                info.ContentDescription = string.IsNullOrEmpty(source.Hint) ? null : Speak(source) + ". " + source.Hint;
                info.Enabled = !source.Has(AccessibilityStates.Disabled);
                info.Checkable = source.Role is AccessibilityRole.CheckBox or AccessibilityRole.RadioButton or AccessibilityRole.ToggleButton or AccessibilityRole.Switch;
#pragma warning disable CA1422 // the int-state replacement only exists from API 36
                info.Checked = source.Has(AccessibilityStates.Checked);
#pragma warning restore CA1422
                info.Selected = source.Has(AccessibilityStates.Selected);
                info.Password = source.Has(AccessibilityStates.Password);
                info.Focusable = (source.Actions & AccessibilityActions.Focus) != 0;
                info.Focused = source.Has(AccessibilityStates.Focused);
                info.Clickable = (source.Actions & AccessibilityActions.Activate) != 0;
                info.VisibleToUser = true;
                info.AccessibilityFocused = _bridge._accessibilityFocusId == virtualViewId;

                var location = new int[2];
                view.GetLocationOnScreen(location);
                var window = _bridge._engine.LayoutToWindow(source.Bounds);
                info.SetBoundsInScreen(new Android.Graphics.Rect(
                    location[0] + (int)window.Left, location[1] + (int)window.Top,
                    location[0] + (int)window.Right, location[1] + (int)window.Bottom));

                info.AddAction(_bridge._accessibilityFocusId == virtualViewId
                    ? AccessibilityNodeInfo.AccessibilityAction.ActionClearAccessibilityFocus
                    : AccessibilityNodeInfo.AccessibilityAction.ActionAccessibilityFocus);
                if (info.Clickable)
                    info.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionClick);
                if ((source.Actions & AccessibilityActions.Increment) != 0)
                    info.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionScrollForward);
                if ((source.Actions & AccessibilityActions.Decrement) != 0)
                    info.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionScrollBackward);
                if (info.Focusable)
                    info.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionFocus);
                return info;
            }

            public override bool PerformAction(int virtualViewId, Android.Views.Accessibility.Action action, Bundle? arguments)
            {
                if (virtualViewId == HostId)
                    return _bridge._view.PerformAccessibilityAction(action, arguments);
                var node = _bridge.Find(virtualViewId);
                if (node == null)
                    return false;

                switch (action)
                {
                    case Android.Views.Accessibility.Action.AccessibilityFocus:
                        _bridge._accessibilityFocusId = virtualViewId;
                        _bridge.SendEvent(virtualViewId, EventTypes.ViewAccessibilityFocused, Speak(node));
                        return true;
                    case Android.Views.Accessibility.Action.ClearAccessibilityFocus:
                        if (_bridge._accessibilityFocusId == virtualViewId)
                            _bridge._accessibilityFocusId = int.MinValue;
                        _bridge.SendEvent(virtualViewId, EventTypes.ViewAccessibilityFocusCleared, null);
                        return true;
                    case Android.Views.Accessibility.Action.Click:
                        return Run(node, AccessibilityActions.Activate, EventTypes.ViewClicked);
                    case Android.Views.Accessibility.Action.ScrollForward:
                        return Run(node, AccessibilityActions.Increment, EventTypes.ViewScrolled);
                    case Android.Views.Accessibility.Action.ScrollBackward:
                        return Run(node, AccessibilityActions.Decrement, EventTypes.ViewScrolled);
                    case Android.Views.Accessibility.Action.Focus:
                        return Run(node, AccessibilityActions.Focus, EventTypes.ViewFocused);
                    default:
                        return false;
                }
            }

            private bool Run(AccessibilityNode node, AccessibilityActions action, EventTypes feedback)
            {
                if ((node.Actions & action) == 0)
                    return false;
                var control = node.Control;
                _bridge._engine.InvokeOnGameThread(() => AccessibilityTree.PerformAction(control, action));
                _bridge.SendEvent(node.Id, feedback, null);
                return true;
            }

            private static AccessibilityNodeInfo Obtain(View view, int? virtualViewId)
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(30))
                    return virtualViewId is { } id ? new AccessibilityNodeInfo(view, id) : new AccessibilityNodeInfo(view);
#pragma warning disable CA1422 // obtain() before API 30
                return virtualViewId is { } legacyId ? AccessibilityNodeInfo.Obtain(view, legacyId)! : AccessibilityNodeInfo.Obtain(view)!;
#pragma warning restore CA1422
            }
        }

        private sealed class HoverListener : Java.Lang.Object, View.IOnHoverListener
        {
            private readonly AndroidAccessibilityBridge _bridge;

            public HoverListener(AndroidAccessibilityBridge bridge)
            {
                _bridge = bridge;
            }

            public bool OnHover(View? v, MotionEvent? e)
            {
                if (!_bridge._active || e == null)
                    return false;
                var id = _bridge.HitTest(e.GetX(), e.GetY())?.Id ?? int.MinValue;
                if (e.Action == MotionEventActions.HoverExit)
                    id = int.MinValue;
                if (id == _bridge._hoverId)
                    return true;
                if (_bridge._hoverId != int.MinValue)
                    _bridge.SendEvent(_bridge._hoverId, EventTypes.ViewHoverExit, null);
                _bridge._hoverId = id;
                if (id != int.MinValue)
                    _bridge.SendEvent(id, EventTypes.ViewHoverEnter, null);
                return true;
            }
        }

        private sealed class TouchExplorationListener : Java.Lang.Object, AccessibilityManager.ITouchExplorationStateChangeListener
        {
            private readonly AndroidAccessibilityBridge _bridge;

            public TouchExplorationListener(AndroidAccessibilityBridge bridge)
            {
                _bridge = bridge;
            }

            public void OnTouchExplorationStateChanged(bool enabled) => _bridge._active = enabled;
        }
    }
}
#endif
