# Release Process

`master` is the active development and release source for the modernized project. The old `release` branch is historical and should not be used for new releases.

## Normal Release

1. Merge the release-ready changes into `master`.
2. Ensure CI is green.
3. Create an annotated version tag, for example:

   ```powershell
   git tag -a v0.2.0-alpha.2 -m "v0.2.0-alpha.2"
   git push origin v0.2.0-alpha.2
   ```

4. GitHub Actions runs `.github/workflows/release-nuget.yml`.
5. The workflow derives the package version from the tag, restores tools, builds, tests, packs, validates metadata and publishes the `.nupkg` to NuGet.
6. Create a GitHub Release from the tag and include the changelog section plus package artifacts when useful.

Release tags must use `vMAJOR.MINOR.PATCH` or `vMAJOR.MINOR.PATCH-PRERELEASE`. The project file keeps `0.0.0-local` only as a local fallback; NuGet releases are tag-driven.

## One-Time NuGet.org Setup

Configure Trusted Publishing on NuGet.org with:

- package: `MonoGame.PortableUI`
- repository: `Matt-17/MonoGame.PortableUI`
- workflow file: `release-nuget.yml`
- environment: `NuGet`

The workflow expects the NuGet Trusted Publishing login action to provide `NUGET_API_KEY`. If NuGet requires the username input for this package owner, set `NUGET_USER` as a repository or environment variable. This value must be the NuGet username that created the trusted publishing policy, not necessarily the package owner.

## Branch Policy

After this modernization lands, enable branch protection on `master` and require the CI workflow before merge. The historical `release` branch can be archived or deleted after the first successful modern tag release.
