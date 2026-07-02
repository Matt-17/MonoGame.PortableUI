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

## Small API Examples

Initialize the screen engine from your `Game` and keep the layout in sync with the window:

```csharp
private ScreenEngine? _screenEngine;

protected override void Initialize()
{
    _screenEngine = ScreenEngine.Initialize(this);
    _screenEngine.SetScreenSize(Window.ClientBounds.Width, Window.ClientBounds.Height);
    Window.ClientSizeChanged += (_, _) =>
        _screenEngine.SetScreenSize(Window.ClientBounds.Width, Window.ClientBounds.Height);
    base.Initialize();
}

protected override void LoadContent()
{
    FontManager.LoadFonts(this, "Segoe", "default");
    _screenEngine?.NavigateToScreen(new MainScreen());
}
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
var textBox = new TextBox { HintText = "Player name", Height = 36 };
textBox.TextChanged += (_, args) => Console.WriteLine(args.NewText);

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
