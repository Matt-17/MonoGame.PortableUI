using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class DragDropTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private static (TestScreen Screen, Grid Root, Button Source, Border Target) CreateScene()
        {
            var screen = new TestScreen();
            var root = new Grid();
            var source = new Button { Width = 100, Height = 40 };
            var target = new Border { AllowDrop = true };
            root.AddChild(source);
            root.AddChild(target);
            screen.Content = root;
            root.UpdateLayout(new Rect(0, 0, 800, 600));
            source.UpdateLayout(new Rect(500, 500, 100, 40));
            target.UpdateLayout(new Rect(0, 0, 200, 200));
            return (screen, root, source, target);
        }

        [TestMethod]
        public void Drag_lifecycle_enter_over_drop_completes_with_effect()
        {
            var (screen, root, source, target) = CreateScene();
            var log = new System.Collections.Generic.List<string>();
            target.DragEnter += (s, a) => { a.Effect = DragDropEffects.Move; log.Add("enter"); };
            target.DragOver += (s, a) => { a.Effect = DragDropEffects.Move; log.Add("over"); };
            target.Drop += (s, a) => log.Add($"drop:{a.Payload}");

            var operation = screen.BeginDrag(source, "card-1", DragDropEffects.Move, null);
            Assert.IsNotNull(operation);
            DragCompletedEventArgs? completed = null;
            operation!.Completed += (s, a) => completed = a;

            screen.UpdateDrag(operation, new PointF(100, 100), root);
            Assert.AreSame(target, operation.CurrentTarget);
            screen.UpdateDrag(operation, new PointF(110, 100), root);
            screen.CompleteDrag(operation, new PointF(110, 100));

            CollectionAssert.Contains(log, "enter");
            CollectionAssert.Contains(log, "drop:card-1");
            Assert.IsNotNull(completed);
            Assert.AreSame(target, completed!.Target);
            Assert.AreEqual(DragDropEffects.Move, completed.Effect);
            Assert.IsNull(screen.ActiveDrag);
            Assert.IsFalse(operation.IsActive);
        }

        [TestMethod]
        public void Leaving_the_target_raises_drag_leave_and_drop_without_target_cancels()
        {
            var (screen, root, source, target) = CreateScene();
            var leaves = 0;
            var drops = 0;
            target.DragEnter += (s, a) => a.Effect = DragDropEffects.Copy;
            target.DragOver += (s, a) => a.Effect = DragDropEffects.Copy;
            target.DragLeave += (s, a) => leaves++;
            target.Drop += (s, a) => drops++;

            var operation = screen.BeginDrag(source, "card-2", DragDropEffects.Copy, null)!;
            var canceled = false;
            operation.Canceled += (s, a) => canceled = true;

            screen.UpdateDrag(operation, new PointF(50, 50), root);
            Assert.AreSame(target, operation.CurrentTarget);

            // Move off the target (still inside the root).
            screen.UpdateDrag(operation, new PointF(400, 400), root);
            Assert.AreEqual(1, leaves);
            Assert.IsNull(operation.CurrentTarget);

            screen.CompleteDrag(operation, new PointF(400, 400));
            Assert.AreEqual(0, drops);
            Assert.IsTrue(canceled);
            Assert.IsNull(screen.ActiveDrag);
        }

        [TestMethod]
        public void Target_rejecting_with_effect_none_prevents_drop()
        {
            var (screen, root, source, target) = CreateScene();
            var drops = 0;
            target.DragEnter += (s, a) => a.Effect = DragDropEffects.None;
            target.DragOver += (s, a) => a.Effect = DragDropEffects.None;
            target.Drop += (s, a) => drops++;

            var operation = screen.BeginDrag(source, "card-3", DragDropEffects.All, null)!;
            var canceled = false;
            operation.Canceled += (s, a) => canceled = true;

            screen.UpdateDrag(operation, new PointF(50, 50), root);
            screen.CompleteDrag(operation, new PointF(50, 50));

            Assert.AreEqual(0, drops);
            Assert.IsTrue(canceled);
        }

        [TestMethod]
        public void Cancel_drag_raises_canceled_and_clears_state()
        {
            var (screen, root, source, target) = CreateScene();
            target.DragEnter += (s, a) => a.Effect = DragDropEffects.Move;
            var operation = screen.BeginDrag(source, "card-4", DragDropEffects.Move, null)!;
            var canceled = false;
            operation.Canceled += (s, a) => canceled = true;

            screen.UpdateDrag(operation, new PointF(50, 50), root);
            screen.CancelDrag();

            Assert.IsTrue(canceled);
            Assert.IsNull(screen.ActiveDrag);
            Assert.IsFalse(operation.IsActive);
        }

        [TestMethod]
        public void Deepest_allow_drop_control_wins()
        {
            var (screen, root, _, target) = CreateScene();
            var inner = new Border { AllowDrop = true };
            target.Content = inner;
            // Setting Content invalidates layout; without a ScreenEngine that zeroes the
            // manually laid-out rects, so re-establish them afterwards.
            root.UpdateLayout(new Rect(0, 0, 800, 600));
            target.UpdateLayout(new Rect(0, 0, 200, 200));
            inner.UpdateLayout(new Rect(20, 20, 100, 100));

            Control? entered = null;
            inner.DragEnter += (s, a) => { a.Effect = DragDropEffects.Move; entered = inner; };
            target.DragEnter += (s, a) => { a.Effect = DragDropEffects.Move; entered ??= target; };

            var sourceButton = new Button();
            var operation = screen.BeginDrag(sourceButton, "card-5", DragDropEffects.Move, null)!;
            screen.UpdateDrag(operation, new PointF(50, 50), root);

            Assert.AreSame(inner, entered);
            Assert.AreSame(inner, operation.CurrentTarget);
            screen.CancelDrag();
        }
    }
}
