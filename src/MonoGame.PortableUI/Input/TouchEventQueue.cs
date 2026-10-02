using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;

namespace MonoGame.PortableUI.Input
{
    /// <summary>
    ///     Touch events from a platform thread turned into per-frame touch states with
    ///     <c>TouchPanel.GetState()</c> semantics: Pressed once, then Moved while held, Released once; a
    ///     tap that starts and ends between two frames is Pressed, then Released the next frame. Adding is
    ///     thread-safe; <see cref="Read"/> belongs to the game thread.
    /// </summary>
    internal sealed class TouchEventQueue
    {
        internal enum Kind : byte { Down, Move, Up }

        private readonly struct RawTouch
        {
            public RawTouch(Kind kind, int id, Vector2 position)
            {
                Kind = kind;
                Id = id;
                Position = position;
            }

            public Kind Kind { get; }
            public int Id { get; }
            public Vector2 Position { get; }
        }

        private sealed class Slot
        {
            public TouchLocationState State;
            public Vector2 Position;
            public bool ReleasePending;
        }

        private readonly object _gate = new object();
        private List<RawTouch> _pending = new List<RawTouch>();
        private List<RawTouch> _draining = new List<RawTouch>();

        // Game thread only. Insertion order: the first finger down stays touches[0], the primary touch.
        private readonly Dictionary<int, Slot> _slots = new Dictionary<int, Slot>();
        private readonly List<int> _order = new List<int>();
        private TouchLocation[][] _arrays = new TouchLocation[4][];

        /// <summary>Records one pointer event. Thread-safe.</summary>
        public void Add(Kind kind, int id, Vector2 position)
        {
            lock (_gate)
                _pending.Add(new RawTouch(kind, id, position));
        }

        /// <summary>This frame's touches. Call once per frame; the result reuses its array.</summary>
        public TouchCollection Read()
        {
            lock (_gate)
                (_pending, _draining) = (_draining, _pending);

            // Advance last frame's states.
            for (var i = _order.Count - 1; i >= 0; i--)
            {
                var id = _order[i];
                var slot = _slots[id];
                if (slot.State == TouchLocationState.Released)
                {
                    _slots.Remove(id);
                    _order.RemoveAt(i);
                }
                else if (slot.ReleasePending)
                {
                    slot.State = TouchLocationState.Released;
                    slot.ReleasePending = false;
                }
                else
                    slot.State = TouchLocationState.Moved;
            }

            foreach (var raw in _draining)
            {
                _slots.TryGetValue(raw.Id, out var slot);
                switch (raw.Kind)
                {
                    case Kind.Down:
                        if (slot == null)
                            _order.Add(raw.Id);
                        _slots[raw.Id] = new Slot { State = TouchLocationState.Pressed, Position = raw.Position };
                        break;
                    case Kind.Move when slot != null && !slot.ReleasePending && slot.State != TouchLocationState.Released:
                        slot.Position = raw.Position;
                        break;
                    case Kind.Up when slot != null && slot.State != TouchLocationState.Released && !slot.ReleasePending:
                        slot.Position = raw.Position;
                        if (slot.State == TouchLocationState.Pressed)
                            slot.ReleasePending = true; // report the press first
                        else
                            slot.State = TouchLocationState.Released;
                        break;
                }
            }
            _draining.Clear();

            if (_order.Count == 0)
                return new TouchCollection(Array.Empty<TouchLocation>());
            if (_order.Count >= _arrays.Length)
                Array.Resize(ref _arrays, _order.Count + 1);
            var touches = _arrays[_order.Count] ??= new TouchLocation[_order.Count];
            for (var i = 0; i < _order.Count; i++)
            {
                var slot = _slots[_order[i]];
                touches[i] = new TouchLocation(_order[i], slot.State, slot.Position);
            }
            return new TouchCollection(touches);
        }
    }
}
