using System;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Accessibility
{
    /// <summary>What a control is, in screen-reader terms (mapped to Android class names / iOS traits).</summary>
    public enum AccessibilityRole
    {
        /// <summary>Not exposed itself (layout containers); its children still are.</summary>
        None,
        Text,
        Heading,
        Button,
        CheckBox,
        RadioButton,
        ToggleButton,
        Switch,
        Slider,
        ProgressBar,
        TextField,
        ComboBox,
        List,
        ListItem,
        TabList,
        Tab,
        Image,
        Link
    }

    [Flags]
    public enum AccessibilityStates
    {
        None = 0,
        Disabled = 1,
        Checked = 2,
        Selected = 4,
        Focused = 8,
        Expanded = 16,
        Password = 32,
        Busy = 64
    }

    [Flags]
    public enum AccessibilityActions
    {
        None = 0,
        /// <summary>Click / double-tap.</summary>
        Activate = 1,
        Increment = 2,
        Decrement = 4,
        Focus = 8
    }

    /// <summary>
    ///     Per-control overrides of the derived description (<see cref="Control.Accessibility"/>).
    ///     Unset values fall back to what the control type implies.
    /// </summary>
    public sealed class AccessibilityProperties
    {
        /// <summary>What the reader says for the control (e.g. for an icon-only button).</summary>
        public string? Label { get; set; }

        /// <summary>Extra hint read after the label ("Double-tap to open the inventory").</summary>
        public string? Hint { get; set; }

        public AccessibilityRole? Role { get; set; }

        /// <summary>True hides the control and its subtree from the reader; false forces it visible
        /// (e.g. an image that carries meaning); null uses the default.</summary>
        public bool? IsHidden { get; set; }

        /// <summary>Text that changes without interaction (score, timer, status) is announced when it does.</summary>
        public bool IsLiveRegion { get; set; }
    }

    /// <summary>One element of the platform-neutral accessibility tree (a snapshot; safe to read on any thread).</summary>
    public sealed class AccessibilityNode
    {
        internal AccessibilityNode(Control control, int id, AccessibilityRole role, string label, string? value, string? hint,
            AccessibilityStates states, AccessibilityActions actions, Rect bounds, bool isLiveRegion)
        {
            Control = control;
            Id = id;
            Role = role;
            Label = label;
            Value = value;
            Hint = hint;
            States = states;
            Actions = actions;
            Bounds = bounds;
            IsLiveRegion = isLiveRegion;
        }

        /// <summary>The control; only touch it on the game thread.</summary>
        public Control Control { get; }

        /// <summary>Stable for the control's lifetime; usable as a platform virtual view id.</summary>
        public int Id { get; }

        public AccessibilityRole Role { get; }
        public string Label { get; }
        public string? Value { get; }
        public string? Hint { get; }
        public AccessibilityStates States { get; }
        public AccessibilityActions Actions { get; }

        /// <summary>Bounds in layout units (see <see cref="ScreenEngine.LayoutToWindow"/> for window pixels).</summary>
        public Rect Bounds { get; }

        public bool IsLiveRegion { get; }

        public bool Has(AccessibilityStates state) => (States & state) != 0;

        public override string ToString() => $"{Role} \"{Label}\"" + (Value != null ? $" = {Value}" : "") + (States != AccessibilityStates.None ? $" [{States}]" : "");

        internal bool SameAs(AccessibilityNode other)
            => Id == other.Id && Role == other.Role && Label == other.Label && Value == other.Value
               && States == other.States && Bounds.Equals(other.Bounds);
    }
}
