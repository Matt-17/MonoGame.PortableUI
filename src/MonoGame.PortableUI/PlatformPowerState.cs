using System.Diagnostics;

namespace MonoGame.PortableUI
{
    /// <summary>Whether the device asks apps to save power (Android battery saver); false elsewhere.</summary>
    internal static class PlatformPowerState
    {
#if ANDROID
        private static readonly long PollInterval = Stopwatch.Frequency * 2;
        private static long _lastPoll = long.MinValue;
        private static bool _powerSaveMode;
#endif

        /// <summary>Polled at most every two seconds: it is read every update.</summary>
        public static bool IsPowerSaveMode
        {
            get
            {
#if ANDROID
                var now = Stopwatch.GetTimestamp();
                if (_lastPoll != long.MinValue && now - _lastPoll < PollInterval)
                    return _powerSaveMode;
                _lastPoll = now;
                try
                {
                    var context = global::Android.App.Application.Context;
                    _powerSaveMode = context.GetSystemService(global::Android.Content.Context.PowerService)
                        is global::Android.OS.PowerManager { IsPowerSaveMode: true };
                }
                catch (System.Exception)
                {
                    _powerSaveMode = false;
                }
                return _powerSaveMode;
#else
                return false;
#endif
            }
        }
    }
}
