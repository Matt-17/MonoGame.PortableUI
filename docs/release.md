# Release Process

`master` is the active development and release source for the modernized project. The old `release` branch is historical and should not be used for new releases.

## Normal Release

1. Merge the release-ready changes into `master`.
2. Ensure CI is green.
3. Create an annotated version tag, for example:

   ```powershell
   git tag -a v0.2.0-alpha.1 -m "v0.2.0-alpha.1"
   git push origin v0.2.0-alpha.1
   ```

4. GitHub Actions runs `.github/workflows/publish-nuget.yml`.
5. The workflow restores tools, builds, tests, packs, validates metadata and publishes the `.nupkg` to NuGet.
6. Create a GitHub Release from the tag and include the changelog section plus package artifacts when useful.

## One-Time NuGet.org Setup

Configure Trusted Publishing on NuGet.org with:

- package: `MonoGame.PortableUI`
- repository: `Matt-17/MonoGame.PortableUI`
- workflow file: `.github/workflows/publish-nuget.yml`
- environment: `nuget`

The workflow expects the NuGet Trusted Publishing login action to provide `NUGET_API_KEY`. If NuGet requires the username input for this package owner, set `NUGET_USER` as an environment secret on the `nuget` environment.

## Branch Policy

After this modernization lands, enable branch protection on `master` and require the CI workflow before merge. The historical `release` branch can be archived or deleted after the first successful modern tag release.
