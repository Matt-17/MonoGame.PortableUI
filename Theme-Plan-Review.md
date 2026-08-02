# Theme-Plan Review

This review records refinements for `Theme-Plan.md`. It is meant to sharpen the
existing architecture, not to introduce a separate theming track.

## Theme Islands Refinements

### Summary

`Theme-Plan.md` already points toward Theme Islands in two places: T3 recommends
draw/use-time theme resolution, and D1 mentions a `ThemeScope` for live
per-subtree previews. The plan should promote that idea into a first-class
architecture detail so game UIs can use multiple themes in the same screen
without rebuilding or manually restyling every control.

### Review Notes

- Move the `ThemeScope` idea from D1 into T3. Subtree theming is core theming
  architecture, not only a demo gallery implementation detail.
- Use `ThemeIsland` as the public user-facing name. Keep `ThemeScope` only as
  an internal/technical term if needed.
- Define the resolution rule explicitly: nearest `ThemeIsland.Theme` wins,
  otherwise inherit from the parent tree, otherwise use `ScreenEngineOptions.Theme`,
  otherwise fall back to `PortableTheme.CreateDefault()`.
- Model `ThemeIsland` as a transparent container control because the current
  layout, draw, input, popup and generated-child paths are all `Control`-based.
- Make live switching explicit: assigning `ScreenEngineOptions.Theme` or
  `ThemeIsland.Theme` must update existing controls without rebuilding the tree.
- Preserve local overrides. Manually set control properties must not be
  overwritten by global or island theme changes.
- Include overlay content in the inheritance model. Tooltips, context menus,
  combo box dropdowns and flyout content should inherit the theme of the owner
  control that opened them.
- Cover generated children. Button text blocks, tab header buttons and list box
  item buttons need the same theme source as their parent control, even when
  created after the parent is attached to a `ThemeIsland`.

### Suggested Wording

- "T3 should explicitly include local theme scopes / Theme Islands. The existing
  draw-time resolution recommendation already enables this; the plan should
  define subtree theme inheritance as a first-class rule."
- "D1's live mini-preview should depend on the same public `ThemeIsland` API
  consumers use, not a demo-only rendering trick."
- "A Theme Island is visually transparent; it only changes theme resolution for
  its descendants."

### Test Additions

- Nested islands: inner theme wins, siblings keep the outer or global theme.
- Runtime island theme switching preserves focus, text input, selection and
  scroll offset.
- Manual property overrides survive both global and island theme changes.
- Later-added descendants inherit the correct island theme.
- Generated popup content inherits the theme of the owner control that opened it.

### Assumptions

- This refines T3 and D1 in `Theme-Plan.md`.
- It does not require the full future `ThemePalette` / `ControlStyle` migration
  before a useful first version can be implemented.

## Brush Implementation Follow-Up

A first release-hardening pass replaces the `TileBrush` and `NineTileBrush`
stubs with minimal production implementations for tiled texture fills and
stretched 9-slice surfaces.

This is intentionally not Brush API v2. Follow-up work should still cover
texture caching, tile and center modes, `BrushContext`, corner-radius/opacity
integration and theme-style integration after the larger rendering foundation
lands.
