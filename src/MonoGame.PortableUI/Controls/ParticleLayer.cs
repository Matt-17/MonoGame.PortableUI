using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>How one particle of a <see cref="ParticleBurst"/> looks.</summary>
    public enum ParticleShape
    {
        /// <summary>A paper strip that tumbles (its width swings with the spin): confetti.</summary>
        Paper,

        /// <summary>A round dot.</summary>
        Dot,

        /// <summary>A line along the direction of flight that gets longer the faster it moves: sparks.</summary>
        Streak
    }

    /// <summary>A burst of particles (<see cref="ParticleLayer.Burst"/>); immutable presets can be shared.</summary>
    public sealed class ParticleBurst
    {
        public int Count { get; init; } = 80;

        /// <summary>Picked in turn for the particles.</summary>
        public IReadOnlyList<Color> Colors { get; init; } = new[] { Color.White };

        public ParticleShape Shape { get; init; } = ParticleShape.Paper;

        /// <summary>Particle size in layout units (a paper strip is X wide and Y tall; a dot uses X).</summary>
        public Vector2 Size { get; init; } = new Vector2(10, 6);

        /// <summary>Launch speed range, layout units per second.</summary>
        public float MinSpeed { get; init; } = 250;

        public float MaxSpeed { get; init; } = 750;

        /// <summary>Launch direction in degrees (0 = right, -90 = up) and the cone around it (360 = all around).</summary>
        public float Direction { get; init; } = -90;

        public float Spread { get; init; } = 360;

        /// <summary>Downward acceleration, layout units per second squared.</summary>
        public float Gravity { get; init; } = 900;

        /// <summary>Air resistance: the share of the speed lost per second.</summary>
        public float Drag { get; init; } = 1.5f;

        /// <summary>Most spin, radians per second (each particle gets a random share, either way).</summary>
        public float Spin { get; init; } = 10;

        /// <summary>How long a particle lives; it fades out over the last third.</summary>
        public TimeSpan Lifetime { get; init; } = TimeSpan.FromSeconds(2.5);

        /// <summary>Colourful paper strips flying up and out, then fluttering down.</summary>
        public static ParticleBurst Confetti { get; } = new ParticleBurst
        {
            Count = 90,
            Colors = new[] { new Color(14, 140, 123), new Color(94, 158, 26), new Color(255, 193, 7), new Color(233, 30, 99), new Color(33, 150, 243) }
        };

        /// <summary>Short bright streaks in all directions, gone in under a second.</summary>
        public static ParticleBurst Sparks { get; } = new ParticleBurst
        {
            Count = 40,
            Shape = ParticleShape.Streak,
            Colors = new[] { new Color(255, 235, 150), new Color(255, 193, 7), Color.White },
            Size = new Vector2(2.5f, 2.5f),
            MinSpeed = 300,
            MaxSpeed = 900,
            Gravity = 300,
            Drag = 3,
            Lifetime = TimeSpan.FromSeconds(0.8)
        };

        /// <summary>Soft dots rising like bubbles.</summary>
        public static ParticleBurst Bubbles { get; } = new ParticleBurst
        {
            Count = 30,
            Shape = ParticleShape.Dot,
            Colors = new[] { new Color(255, 255, 255, 200), new Color(178, 235, 242, 200) },
            Size = new Vector2(8, 8),
            MinSpeed = 40,
            MaxSpeed = 160,
            Spread = 120,
            Gravity = -120,
            Drag = 0.8f,
            Lifetime = TimeSpan.FromSeconds(1.6)
        };
    }

    /// <summary>
    ///     Particle bursts above a screen (confetti, sparks, bubbles): an <see cref="Adorner"/> that never takes a
    ///     touch. <c>control.Burst()</c> finds or adds the screen's layer and bursts from the control's centre.
    /// </summary>
    public sealed class ParticleLayer : Adorner
    {
        private struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Angle;
            public float Spin;
            public float Age;
            public float Life;
            public float Drag;
            public float Gravity;
            public Vector2 Size;
            public Color Color;
            public ParticleShape Shape;
        }

        private readonly List<Particle> _particles = new List<Particle>();
        private readonly Random _random;
        private TimeSpan? _lastStep;

        public ParticleLayer() : this(new Random())
        {
        }

        internal ParticleLayer(Random random)
        {
            _random = random;
        }

        internal IEnumerable<Vector2> PositionsForTests() => _particles.ConvertAll(p => p.Position);

        /// <summary>Particles still in the air.</summary>
        public int ActiveParticles => _particles.Count;

        protected internal override bool HitTest(PointF point) => false;

        /// <summary>Bursts <paramref name="burst"/> (default <see cref="ParticleBurst.Confetti"/>) from <paramref name="origin"/> (layout units).</summary>
        public void Burst(PointF origin, ParticleBurst? burst = null)
        {
            burst ??= ParticleBurst.Confetti;
            var direction = MathHelper.ToRadians(burst.Direction);
            var spread = MathHelper.ToRadians(Math.Clamp(burst.Spread, 0, 360));
            for (var i = 0; i < burst.Count; i++)
            {
                var angle = direction + (Next() - 0.5f) * spread;
                var speed = burst.MinSpeed + Next() * Math.Max(0, burst.MaxSpeed - burst.MinSpeed);
                _particles.Add(new Particle
                {
                    Position = new Vector2(origin.X, origin.Y),
                    Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                    Angle = Next() * MathHelper.TwoPi,
                    Spin = (Next() * 2 - 1) * burst.Spin,
                    Life = (float)burst.Lifetime.TotalSeconds * (0.75f + Next() * 0.25f),
                    Drag = burst.Drag,
                    Gravity = burst.Gravity,
                    Size = burst.Size,
                    Color = burst.Colors.Count > 0 ? burst.Colors[i % burst.Colors.Count] : Color.White,
                    Shape = burst.Shape
                });
            }
            _lastStep = null;
            RequestRedraw();
        }

        private float Next() => (float)_random.NextDouble();

        /// <summary>Moves every particle on by <paramref name="seconds"/> and drops the ones that are gone.</summary>
        internal void Step(float seconds)
        {
            var bottom = ClippingRect.Height > 0 ? ClippingRect.Bottom + 40 : float.MaxValue;
            for (var i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                var before = p.Velocity;
                p.Velocity *= Math.Max(0, 1 - p.Drag * seconds);
                p.Velocity.Y += p.Gravity * seconds;
                // The average of both speeds: exact under constant gravity, whatever the frame rate.
                p.Position += (before + p.Velocity) * (seconds / 2);
                p.Angle += p.Spin * seconds;
                p.Age += seconds;
                if (p.Age >= p.Life || p.Position.Y > bottom)
                    _particles.RemoveAt(i);
                else
                    _particles[i] = p;
            }
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            if (_particles.Count == 0)
            {
                _lastStep = null;
                return;
            }
            var now = ScreenSystem.TotalTime;
            // Clamped: with on-demand rendering the previous frame may be seconds old.
            Step(_lastStep is { } last ? (float)Math.Min(0.05, (now - last).TotalSeconds) : 0);
            _lastStep = now;

            var layout = ClippingRect;
            var scale = layout.Width > 0 ? rect.Width / layout.Width : 1f;
            var pixel = Primitives.Pixel(spriteBatch);
            foreach (var p in _particles)
            {
                var fade = Math.Clamp((p.Life - p.Age) / (p.Life / 3), 0, 1);
                var color = Brush.ApplyOpacity(p.Color, RenderOpacity * fade);
                var at = new Vector2(rect.Left + (p.Position.X - layout.Left) * scale, rect.Top + (p.Position.Y - layout.Top) * scale);
                switch (p.Shape)
                {
                    case ParticleShape.Dot:
                        var d = p.Size.X * scale;
                        RoundedRectRenderer.DrawSolid(spriteBatch, new Rect(at.X - d / 2, at.Y - d / 2, d, d), new CornerRadius(d / 2), color);
                        break;
                    case ParticleShape.Streak:
                        var speed = p.Velocity.Length();
                        var length = Math.Max(p.Size.X, speed * 0.03f) * scale;
                        var heading = MathF.Atan2(p.Velocity.Y, p.Velocity.X);
                        spriteBatch.Draw(pixel, at, null, color, heading, new Vector2(1, 0.5f), new Vector2(length, p.Size.Y * scale), SpriteEffects.None, 0);
                        break;
                    default:
                        // A strip tumbling in 3D: its width swings with the spin.
                        var size = new Vector2(p.Size.X * MathF.Abs(MathF.Cos(p.Angle * 1.3f)) + 2, p.Size.Y) * scale;
                        spriteBatch.Draw(pixel, at, null, color, p.Angle, new Vector2(0.5f), size, SpriteEffects.None, 0);
                        break;
                }
            }
            ScreenEngine.RequestAnimationFrame();
        }
    }

    public static class ParticleExtensions
    {
        /// <summary>The screen's particle layer, added to its <see cref="Screen.Adorners"/> on first use.</summary>
        public static ParticleLayer Particles(this Screen screen)
        {
            foreach (var adorner in screen.Adorners)
            {
                if (adorner is ParticleLayer layer)
                    return layer;
            }
            var added = new ParticleLayer();
            screen.Adorners.Add(added);
            return added;
        }

        /// <summary>Bursts particles from the centre of <paramref name="control"/> (default confetti). Needs the control on a screen.</summary>
        public static void Burst(this Control control, ParticleBurst? burst = null)
        {
            if (control.Screen is not { } screen)
                return;
            var r = control.ClippingRect;
            screen.Particles().Burst(new PointF(r.Left + r.Width / 2, r.Top + r.Height / 2), burst);
        }
    }
}
