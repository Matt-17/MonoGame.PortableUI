#if ANDROID
using System;
using Android.OS;
using Android.Views;
using MonoGame.PortableUI.Haptics;

namespace MonoGame.PortableUI
{
    /// <summary>
    ///     UI haptics on Android through the view's haptic feedback (<c>View.PerformHapticFeedback</c>): the
    ///     system's own click, tick and reject patterns, tuned per device, no VIBRATE permission, and off when
    ///     the user turned touch feedback off. Answers touch input only.
    ///     <code>engine.Haptics.Players.Add(new AndroidHaptics(gameView));</code>
    /// </summary>
    public sealed class AndroidHaptics : IUiHapticPlayer
    {
        private readonly View _view;
        private readonly Feedback?[] _feedback = new Feedback?[(int)UiHaptic.Double + 1];

        public AndroidHaptics(View view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            var api30 = Build.VERSION.SdkInt >= BuildVersionCodes.R;
            _feedback[(int)UiHaptic.Tick] = new Feedback(view, FeedbackConstants.ClockTick);
            _feedback[(int)UiHaptic.Tap] = new Feedback(view, FeedbackConstants.KeyboardTap);
            _feedback[(int)UiHaptic.Click] = new Feedback(view, FeedbackConstants.VirtualKey);
            _feedback[(int)UiHaptic.Heavy] = new Feedback(view, FeedbackConstants.LongPress);
            _feedback[(int)UiHaptic.Double] = new Feedback(view, api30 ? FeedbackConstants.Reject : FeedbackConstants.LongPress);
        }

        public bool Supports(UiInputDevice device) => device == UiInputDevice.Touch;

        public bool IsActive => false;

        public void Play(UiHaptic haptic, float intensity)
        {
            if (intensity <= 0 || _feedback[(int)haptic] is not { } feedback)
                return;
            // The game loop may run off the UI thread; the pre-made runnables avoid a delegate per pulse.
            if (Looper.MyLooper() == Looper.MainLooper)
                feedback.Run();
            else
                _view.Post(feedback);
        }

        public void Update(TimeSpan now)
        {
        }

        private sealed class Feedback : Java.Lang.Object, Java.Lang.IRunnable
        {
            private readonly View _view;
            private readonly FeedbackConstants _constant;

            public Feedback(View view, FeedbackConstants constant)
            {
                _view = view;
                _constant = constant;
            }

            public void Run() => _view.PerformHapticFeedback(_constant);
        }
    }
}
#endif
