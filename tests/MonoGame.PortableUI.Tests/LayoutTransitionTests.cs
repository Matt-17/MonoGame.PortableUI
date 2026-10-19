using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>#159: layout transitions (FLIP) - a control moved by layout glides from where it was drawn.</summary>
    [TestClass]
    [DoNotParallelize] // drives the global ScreenSystem.TotalTime clock
    public class LayoutTransitionTests
    {
        private static readonly Rect Area = new Rect(0, 0, 300, 400);
        private static readonly Transition Linear100 = new Transition(TimeSpan.FromMilliseconds(100), Easings.Linear);

        private static Border Card(float height = 40) => new Border { Height = height, Width = 100, HorizontalAlignment = HorizontalAlignment.Left };

        private static void At(double seconds) => ScreenSystem.TotalTime = TimeSpan.FromSeconds(seconds);

        private static void Tick(Control control, double seconds)
        {
            At(seconds);
            foreach (var c in Walk(control))
                c.UpdateTimers();
        }

        private static IEnumerable<Control> Walk(Control control)
        {
            yield return control;
            for (var i = 0; i < control.VisualChildCount; i++)
            {
                foreach (var c in Walk(control.GetVisualChild(i)))
                    yield return c;
            }
        }

        private static float DrawnTop(Control control)
            => Vector2.Transform(new Vector2(control.ClippingRect.Left, control.ClippingRect.Top), control.GetRenderTransform()).Y;

        private static StackPanel Stack(int count, out Border[] cards)
        {
            var stack = new StackPanel { LayoutTransition = Linear100 };
            cards = new Border[count];
            for (var i = 0; i < count; i++)
            {
                cards[i] = Card();
                stack.AddChild(cards[i]);
            }
            stack.UpdateLayout(Area);
            return stack;
        }

        [TestMethod]
        public void Removing_a_child_lets_the_siblings_glide_up()
        {
            At(1);
            var stack = Stack(3, out var cards);
            Assert.AreEqual(80, cards[2].ClippingRect.Top);

            stack.Children.Remove(cards[0]);
            stack.UpdateLayout(Area);

            Assert.AreEqual(40, cards[2].ClippingRect.Top, "layout (and hit testing) take the new place at once");
            Assert.AreEqual(80, DrawnTop(cards[2]), 0.001f, "drawn where it was");
            Assert.IsTrue(cards[2].IsLayoutTransitioning);

            Tick(stack, 1.05);
            Assert.AreEqual(60, DrawnTop(cards[2]), 0.5f, "half way");
            Tick(stack, 1.2);
            Assert.AreEqual(40, DrawnTop(cards[2]), 0.001f);
            Assert.IsFalse(cards[2].IsLayoutTransitioning);
        }

        [TestMethod]
        public void Inserting_a_child_pushes_the_siblings_down_smoothly_and_the_new_one_appears_in_place()
        {
            At(2);
            var stack = Stack(2, out var cards);
            var inserted = Card();
            stack.Children.Insert(0, inserted);
            stack.UpdateLayout(Area);

            Assert.AreEqual(0, DrawnTop(cards[0]), 0.001f, "first card starts where it was");
            Assert.AreEqual(40, cards[0].ClippingRect.Top);
            Assert.IsFalse(inserted.IsLayoutTransitioning, "a new child has no previous place");
        }

        [TestMethod]
        public void A_new_move_mid_flight_starts_from_where_it_is_drawn()
        {
            At(3);
            var stack = Stack(3, out var cards);
            stack.Children.Remove(cards[0]);
            stack.UpdateLayout(Area);
            Tick(stack, 3.05); // card 2 drawn at 60, arranged at 40

            stack.Children.Insert(0, Card(100));
            stack.UpdateLayout(Area);
            Assert.AreEqual(140, cards[2].ClippingRect.Top);
            Assert.AreEqual(60, DrawnTop(cards[2]), 0.5f, "no jump");
        }

        [TestMethod]
        public void A_child_moved_with_its_gliding_parent_does_not_glide_twice()
        {
            At(4);
            var outer = new StackPanel { LayoutTransition = Linear100 };
            var first = Card();
            var inner = new StackPanel { LayoutTransition = Linear100 };
            var nested = Card();
            inner.AddChild(nested);
            outer.AddChild(first);
            outer.AddChild(inner);
            outer.UpdateLayout(Area);
            Assert.AreEqual(40, nested.ClippingRect.Top);

            outer.Children.Remove(first);
            outer.UpdateLayout(Area);

            Assert.IsTrue(inner.IsLayoutTransitioning);
            Assert.IsFalse(nested.IsLayoutTransitioning, "the parent's glide carries it");
            var drawn = Vector2.Transform(new Vector2(0, nested.ClippingRect.Top), nested.GetRenderTransform() * inner.GetRenderTransform());
            Assert.AreEqual(40, drawn.Y, 0.001f);
        }

        [TestMethod]
        public void A_moving_parent_without_a_transition_still_lets_the_children_glide()
        {
            At(5);
            // A centred row: adding a card re-centres the panel, the cards in it glide to their new places.
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, LayoutTransition = Linear100 };
            var a = Card();
            row.AddChild(a);
            var host = new Grid();
            host.AddChild(row);
            host.UpdateLayout(Area);
            Assert.AreEqual(100, a.ClippingRect.Left);

            row.AddChild(Card());
            host.UpdateLayout(Area);
            Assert.AreEqual(50, a.ClippingRect.Left);
            var drawn = Vector2.Transform(new Vector2(a.ClippingRect.Left, 0), a.GetRenderTransform()).X;
            Assert.AreEqual(100, drawn, 0.001f);
        }

        [TestMethod]
        public void Without_a_transition_nothing_glides()
        {
            At(6);
            var stack = Stack(2, out var cards);
            stack.LayoutTransition = null;
            stack.Children.Remove(cards[0]);
            stack.UpdateLayout(Area);
            Assert.IsFalse(cards[1].IsLayoutTransitioning);
            Assert.AreEqual(Matrix.Identity, cards[1].GetRenderTransform());
        }

        [TestMethod]
        public void A_child_entry_overrides_the_panel_and_complete_snaps_it()
        {
            At(7);
            var stack = new StackPanel();
            var first = Card();
            var second = Card();
            second.Transitions.Add(TransitionProperty.Layout, Linear100);
            stack.AddChild(first);
            stack.AddChild(second);
            stack.UpdateLayout(Area);

            first.Height = 10;
            stack.UpdateLayout(Area);
            Assert.IsTrue(second.IsLayoutTransitioning);
            Assert.IsTrue(second.IsTransitioning);

            second.CompleteTransitions();
            Assert.IsFalse(second.IsLayoutTransitioning);
            Assert.AreEqual(10, DrawnTop(second), 0.001f);
        }

        [TestMethod]
        public void Scrolling_does_not_glide()
        {
            At(8);
            var content = new StackPanel { LayoutTransition = Linear100 };
            var cards = new Border[20];
            for (var i = 0; i < cards.Length; i++)
            {
                cards[i] = Card();
                content.AddChild(cards[i]);
            }
            var viewer = new ScrollViewer { Content = content, Height = 200, VerticalAlignment = VerticalAlignment.Top };
            viewer.UpdateLayout(Area);

            viewer.ScrollTo(new PointF(0, 120));
            viewer.UpdateLayout(Area);
            Assert.AreEqual(-120, cards[0].ClippingRect.Top, 0.001f, "scrolled");
            foreach (var card in cards)
                Assert.IsFalse(card.IsLayoutTransitioning, "a scroll is no layout change");
        }

        [TestMethod]
        public void Canvas_position_changes_glide()
        {
            At(9);
            var canvas = new Canvas { LayoutTransition = Linear100 };
            var piece = Card();
            canvas.AddChild(piece, 10, 10);
            canvas.UpdateLayout(Area);

            Canvas.SetLeft(piece, 110);
            Assert.AreEqual(110, piece.ClippingRect.Left, "moved without a layout pass");
            var drawn = Vector2.Transform(new Vector2(piece.ClippingRect.Left, 0), piece.GetRenderTransform()).X;
            Assert.AreEqual(10, drawn, 0.001f);
            Tick(canvas, 9.2);
            Assert.IsFalse(piece.IsLayoutTransitioning);
        }

        [TestMethod]
        public void ListBox_rows_follow_their_items_on_insert()
        {
            At(10);
            var list = new ListBox { LayoutTransition = Linear100, ItemHeight = 30, Height = 300, VerticalAlignment = VerticalAlignment.Top };
            var items = new object[] { "a", "b", "c" };
            list.Items.AddRange(items);
            list.UpdateLayout(Area);
            list.UpdateLayout(Area);
            var rowOfC = RowFor(list, items[2]);
            var shownBefore = DrawnTop(rowOfC);

            list.Items.Insert(0, "new");
            list.UpdateLayout(Area);

            var row = RowFor(list, items[2]);
            Assert.AreEqual(shownBefore + 30, row.ClippingRect.Top, 0.5f, "c moved down one row");
            Assert.AreEqual(shownBefore, DrawnTop(row), 0.5f, "drawn where c was, whichever row control shows it now");
            Assert.IsTrue(row.IsLayoutTransitioning);
            Assert.IsFalse(RowFor(list, "new").IsLayoutTransitioning, "the new item appears in place");
        }

        private static Control RowFor(ListBox list, object item)
        {
            foreach (var button in list.ItemButtons)
            {
                if (Equals(button.Text, item))
                    return button;
            }
            throw new AssertFailedException($"no row shows {item}");
        }
    }
}
