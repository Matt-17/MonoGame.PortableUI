# MonoGame.PortableUI

[![NuGet](https://img.shields.io/nuget/v/MonoGame.PortableUI?label=NuGet)](https://www.nuget.org/packages/MonoGame.PortableUI)
[![CI](https://github.com/Matt-17/MonoGame.PortableUI/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/Matt-17/MonoGame.PortableUI/actions/workflows/ci.yml)
[![Publish NuGet](https://github.com/Matt-17/MonoGame.PortableUI/actions/workflows/release-nuget.yml/badge.svg)](https://github.com/Matt-17/MonoGame.PortableUI/actions/workflows/release-nuget.yml)
[![Release source](https://img.shields.io/badge/release%20source-master-blue)](https://github.com/Matt-17/MonoGame.PortableUI/tree/master)
[![License](https://img.shields.io/github/license/Matt-17/MonoGame.PortableUI)](https://github.com/Matt-17/MonoGame.PortableUI/blob/master/LICENSE.md)

MonoGame.PortableUI is a lightweight code-first UI layer for MonoGame. The modernized package line targets .NET 8 and the current stable MonoGame DesktopGL package line.

This branch intentionally removes the legacy PCL, Xamarin Android/iOS and WindowsDX project set. DesktopGL is the verified demo platform for this release line; mobile platforms can be reintroduced later on current MonoGame and .NET platform projects instead of the deprecated Xamarin/PCL toolchain.

## Quick Start

```powershell
dotnet tool restore
dotnet restore
dotnet build
dotnet test
dotnet run --project samples/MonoGame.PortableUI.Demo
```

Run the layout benchmarks manually when working on performance:

```powershell
dotnet run --project benchmarks/MonoGame.PortableUI.Benchmarks --configuration Release -- --filter *Layout*
```

To edit demo content:

```powershell
dotnet mgcb-editor samples/MonoGame.PortableUI.Demo/Content/Content.mgcb
```

## Theme Gallery

Regenerate the gallery images from the demo:

```powershell
dotnet run --project samples/MonoGame.PortableUI.Demo -- --theme glass --screenshot docs/themes --screenshot-screen gallery
```

The `default` theme shows the library's built-in styling when no theme is applied:

| Default (no theme) |
|---|
| ![Default](docs/themes/default.png) |

| C64 | Game Boy | NES | Mac 1-bit |
|---|---|---|---|
| ![C64](docs/themes/c64.png) | ![Game Boy](docs/themes/gameboy.png) | ![NES](docs/themes/nes.png) | ![Mac 1-bit](docs/themes/mac1bit.png) |

| DOS | Norton | Phosphor | Amber |
|---|---|---|---|
| ![DOS](docs/themes/dos.png) | ![Norton](docs/themes/norton.png) | ![Phosphor](docs/themes/phosphor.png) | ![Amber](docs/themes/amber.png) |

| Amiga | Windows 95 | Mac OS 9 | NeXTSTEP |
|---|---|---|---|
| ![Amiga](docs/themes/amiga.png) | ![Windows 95](docs/themes/win95.png) | ![Mac OS 9](docs/themes/macos9.png) | ![NeXTSTEP](docs/themes/nextstep.png) |

| BeOS | Luna | Aqua | Aero |
|---|---|---|---|
| ![BeOS](docs/themes/beos.png) | ![Luna](docs/themes/luna.png) | ![Aqua](docs/themes/aqua.png) | ![Aero](docs/themes/aero.png) |

| Metro | Fluent | Material | Frosted Glass |
|---|---|---|---|
| ![Metro](docs/themes/metro.png) | ![Fluent](docs/themes/fluent.png) | ![Material](docs/themes/material.png) | ![Frosted Glass](docs/themes/glass.png) |

| Liquid Glass | Aurora | Terminal | Studio |
|---|---|---|---|
| ![Liquid Glass](docs/themes/liquid.png) | ![Aurora](docs/themes/aurora.png) | ![Terminal](docs/themes/terminal.png) | ![Studio](docs/themes/studio.png) |

| Cyberpunk | Vaporwave | Nord | Dracula |
|---|---|---|---|
| ![Cyberpunk](docs/themes/cyberpunk.png) | ![Vaporwave](docs/themes/vaporwave.png) | ![Nord](docs/themes/nord.png) | ![Dracula](docs/themes/dracula.png) |

| Solarized Light | Solarized Dark | Gruvbox | Parchment |
|---|---|---|---|
| ![Solarized Light](docs/themes/solarized-light.png) | ![Solarized Dark](docs/themes/solarized-dark.png) | ![Gruvbox](docs/themes/gruvbox.png) | ![Parchment](docs/themes/parchment.png) |

| LCARS | E-Ink | Neumorphism | Brutalist |
|---|---|---|---|
| ![LCARS](docs/themes/lcars.png) | ![E-Ink](docs/themes/eink.png) | ![Neumorphism](docs/themes/neumorphic.png) | ![Brutalist](docs/themes/brutalist.png) |

## Small API Examples

Initialize the screen engine from your `Game`. By default, PortableUI tracks the MonoGame viewport and keeps the layout in sync with the window:

```csharp
private ScreenEngine? _screenEngine;

protected override void Initialize()
{
    _screenEngine = ScreenEngine.Initialize(this);
    base.Initialize();
}

protected override void LoadContent()
{
    FontManager.LoadFonts(this, "Segoe", "default");
    _screenEngine?.NavigateToScreen(new MainScreen());
}
```

If your UI uses a virtual coordinate space instead of the actual backbuffer size, switch to manual sizing:

```csharp
protected override void Initialize()
{
    _screenEngine = ScreenEngine.Initialize(this, new ScreenEngineOptions
    {
        ScreenSizeMode = ScreenSizeMode.Manual
    });
    _screenEngine.SetScreenSize(1280, 720);
    base.Initialize();
}
```

Desktop hosts can opt into system clipboard support through the engine options:

```csharp
_screenEngine = ScreenEngine.Initialize(this, new ScreenEngineOptions
{
    ClipboardService = OperatingSystem.IsWindows() ? new WindowsClipboardService() : NullClipboardService.Instance
});
```

Build screens with code-first controls:

```csharp
public sealed class MainScreen : Screen
{
    public MainScreen()
    {
        BackgroundBrush = Color.Black;

        var layout = new Grid
        {
            Margin = 16,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition()
            }
        };

        layout.AddChild(new TextBlock
        {
            Text = "Inventory",
            TextColor = Color.White,
            TextSize = 18
        });

        var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 12) };
        var save = new TextButton("Save") { Height = 40, BackgroundBrush = Color.White };
        save.Click += (_, _) => ScreenEngine?.NavigateToScreen(new DetailsScreen());
        stack.AddChild(save);

        layout.AddChild(stack, row: 1);
        Content = layout;
    }
}
```

Handle text input and selection events:

```csharp
var textBox = new TextBox { HintText = "Player name", MaxLength = 24, Height = 36 };
textBox.TextChanged += (_, args) => Console.WriteLine(args.NewText);
textBox.SelectAll();

var password = new TextBox { HintText = "Password", PasswordChar = '*', Height = 36 };

var notes = new TextBox { HintText = "Notes", IsMultiline = true, Height = 96 };
notes.EnterPressed += (_, _) => Console.WriteLine("Submitted with Ctrl+Enter");

var combo = new ComboBox { Height = 36 };
combo.Items.Add("Compact");
combo.Items.Add("Touch");
combo.SelectionChanged += (_, _) => Console.WriteLine(combo.SelectedItem);
combo.SelectedIndex = 0;
```

## Projects

- `src/MonoGame.PortableUI` contains the library.
- `samples/MonoGame.PortableUI.Demo` contains the DesktopGL demo and MGCB content.
- `benchmarks/MonoGame.PortableUI.Benchmarks` contains BenchmarkDotNet layout and visual-tree baselines.
- `tests/MonoGame.PortableUI.Tests` contains windowless regression tests for layout, state, input and composite controls.
- `docs/issues.md` maps the historical GitHub issue backlog to fixes, tests or obsolete platform notes.

## Fonts

The demo bundles only free, open-licensed fonts (SIL OFL 1.1, CC BY-SA 4.0 — all usable in
commercial projects; no proprietary system fonts are redistributed). The full per-font list with
authors, licenses and sources lives in [docs/FONTS.md](docs/FONTS.md).

## Packaging

The package ID remains `MonoGame.PortableUI`. Packaging is SDK-based and uses modern NuGet metadata:

- `PackageLicenseExpression=MIT`
- `PackageReadmeFile=README.md`
- repository metadata and SourceLink
- symbol packages via `.snupkg`

The project file uses `0.0.0-local` as a local fallback. Published package versions are derived from release tags such as `v0.2.0-alpha.1`.

## Release Flow

`master` is the release source. The old `release` branch is no longer used for active development.

1. Merge changes into `master`.
2. Create a tag such as `v0.2.0-alpha.2`.
3. GitHub Actions derives the package version from the tag, then builds, tests and packs.
4. The NuGet publish workflow uses Trusted Publishing for tags.
5. NuGet.org still needs the one-time trusted publisher setup for this repository, `release-nuget.yml` workflow file and `NuGet` environment.
