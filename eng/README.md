# Engine Scripts

Run `./eng/build-effects.ps1` from the repository root after editing files in `src/MonoGame.PortableUI/Effects/src`.

The generated `.ogl.mgfxo` files in `src/MonoGame.PortableUI/Effects/compiled` are embedded by the library. Runtime effect loading is best-effort; PortableUI falls back to CPU paths when an effect is missing or rejected by the graphics device.
