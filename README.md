# MonoGame.PortableUI

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

To edit demo content:

```powershell
dotnet mgcb-editor samples/MonoGame.PortableUI.Demo/Content/Content.mgcb
```

## Projects

- `src/MonoGame.PortableUI` contains the library.
- `samples/MonoGame.PortableUI.Demo` contains the DesktopGL demo and MGCB content.
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
