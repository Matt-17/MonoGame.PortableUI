# Path drawing

**Decision:** shapes are rasterized on the CPU into a texture at the size they are drawn
(`PathGeometry` → `PathRasterizer` → `PathShape`), not triangulated into a GPU mesh per frame.

## Why

- The shapes UI code needs (icons, badges, decorative outlines, clip masks) are static or change
  rarely. Rasterizing once per pixel size and drawing one sprite per frame is the cheapest
  possible per-frame cost, and it shares the existing SpriteBatch/scissor/opacity pipeline.
- Rasterizing *at the target pixel size* (not scaling a pre-rendered bitmap) keeps edges crisp at
  every size; the 4×4 supersampling gives the same anti-aliasing quality as the rounded-rect
  masks.
- A triangulator (ear clipping plus stroke expansion and AA fringes) is a lot more code, needs a
  custom effect or vertex path outside SpriteBatch, and only pays off for shapes that change
  every frame. That is not a current requirement.

## Usage

```csharp
var star = new PathGeometry(100, 100)
    .MoveTo(50, 5).LineTo(61, 38).LineTo(95, 38).LineTo(68, 59)
    .LineTo(79, 92).LineTo(50, 72).LineTo(21, 92).LineTo(32, 59)
    .LineTo(5, 38).LineTo(39, 38).Close();

var shape = new PathShape { Geometry = star, Fill = Color.Gold, Stroke = Color.Black, StrokeWidth = 3, Width = 48, Height = 48 };
```

`PathGeometry` supports `MoveTo`, `LineTo`, `QuadraticTo`, `CubicTo`, `ArcTo`, `Close`, plus
`AddRoundedRectangle` and `AddEllipse`, with non-zero (default) or even-odd fill. Coordinates are
in the geometry's `ViewBox`; the control maps the view box onto its arranged size.

## Cost and when to choose something else

- Rasterizing costs `width × height × 16 samples × segments`; a 64×64 icon with a few dozen
  segments takes well under a millisecond. `PathShape` re-rasterizes only when its pixel size,
  geometry, colours or the graphics device (reset/context loss) change.
- Animating a shape's *size* every frame re-rasterizes every frame — animate `Scale` (a render
  transform) instead, or pre-render the few sizes you need with `PathRasterizer.CreateTexture`.
- If a project ever needs shapes whose outline changes every frame, that is the point to add a
  triangulating renderer; the `PathGeometry` API can stay the same.
