using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>One option of a <see cref="SegmentedControl"/>: its label and an optional muted note beside it.</summary>
    public sealed class Segment
    {
        public Segment(string text, string? note = null)
        {
            Text = text;
            Note = note;
        }

        public string Text { get; set; }

        /// <summary>A second, muted text on the right of the label (e.g. "from 18"); null = none.</summary>
        public string? Note { get; set; }

        /// <summary>App data of the option.</summary>
        public object? Tag { get; set; }
    }

    /// <summary>
    ///     A choice between options side by side (<see cref="Orientation.Horizontal"/>, like "Light / System / Dark") or
    ///     stacked (<see cref="Orientation.Vertical"/>, like a list of plans), equally sized. A selection
    ///     <see cref="Indicator"/> - a real control, so deformation, glow and the like work on it - slides between them
    ///     as <see cref="IndicatorMotion"/> says (the theme's <see cref="PortableTheme.SegmentedIndicatorMotion"/>, a drop
    ///     by default). Labels take <see cref="SelectedTextColor"/> exactly where the indicator covers them, so the colours
    ///     cross over with it. With <see cref="SegmentSpacing"/> and an <see cref="OptionBrush"/> each option is a face of
    ///     its own (a row of pills) and the indicator covers one face. Edit <see cref="Segments"/> and the options follow on
    ///     the next layout pass (after changing an option in place call <see cref="Refresh"/>).
    /// </summary>
    public class SegmentedControl : Control
    {
        private readonly List<Option> _options = new List<Option>();
        private readonly List<Option> _covered = new List<Option>();
        private readonly Border _indicator;
        private readonly CoverLayer _cover;
        private readonly PartMoveProgress _move = new PartMoveProgress();
        private Orientation _orientation = Orientation.Horizontal;
        private int _selectedIndex = -1;
        private int _shownIndex = -1;
        private Vector2 _from;
        private Vector2 _to;
        private Vector2 _offset;
        private Vector2 _slotStep;
        private TimeSpan _lastStep;
        private bool _hasLastStep;
        private bool _optionsDirty = true;
        private PartMotion? _indicatorMotion;
        private PartMotion? _attachedMotion;
        private Deformation? _givenDeformation;
        private float _itemHeight;
        private float _indicatorInset;
        private float _segmentSpacing;
        private Brush? _optionBrush;
        private CornerRadius? _optionCornerRadius;
        private UIFont? _dynamicFont;
        private UIFont? _noteFont;
        private float _textSize;
        private Color _textColor;
        private Color _selectedTextColor;
        private Color _noteColor;
        private Color _selectedNoteColor;

        public SegmentedControl()
        {
            var theme = PortableTheme.ResolveCurrent();

            IsFocusable = true;
            ShowFocusVisual = true;
            BackgroundBrush = theme.SegmentedTrackBrush;
            CornerRadius = theme.SegmentedCornerRadius;
            _itemHeight = theme.SegmentedItemHeight;
            _indicatorInset = theme.SegmentedIndicatorInset;
            _segmentSpacing = theme.SegmentedSpacing;
            _optionBrush = theme.SegmentedOptionBrush;
            _textSize = theme.TextSize;
            _textColor = theme.SegmentedTextColor;
            _selectedTextColor = theme.SegmentedSelectedTextColor;
            _noteColor = theme.SegmentedNoteColor;
            _selectedNoteColor = theme.SegmentedSelectedNoteColor;
            _indicator = new Border
            {
                BackgroundBrush = theme.SegmentedIndicatorBrush,
                IsHitTestVisible = false,
                IsFocusable = false,
                Parent = this
            };
            _cover = new CoverLayer(this) { Parent = this };
            KeyDown += (_, args) =>
            {
                var step = args.Key switch
                {
                    Microsoft.Xna.Framework.Input.Keys.Left when _orientation == Orientation.Horizontal => -1,
                    Microsoft.Xna.Framework.Input.Keys.Right when _orientation == Orientation.Horizontal => 1,
                    Microsoft.Xna.Framework.Input.Keys.Up when _orientation == Orientation.Vertical => -1,
                    Microsoft.Xna.Framework.Input.Keys.Down when _orientation == Orientation.Vertical => 1,
                    _ => 0
                };
                if (step == 0 || Segments.Count == 0)
                    return;
                var index = Math.Clamp(_selectedIndex + step, 0, Segments.Count - 1);
                if (index == _selectedIndex)
                    return;
                SelectedIndex = index;
                args.Handled = true;
            };
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (ReferenceEquals(BackgroundBrush, oldTheme.SegmentedTrackBrush))
                BackgroundBrush = newTheme.SegmentedTrackBrush;
            if (CornerRadius.Equals(oldTheme.SegmentedCornerRadius))
                CornerRadius = newTheme.SegmentedCornerRadius;
            if (ReferenceEquals(_indicator.BackgroundBrush, oldTheme.SegmentedIndicatorBrush))
                _indicator.BackgroundBrush = newTheme.SegmentedIndicatorBrush;
            if (_itemHeight.Equals(oldTheme.SegmentedItemHeight))
                ItemHeight = newTheme.SegmentedItemHeight;
            if (_indicatorInset.Equals(oldTheme.SegmentedIndicatorInset))
                IndicatorInset = newTheme.SegmentedIndicatorInset;
            if (_segmentSpacing.Equals(oldTheme.SegmentedSpacing))
                SegmentSpacing = newTheme.SegmentedSpacing;
            if (ReferenceEquals(_optionBrush, oldTheme.SegmentedOptionBrush))
                OptionBrush = newTheme.SegmentedOptionBrush;
            if (_textSize.Equals(oldTheme.TextSize))
                TextSize = newTheme.TextSize;
            if (_textColor.Equals(oldTheme.SegmentedTextColor))
                TextColor = newTheme.SegmentedTextColor;
            if (_selectedTextColor.Equals(oldTheme.SegmentedSelectedTextColor))
                SelectedTextColor = newTheme.SegmentedSelectedTextColor;
            if (_noteColor.Equals(oldTheme.SegmentedNoteColor))
                NoteColor = newTheme.SegmentedNoteColor;
            if (_selectedNoteColor.Equals(oldTheme.SegmentedSelectedNoteColor))
                SelectedNoteColor = newTheme.SegmentedSelectedNoteColor;
        }

        /// <summary>The options, in order.</summary>
        public List<Segment> Segments { get; } = new List<Segment>();

        /// <summary>Adds an option; returns this control for chaining.</summary>
        public SegmentedControl Add(string text, string? note = null)
        {
            Segments.Add(new Segment(text, note));
            InvalidateLayout(true);
            return this;
        }

        /// <summary>Rebuilds the options after <see cref="Segments"/> changed in place.</summary>
        public void Refresh()
        {
            _optionsDirty = true;
            InvalidateLayout(true);
        }

        /// <summary>Side by side (default) or stacked.</summary>
        public Orientation Orientation
        {
            get => _orientation;
            set
            {
                if (_orientation == value)
                    return;
                _orientation = value;
                Refresh();
            }
        }

        /// <summary>The selected option, -1 for none. Setting it moves the indicator.</summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value < -1)
                    value = -1;
                if (_selectedIndex == value)
                    return;
                _selectedIndex = value;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                RequestRedraw();
            }
        }

        /// <summary>The selected option, or null.</summary>
        public Segment? SelectedSegment => _selectedIndex >= 0 && _selectedIndex < Segments.Count ? Segments[_selectedIndex] : null;

        /// <summary>Raised when <see cref="SelectedIndex"/> changes (by a tap, a key or code).</summary>
        public event EventHandler? SelectionChanged;

        /// <summary>The selection indicator as a control (its brush, deformation, glow and the like are free to set; its
        /// corner radius follows <see cref="OptionCornerRadius"/>). It rests on the first option and slides by its
        /// <see cref="Control.Translation"/>, which this control drives.</summary>
        public Border Indicator => _indicator;

        /// <summary>The control of option <paramref name="index"/> (its <see cref="Control.ClippingRect"/> is where it is,
        /// e.g. the origin of a reveal), or null outside the range or before the first layout pass.</summary>
        public Control? GetOption(int index) => index >= 0 && index < _options.Count ? _options[index] : null;

        /// <summary>Gap between neighbouring options (default from <see cref="PortableTheme.SegmentedSpacing"/>, 0).</summary>
        public float SegmentSpacing
        {
            get => _segmentSpacing;
            set
            {
                if (_segmentSpacing.Equals(value))
                    return;
                _segmentSpacing = Math.Max(0, value);
                InvalidateLayout(true);
            }
        }

        /// <summary>Face drawn under each option (a pill per option with <see cref="SegmentSpacing"/>); null = none, the
        /// options lie on the track (<see cref="Control.BackgroundBrush"/>, which may be null).</summary>
        public Brush? OptionBrush
        {
            get => _optionBrush;
            set
            {
                if (ReferenceEquals(_optionBrush, value))
                    return;
                _optionBrush = value;
                InvalidateLayout(false);
            }
        }

        /// <summary>Corners of the option faces and the indicator; null = the track's corners less
        /// <see cref="IndicatorInset"/>. Large values make pills.</summary>
        public CornerRadius? OptionCornerRadius
        {
            get => _optionCornerRadius;
            set
            {
                if (Nullable.Equals(_optionCornerRadius, value))
                    return;
                _optionCornerRadius = value;
                InvalidateLayout(true);
            }
        }

        /// <summary>Runtime font of the labels (e.g. a semibold face); null = the theme's.</summary>
        public UIFont? DynamicFont
        {
            get => _dynamicFont;
            set
            {
                if (ReferenceEquals(_dynamicFont, value))
                    return;
                _dynamicFont = value;
                Refresh();
            }
        }

        /// <summary>Runtime font of the notes; null = the theme's.</summary>
        public UIFont? NoteFont
        {
            get => _noteFont;
            set
            {
                if (ReferenceEquals(_noteFont, value))
                    return;
                _noteFont = value;
                Refresh();
            }
        }

        /// <summary>How the indicator travels (<see cref="PartMotion"/>); null = the theme's
        /// <see cref="PortableTheme.SegmentedIndicatorMotion"/>.</summary>
        public PartMotion? IndicatorMotion
        {
            get => _indicatorMotion;
            set
            {
                if (_indicatorMotion == value)
                    return;
                _indicatorMotion = value;
                RequestRedraw();
            }
        }

        private PartMotion EffectiveIndicatorMotion => _indicatorMotion ?? ResolveTheme().SegmentedIndicatorMotion ?? PartMotion.None;

        /// <summary>Height of a side-by-side control, or of each stacked option, unless its text needs more.</summary>
        public float ItemHeight
        {
            get => _itemHeight;
            set
            {
                if (_itemHeight.Equals(value))
                    return;
                _itemHeight = value;
                InvalidateLayout(true);
            }
        }

        /// <summary>Gap between the track's edge and the options (the indicator sits inside it).</summary>
        public float IndicatorInset
        {
            get => _indicatorInset;
            set
            {
                if (_indicatorInset.Equals(value))
                    return;
                _indicatorInset = value;
                InvalidateLayout(true);
            }
        }

        public float TextSize
        {
            get => _textSize;
            set
            {
                if (_textSize.Equals(value))
                    return;
                _textSize = value;
                Refresh();
            }
        }

        /// <summary>Label colour outside the indicator.</summary>
        public Color TextColor
        {
            get => _textColor;
            set => SetColor(ref _textColor, value);
        }

        /// <summary>Label colour where the indicator covers it.</summary>
        public Color SelectedTextColor
        {
            get => _selectedTextColor;
            set => SetColor(ref _selectedTextColor, value);
        }

        /// <summary>Colour of a <see cref="Segment.Note"/> outside the indicator.</summary>
        public Color NoteColor
        {
            get => _noteColor;
            set => SetColor(ref _noteColor, value);
        }

        /// <summary>Colour of a <see cref="Segment.Note"/> where the indicator covers it.</summary>
        public Color SelectedNoteColor
        {
            get => _selectedNoteColor;
            set => SetColor(ref _selectedNoteColor, value);
        }

        public override IEnumerable<Control> GetDescendants()
        {
            yield return _indicator;
            foreach (var option in _options)
                yield return option;
            yield return _cover;
        }

        protected internal override int VisualChildCount => _options.Count + 2;

        protected internal override Control GetVisualChild(int index)
            => index == 0 ? _indicator : index <= _options.Count ? _options[index - 1] : _cover;

        public override Size MeasureLayout()
        {
            if (IsGoneShown)
                return Size.Empty;
            SyncOptions();

            var inset = _indicatorInset;
            var widest = 0f;
            var tallest = _itemHeight - inset * 2;
            foreach (var option in _options)
            {
                var size = option.Measure();
                widest = Math.Max(widest, size.Width);
                tallest = Math.Max(tallest, size.Height);
            }

            var count = Math.Max(1, _options.Count);
            var gaps = _segmentSpacing * (count - 1);
            var width = _orientation == Orientation.Horizontal ? widest * count + gaps : widest;
            var height = _orientation == Orientation.Horizontal ? tallest : tallest * count + gaps;
            return ApplyConstraints(new Size(
                Width.IsFixed() ? Width : width + inset * 2,
                Height.IsFixed() ? Height : height + inset * 2)) + Margin;
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            SyncOptions();
            var inner = ClippingRect - new Thickness(_indicatorInset);
            var count = Math.Max(1, _options.Count);
            var horizontal = _orientation == Orientation.Horizontal;
            var gaps = _segmentSpacing * (count - 1);
            var slot = horizontal
                ? new Rect(inner.Left, inner.Top, Math.Max(0, inner.Width - gaps) / count, inner.Height)
                : new Rect(inner.Left, inner.Top, inner.Width, Math.Max(0, inner.Height - gaps) / count);
            _slotStep = horizontal ? new Vector2(slot.Width + _segmentSpacing, 0) : new Vector2(0, slot.Height + _segmentSpacing);
            for (var i = 0; i < _options.Count; i++)
            {
                var place = slot + new PointF(_slotStep.X * i, _slotStep.Y * i);
                _options[i].UpdateLayout(place);
                _covered[i].UpdateLayout(place);
            }

            _indicator.CornerRadius = OptionRadius;
            _indicator.UpdateLayout(slot);
            _cover.UpdateLayout(slot);
            // A new width moves the indicator's place along with the options.
            if (!_move.IsMoving)
                _offset = _to = _from = _slotStep * Math.Max(0, _shownIndex);
            PlaceIndicator();
        }

        internal override void OnFrameUpdate()
        {
            base.OnFrameUpdate();
            AdvanceAnimation();
        }

        private CornerRadius OptionRadius
        {
            get
            {
                if (_optionCornerRadius is { } own)
                    return own;
                var radius = CornerRadius;
                return new CornerRadius(
                    Math.Max(0, radius.TopLeft - _indicatorInset), Math.Max(0, radius.TopRight - _indicatorInset),
                    Math.Max(0, radius.BottomRight - _indicatorInset), Math.Max(0, radius.BottomLeft - _indicatorInset));
            }
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            if (_optionBrush == null)
                return;
            // The faces lie under the indicator and the labels.
            var radius = ToRender(OptionRadius);
            var box = ClippingRect;
            foreach (var option in _options)
            {
                var place = option.ClippingRect;
                var face = new Rect(rect.Left + ToRender(place.Left - box.Left), rect.Top + ToRender(place.Top - box.Top), ToRender(place.Width), ToRender(place.Height));
                _optionBrush.Draw(spriteBatch, new BrushContext(face, radius, RenderOpacity, spriteBatch.GraphicsDevice));
            }
        }

        private void SetColor(ref Color field, Color value)
        {
            if (field == value)
                return;
            field = value;
            for (var i = 0; i < _options.Count; i++)
            {
                _options[i].SetColors(_textColor, _noteColor);
                _covered[i].SetColors(_selectedTextColor, _selectedNoteColor);
            }
            InvalidateLayout(false);
        }

        private void SyncOptions()
        {
            if (!_optionsDirty && _options.Count == Segments.Count)
                return;
            _optionsDirty = false;
            foreach (var option in _options)
                option.Parent = null;
            foreach (var option in _covered)
                option.Parent = null;
            _options.Clear();
            _covered.Clear();
            for (var i = 0; i < Segments.Count; i++)
            {
                var index = i;
                var option = new Option(Segments[i], _orientation, _textSize, _textColor, _noteColor, _dynamicFont, _noteFont) { Parent = this };
                option.Click += (_, _) => SelectedIndex = index;
                _options.Add(option);
                _covered.Add(new Option(Segments[i], _orientation, _textSize, _selectedTextColor, _selectedNoteColor, _dynamicFont, _noteFont) { IsHitTestVisible = false, Parent = _cover });
            }
        }

        private void PlaceIndicator()
        {
            var shown = _shownIndex >= 0 && _shownIndex < _options.Count;
            _indicator.IsVisible = shown;
            _cover.IsVisible = shown;
            _indicator.SetTranslationShown(_offset);
            // The cover moves with the indicator and its labels stay put, so it shows them where the indicator lies.
            _cover.SetTranslationShown(_offset);
            foreach (var option in _covered)
                option.SetTranslationShown(-_offset);
        }

        private void AdvanceAnimation()
        {
            var motion = EffectiveIndicatorMotion;
            if (!ReferenceEquals(motion, _attachedMotion))
            {
                _attachedMotion = motion;
                motion.AttachTo(_indicator, ref _givenDeformation);
            }

            var now = ScreenSystem.TotalTime;
            // Clamped: with on-demand rendering the previous frame may be seconds old.
            var seconds = _hasLastStep ? Math.Min(0.05f, (float)(now - _lastStep).TotalSeconds) : 0f;
            _lastStep = now;
            _hasLastStep = true;

            var target = _selectedIndex >= 0 && _selectedIndex < Segments.Count ? _selectedIndex : -1;
            if (target != _shownIndex)
            {
                var appearing = _shownIndex < 0 || target < 0;
                _shownIndex = target;
                var destination = _slotStep * Math.Max(0, target);
                var previous = _to - _from;
                _from = _offset;
                _to = destination;
                var distance = _to - _from;
                if (appearing || seconds <= 0 || distance == Vector2.Zero)
                {
                    _move.Stop();
                    _from = _to;
                }
                else
                {
                    var longest = Math.Max(1, _slotStep.Length() * Math.Max(1, Segments.Count - 1));
                    var lengthSquared = distance.LengthSquared();
                    _move.Start(motion, distance.Length() / longest, Vector2.Dot(previous, distance) / lengthSquared);
                }
            }
            else if (!_move.IsMoving)
            {
                return;
            }

            _move.Step(motion, seconds);
            _offset = _from + (_to - _from) * _move.Progress;
            motion.Update(_indicator, _move.Progress);
            PlaceIndicator();
            RequestRedraw();
        }

        /// <summary>The label (and note) of one option; tapping it selects it.</summary>
        private sealed class Option : Control
        {
            private readonly TextBlock _label;
            private readonly TextBlock? _note;

            public Option(Segment segment, Orientation orientation, float textSize, Color textColor, Color noteColor, UIFont? font, UIFont? noteFont)
            {
                IsFocusable = false;
                var horizontal = orientation == Orientation.Horizontal;
                _label = new TextBlock
                {
                    Text = segment.Text,
                    TextSize = textSize,
                    TextColor = textColor,
                    DynamicFont = font,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = horizontal ? HorizontalAlignment.Center : HorizontalAlignment.Left,
                    IsHitTestVisible = false,
                    Parent = this
                };
                if (segment.Note is { } note)
                {
                    _note = new TextBlock
                    {
                        Text = note,
                        TextSize = textSize * 0.85f,
                        TextColor = noteColor,
                        DynamicFont = noteFont,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        IsHitTestVisible = false,
                        Parent = this
                    };
                }
            }

            // Space between the label and its note, and around the text of a stacked option.
            private const float Gap = 8;
            private const float SidePadding = 12;

            public void SetColors(Color text, Color note)
            {
                _label.TextColor = text;
                if (_note != null)
                    _note.TextColor = note;
            }

            public override IEnumerable<Control> GetDescendants()
            {
                yield return _label;
                if (_note != null)
                    yield return _note;
            }

            protected internal override int VisualChildCount => _note != null ? 2 : 1;

            protected internal override Control GetVisualChild(int index) => index == 0 ? _label : _note!;

            public override Size MeasureLayout()
            {
                var label = _label.Measure();
                var note = _note?.Measure() ?? Size.Empty;
                var width = label.Width + (_note != null ? Gap + note.Width : 0) + SidePadding * 2;
                return new Size(width, Math.Max(label.Height, note.Height)) + Margin;
            }

            public override void UpdateLayout(Rect rect)
            {
                base.UpdateLayout(rect);
                var box = ClippingRect;
                var inner = new Rect(box.Left + SidePadding, box.Top, Math.Max(0, box.Width - SidePadding * 2), box.Height);
                if (_note == null)
                {
                    _label.UpdateLayout(inner);
                    return;
                }

                var noteWidth = _note.Measure().Width;
                if (_label.HorizontalAlignment == HorizontalAlignment.Center)
                {
                    // Side by side: label and note centred together.
                    var labelWidth = _label.Measure().Width;
                    var left = inner.Left + Math.Max(0, (inner.Width - labelWidth - Gap - noteWidth) / 2);
                    _label.UpdateLayout(new Rect(left, inner.Top, labelWidth, inner.Height));
                    _note.UpdateLayout(new Rect(left + labelWidth + Gap, inner.Top, noteWidth, inner.Height));
                    return;
                }

                _label.UpdateLayout(new Rect(inner.Left, inner.Top, Math.Max(0, inner.Width - noteWidth - Gap), inner.Height));
                _note.UpdateLayout(new Rect(inner.Right - noteWidth, inner.Top, noteWidth, inner.Height));
            }
        }

        /// <summary>Holds the labels in their selected colours, clipped to where the indicator is.</summary>
        private sealed class CoverLayer : Control
        {
            private readonly SegmentedControl _owner;

            public CoverLayer(SegmentedControl owner)
            {
                _owner = owner;
                IsFocusable = false;
                IsHitTestVisible = false;
            }

            protected internal override bool ClipsDescendants => true;

            public override IEnumerable<Control> GetDescendants() => _owner._covered;

            protected internal override int VisualChildCount => _owner._covered.Count;

            protected internal override Control GetVisualChild(int index) => _owner._covered[index];
        }
    }
}
