using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Accessibility
{
    /// <summary>
    ///     Maps a control tree onto the platform-neutral accessibility model: one
    ///     <see cref="AccessibilityNode"/> per element a screen reader should visit, in reading order.
    /// </summary>
    /// <remarks>
    ///     Reading order follows the keyboard/gamepad focus order: controls with an explicit
    ///     <see cref="Control.TabIndex"/> first (ascending), then the rest in visual-tree order. Layout
    ///     containers are not exposed; leaf roles (buttons, fields, ...) swallow their children, so a
    ///     button is read as one element with its caption.
    /// </remarks>
    public static class AccessibilityTree
    {
        private static int _nextId;

        /// <summary>The nodes of a screen (its content, or the open flyout) in reading order.</summary>
        public static IReadOnlyList<AccessibilityNode> Build(Screen screen)
        {
            if (screen == null)
                throw new ArgumentNullException(nameof(screen));
            return Build(screen.AccessibilityRoot);
        }

        public static IReadOnlyList<AccessibilityNode> Build(Control root)
        {
            var collected = new List<AccessibilityNode>();
            Collect(root, collected);
            // Stable: explicit TabIndex first, everything else keeps tree order.
            return collected
                .Select((node, sequence) => (node, sequence))
                .OrderBy(e => e.node.Control.TabIndex < 0 ? int.MaxValue : e.node.Control.TabIndex)
                .ThenBy(e => e.sequence)
                .Select(e => e.node)
                .ToList();
        }

        /// <summary>The description of a single control, or null when it is not exposed itself.</summary>
        public static AccessibilityNode? Describe(Control control)
        {
            if (control == null || control.IsGone || !control.IsVisible)
                return null;
            var overrides = control.AccessibilityIfCreated;
            if (overrides?.IsHidden == true)
                return null;

            var (role, label, value, states, actions) = Derive(control);
            if (overrides?.Role is { } roleOverride)
                role = roleOverride;
            if (!string.IsNullOrEmpty(overrides?.Label))
                label = overrides!.Label!;

            if (overrides?.IsHidden == false && role == AccessibilityRole.None)
                role = AccessibilityRole.Text;
            if (role == AccessibilityRole.None)
                return null;
            // Decorative: nothing to say and nothing to do.
            if (string.IsNullOrWhiteSpace(label) && string.IsNullOrEmpty(value) && overrides?.IsHidden != false
                && (role == AccessibilityRole.Text || role == AccessibilityRole.Image))
                return null;

            if (!control.IsEnabled)
            {
                states |= AccessibilityStates.Disabled;
                actions = AccessibilityActions.None;
            }
            if (ReferenceEquals(ScreenEngine.For(control)?.FocusedControl, control))
                states |= AccessibilityStates.Focused;
            if (control.IsFocusable && control.IsEnabled)
                actions |= AccessibilityActions.Focus;

            return new AccessibilityNode(control, GetId(control), role, label ?? "", value, overrides?.Hint, states, actions,
                control.ClippingRect, overrides?.IsLiveRegion ?? false);
        }

        /// <summary>Runs an action the way a screen reader requests it (call on the game thread).</summary>
        public static bool PerformAction(Control control, AccessibilityActions action)
        {
            if (control == null || !control.IsEnabled)
                return false;
            switch (action)
            {
                case AccessibilityActions.Activate:
                    control.OnClick();
                    return true;
                case AccessibilityActions.Increment when control is Slider slider:
                    slider.Value += slider.SmallChange;
                    return true;
                case AccessibilityActions.Decrement when control is Slider slider:
                    slider.Value -= slider.SmallChange;
                    return true;
                case AccessibilityActions.Focus when control.IsFocusable:
                    control.Focus();
                    return true;
                default:
                    return false;
            }
        }

        internal static int GetId(Control control)
        {
            if (control.AccessibilityId == 0)
                control.AccessibilityId = System.Threading.Interlocked.Increment(ref _nextId);
            return control.AccessibilityId;
        }

        private static void Collect(Control control, List<AccessibilityNode> into)
        {
            if (control.IsGone || !control.IsVisible || control.AccessibilityIfCreated?.IsHidden == true)
                return;

            var node = Describe(control);
            if (node != null)
            {
                into.Add(node);
                if (IsLeaf(node.Role))
                    return;
            }

            var count = control.VisualChildCount;
            for (var i = 0; i < count; i++)
                Collect(control.GetVisualChild(i), into);
        }

        private static bool IsLeaf(AccessibilityRole role) => role != AccessibilityRole.List && role != AccessibilityRole.TabList;

        private static (AccessibilityRole Role, string? Label, string? Value, AccessibilityStates States, AccessibilityActions Actions) Derive(Control control)
        {
            switch (control)
            {
                case TextBox textBox:
                    var shown = textBox.PasswordChar != default ? new string('•', textBox.Text.Length) : textBox.Text;
                    return (AccessibilityRole.TextField, textBox.ToolTip ?? textBox.HintText, shown,
                        textBox.PasswordChar != default ? AccessibilityStates.Password : AccessibilityStates.None, AccessibilityActions.Focus);
                case TextBlock textBlock:
                    return (textBlock.IsHeading ? AccessibilityRole.Heading : AccessibilityRole.Text, textBlock.Text, null, AccessibilityStates.None, AccessibilityActions.None);
                case ToggleSwitch toggle:
                    return (AccessibilityRole.Switch, CaptionOf(toggle), null, toggle.IsOn ? AccessibilityStates.Checked : AccessibilityStates.None, AccessibilityActions.Activate);
                case CheckBox checkBox:
                    return (AccessibilityRole.CheckBox, Prefer(checkBox.Text, CaptionOf(checkBox)), null, checkBox.IsChecked ? AccessibilityStates.Checked : AccessibilityStates.None, AccessibilityActions.Activate);
                case RadioButton radio:
                    return (AccessibilityRole.RadioButton, Prefer(radio.Text, CaptionOf(radio)), null, radio.IsChecked ? AccessibilityStates.Checked : AccessibilityStates.None, AccessibilityActions.Activate);
                case ToggleButton toggleButton:
                    return (AccessibilityRole.ToggleButton, Prefer(toggleButton.Text, CaptionOf(toggleButton)), null, toggleButton.IsChecked ? AccessibilityStates.Checked : AccessibilityStates.None, AccessibilityActions.Activate);
                case ComboBox combo:
                    return (AccessibilityRole.ComboBox, combo.ToolTip ?? "", combo.SelectedItem?.ToString(), AccessibilityStates.None, AccessibilityActions.Activate);
                case Button button:
                    return DeriveButton(button);
                case Slider slider:
                    return (AccessibilityRole.Slider, slider.ToolTip ?? "", FormatNumber(slider.Value), AccessibilityStates.None,
                        AccessibilityActions.Increment | AccessibilityActions.Decrement);
                case ProgressBar progress:
                    var range = progress.Maximum - progress.Minimum;
                    var percent = range > 0 ? (progress.Value - progress.Minimum) / range * 100 : 0;
                    return (AccessibilityRole.ProgressBar, progress.ToolTip ?? "", FormatNumber(MathF.Round(percent)) + " %", AccessibilityStates.None, AccessibilityActions.None);
                case ListBox list:
                    return (AccessibilityRole.List, list.ToolTip ?? "", null, AccessibilityStates.None, AccessibilityActions.None);
                case TabControl tabs:
                    return (AccessibilityRole.TabList, tabs.ToolTip ?? "", null, AccessibilityStates.None, AccessibilityActions.None);
                case Image image:
                    return (AccessibilityRole.Image, image.ToolTip, null, AccessibilityStates.None, AccessibilityActions.None);
                default:
                    return (AccessibilityRole.None, control.ToolTip, null, AccessibilityStates.None, AccessibilityActions.None);
            }
        }

        private static (AccessibilityRole, string?, string?, AccessibilityStates, AccessibilityActions) DeriveButton(Button button)
        {
            var label = Prefer(button.Text, CaptionOf(button));
            if (string.IsNullOrWhiteSpace(label))
                label = button.ToolTip;

            // Item buttons of a ListBox / tab headers of a TabControl.
            if (FindAncestor<ListBox>(button) is { } list && button.Tag is int index)
                return (AccessibilityRole.ListItem, label, null, list.IsSelected(index) ? AccessibilityStates.Selected : AccessibilityStates.None, AccessibilityActions.Activate);
            if (FindAncestor<TabControl>(button) is { } tabs && button.Tag is int tab)
                return (AccessibilityRole.Tab, label, null, tabs.SelectedIndex == tab ? AccessibilityStates.Selected : AccessibilityStates.None, AccessibilityActions.Activate);

            return (AccessibilityRole.Button, label, null, AccessibilityStates.None, AccessibilityActions.Activate);
        }

        private static T? FindAncestor<T>(Control control) where T : Control
        {
            var current = control.Parent;
            for (var depth = 0; depth < 6 && current != null; depth++)
            {
                if (current is T match)
                    return match;
                current = current.Parent;
            }
            return null;
        }

        private static string Prefer(string? first, string second) => string.IsNullOrWhiteSpace(first) ? second : first!;

        /// <summary>Text of all TextBlocks inside a control (the caption of templated content).</summary>
        private static string CaptionOf(Control control)
        {
            var builder = new StringBuilder();
            AppendCaption(control, builder, isRoot: true);
            return builder.ToString().Trim();
        }

        private static void AppendCaption(Control control, StringBuilder builder, bool isRoot)
        {
            if (control.IsGone || !control.IsVisible || control.AccessibilityIfCreated?.IsHidden == true)
                return;
            if (!isRoot && !string.IsNullOrEmpty(control.AccessibilityIfCreated?.Label))
            {
                builder.Append(control.AccessibilityIfCreated!.Label).Append(' ');
                return;
            }
            if (!isRoot && control is TextBlock textBlock && control is not TextBox)
            {
                if (!string.IsNullOrWhiteSpace(textBlock.Text))
                    builder.Append(textBlock.Text).Append(' ');
                return;
            }
            var count = control.VisualChildCount;
            for (var i = 0; i < count; i++)
                AppendCaption(control.GetVisualChild(i), builder, isRoot: false);
        }

        private static string FormatNumber(float value) => value.ToString("0.##", CultureInfo.CurrentCulture);
    }
}
