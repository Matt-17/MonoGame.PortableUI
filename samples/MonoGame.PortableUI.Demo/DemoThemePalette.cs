using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Demo
{
    public sealed class DemoThemePalette
    {
        public static DemoThemePalette Empty { get; } = new DemoThemePalette();

        public Color Background { get; init; }
        public Color Surface { get; init; }
        public Color SurfaceAlt { get; init; }
        public Color Text { get; init; }
        public Color HeadingText { get; init; }
        public Color MutedText { get; init; }
        public Color Primary { get; init; }
        public Color Secondary { get; init; }
        public Color Warning { get; init; }
        public Color Danger { get; init; }
        public Color Info { get; init; }
        public Color Selection { get; init; }
        public Color SelectionText { get; init; }
        public Color TabText { get; init; }
        public Color SelectedTabText { get; init; }
        public Color FieldFrame { get; init; }
        public Color FieldBorder { get; init; }
        public Color DisabledSurface { get; init; }
        public Color DisabledText { get; init; }
    }
}
