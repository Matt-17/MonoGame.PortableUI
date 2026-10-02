using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Accessibility;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class AccessibilityTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private sealed class FakeBridge : IAccessibilityBridge
        {
            public bool IsScreenReaderActive { get; set; } = true;
            public int TreeChanges { get; private set; }
            public List<AccessibilityNode?> Focus { get; } = new List<AccessibilityNode?>();
            public List<string> Live { get; } = new List<string>();

            public void OnTreeChanged(IReadOnlyList<AccessibilityNode> nodes) => TreeChanges++;
            public void OnFocusChanged(AccessibilityNode? node) => Focus.Add(node);
            public void OnLiveRegionChanged(AccessibilityNode node) => Live.Add(node.Label);
        }

        [TestInitialize]
        public void Reset() { if (ScreenEngine.Instance != null) ScreenEngine.Instance.FocusedControl = null; }

        private static (ScreenEngine Engine, TestScreen Screen) Show(Game game, Control content)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 600);
            var screen = new TestScreen { Content = content };
            engine.NavigateToScreen(screen);
            engine.Update(new GameTime());
            return (engine, screen);
        }

        [TestMethod]
        public void Controls_map_to_roles_labels_values_and_states()
        {
            using var game = new Game();
            var checkBox = new CheckBox { Text = "Sound", IsChecked = true };
            var disabled = new Button { Text = "Locked", IsEnabled = false };
            var content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Settings", IsHeading = true },
                    new Button { Text = "Play" },
                    checkBox,
                    new Slider { Minimum = 0, Maximum = 10, Value = 7, ToolTip = "Volume" },
                    new TextBox { HintText = "Name", Text = "Ada" },
                    new TextBox { HintText = "Password", PasswordChar = '*', Text = "abc" },
                    new ProgressBar { Minimum = 0, Maximum = 200, Value = 50, ToolTip = "Download" },
                    disabled,
                    new Border { Height = 10 },
                    new TextBlock { Text = "" }
                }
            };
            var (_, screen) = Show(game, content);

            var nodes = AccessibilityTree.Build(screen);
            CollectionAssert.AreEqual(
                new[] { "Heading \"Settings\"", "Button \"Play\"", "CheckBox \"Sound\" [Checked]", "Slider \"Volume\" = 7",
                        "TextField \"Name\" = Ada", "TextField \"Password\" = ••• [Password]", "ProgressBar \"Download\" = 25 %",
                        "Button \"Locked\" [Disabled]" },
                nodes.Select(n => n.ToString()).ToArray());
            Assert.AreEqual(AccessibilityActions.None, nodes.Last().Actions, "disabled controls offer no actions");
        }

        [TestMethod]
        public void Buttons_read_their_templated_caption_and_overrides_win()
        {
            using var game = new Game();
            var icon = new Button { Content = new Image(), Width = 40, Height = 40 };
            icon.Accessibility.Label = "Delete";
            var templated = new Button { Content = new StackPanel { Children = { new TextBlock { Text = "New" }, new TextBlock { Text = "game" } } } };
            var hidden = new TextBlock { Text = "decor" };
            hidden.Accessibility.IsHidden = true;
            var (_, screen) = Show(game, new StackPanel { Children = { icon, templated, hidden } });

            var nodes = AccessibilityTree.Build(screen);
            Assert.AreEqual(2, nodes.Count);
            Assert.AreEqual("Delete", nodes[0].Label);
            Assert.AreEqual("New game", nodes[1].Label, "children of a button are read as its caption, not separately");
        }

        [TestMethod]
        public void Reading_order_follows_the_focus_order()
        {
            using var game = new Game();
            var a = new Button { Text = "A" };
            var b = new Button { Text = "B", TabIndex = 1 };
            var c = new Button { Text = "C", TabIndex = 0 };
            var (_, screen) = Show(game, new StackPanel { Children = { a, b, c } });

            CollectionAssert.AreEqual(new[] { "C", "B", "A" }, AccessibilityTree.Build(screen).Select(n => n.Label).ToArray());
        }

        [TestMethod]
        public void List_items_and_tabs_expose_selection()
        {
            using var game = new Game();
            var list = new ListBox { Height = 200, ToolTip = "Saves" };
            list.Items.Add("Slot 1");
            list.Items.Add("Slot 2");
            list.SelectedIndex = 1;
            var (_, screen) = Show(game, list);

            var nodes = AccessibilityTree.Build(screen);
            Assert.AreEqual(AccessibilityRole.List, nodes[0].Role);
            Assert.AreEqual("ListItem \"Slot 2\" [Selected]", nodes[2].ToString());
            Assert.IsFalse(nodes[1].Has(AccessibilityStates.Selected));
        }

        [TestMethod]
        public void Actions_activate_buttons_and_step_sliders()
        {
            using var game = new Game();
            var clicked = 0;
            var button = new Button { Text = "Go" };
            button.Click += (s, e) => clicked++;
            var slider = new Slider { Minimum = 0, Maximum = 10, Value = 5, SmallChange = 2 };
            Show(game, new StackPanel { Children = { button, slider } });

            Assert.IsTrue(AccessibilityTree.PerformAction(button, AccessibilityActions.Activate));
            Assert.IsTrue(AccessibilityTree.PerformAction(slider, AccessibilityActions.Increment));
            Assert.AreEqual(1, clicked);
            Assert.AreEqual(7, slider.Value);
        }

        [TestMethod]
        public void Bridge_gets_tree_focus_and_live_region_notifications_only_while_active()
        {
            using var game = new Game();
            var score = new TextBlock { Text = "Score 0" };
            score.Accessibility.IsLiveRegion = true;
            var button = new Button { Text = "Next" };
            var (engine, _) = Show(game, new StackPanel { Children = { score, button } });
            var bridge = new FakeBridge { IsScreenReaderActive = false };
            engine.AccessibilityBridge = bridge;

            engine.Update(new GameTime());
            Assert.AreEqual(0, bridge.TreeChanges, "no tree while no reader runs");
            Assert.AreEqual(0, engine.AccessibilityNodes.Count);

            bridge.IsScreenReaderActive = true;
            engine.Update(new GameTime());
            Assert.AreEqual(1, bridge.TreeChanges);
            engine.Update(new GameTime());
            Assert.AreEqual(1, bridge.TreeChanges, "unchanged tree is not re-sent");

            button.Focus();
            engine.Update(new GameTime());
            Assert.AreEqual("Next", bridge.Focus.Last()?.Label);

            score.Text = "Score 10";
            engine.Update(new GameTime());
            CollectionAssert.AreEqual(new[] { "Score 10" }, bridge.Live);
        }
    }
}
