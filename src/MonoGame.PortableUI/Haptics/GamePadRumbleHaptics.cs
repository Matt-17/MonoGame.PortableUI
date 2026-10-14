using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Haptics
{
    /// <summary>
    ///     UI haptics as gamepad rumble: short pulses on the light (right) motor, the heavy one for a thump
    ///     and the error buzz. Answers gamepad input only. The pulse ends on its own in
    ///     <see cref="Update"/>; the engine keeps updating while it runs, also in OnDemand mode.
    /// </summary>
    public sealed class GamePadRumbleHaptics : IUiHapticPlayer
    {
        // Per pulse: (start ms, length ms, left motor, right motor) segments.
        private static readonly (int Start, int Length, float Left, float Right)[][] Patterns =
        {
            Array.Empty<(int, int, float, float)>(),
            new[] { (0, 18, 0f, 0.18f) },
            new[] { (0, 26, 0f, 0.32f) },
            new[] { (0, 34, 0.1f, 0.5f) },
            new[] { (0, 60, 0.8f, 0.6f) },
            new[] { (0, 45, 0.5f, 0.7f), (95, 45, 0.5f, 0.7f) }
        };

        private (int Start, int Length, float Left, float Right)[] _pattern = Patterns[0];
        private TimeSpan _startedAt;
        private float _intensity;
        private float _length = 1;
        private Vector2 _applied;

        public GamePadRumbleHaptics(PlayerIndex playerIndex = PlayerIndex.One)
        {
            PlayerIndex = playerIndex;
        }

        public PlayerIndex PlayerIndex { get; set; }

        /// <summary>Sets the motors (tests and other backends replace the MonoGame call).</summary>
        internal Action<PlayerIndex, float, float> SetVibration = static (index, left, right) => GamePad.SetVibration(index, left, right);

        public bool Supports(UiInputDevice device) => device == UiInputDevice.GamePad;

        public void Play(UiHaptic haptic, float intensity, float length)
        {
            _pattern = Patterns[(int)haptic];
            _startedAt = ScreenSystem.TotalTime;
            _intensity = intensity;
            _length = length > 0 ? length : 1;
            Update(_startedAt);
        }

        /// <summary>True until the pulse has ended (the engine keeps updating meanwhile).</summary>
        public bool IsActive => _pattern.Length > 0 || _applied != Vector2.Zero;

        public void Update(TimeSpan now)
        {
            // A theme's length factor stretches the whole pattern (pauses included).
            var elapsed = (now - _startedAt).TotalMilliseconds / _length;
            var motors = Vector2.Zero;
            foreach (var segment in _pattern)
            {
                if (elapsed >= segment.Start && elapsed < segment.Start + segment.Length)
                    motors = new Vector2(segment.Left, segment.Right) * _intensity;
            }

            var last = _pattern.Length > 0 ? _pattern[^1] : default;
            if (_pattern.Length > 0 && elapsed >= last.Start + last.Length)
                _pattern = Patterns[0];
            if (motors == _applied)
                return;
            _applied = motors;
            SetVibration(PlayerIndex, motors.X, motors.Y);
        }
    }
}
