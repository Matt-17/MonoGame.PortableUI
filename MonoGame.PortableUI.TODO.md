# MonoGame.PortableUI TODO

Feature requests noted from consuming this library in a downstream project ("The Terra Contracts", `D:\Development\Games\The Terra Contracts`, sibling repo). Filed here so they aren't only sitting in that other repo's chat history.

## 1. Android platform support (high priority — blocks the downstream Android client)

The library currently targets `net8.0` + `MonoGame.Framework.DesktopGL` only (see `Directory.Build.props`, `src/MonoGame.PortableUI/MonoGame.PortableUI.csproj`); Android/iOS/WindowsDX were intentionally removed per the README with a note that they "can be reintroduced later on current MonoGame and .NET platform projects instead of the deprecated Xamarin/PCL toolchain."

Terra Contracts needs an Android (Google Play Store) client and its `TerraContracts.Game` project references this library directly via `ProjectReference` (not the NuGet package), so it needs an Android-capable build of `MonoGame.PortableUI` to unblock `TerraContracts.Android`.

Suggested approach (verified feasible on this machine: .NET 10 SDK 10.0.301, Android workload 36.1.53 already installed):

- Multi-target `src/MonoGame.PortableUI/MonoGame.PortableUI.csproj` as `net8.0;net8.0-android` (or `net10.0;net10.0-android` if the library moves to .NET 10 — see item 3), with the `MonoGame.Framework.DesktopGL` PackageReference conditional on the non-Android TFM and `MonoGame.Framework.Android` conditional on the Android TFM.
- `MonoGame.Framework.Android` 3.8.4.1 declares its NuGet dependency group for `net8.0-android34.0`, but a consuming app targeting a newer Android TFM (tested: `net10.0-android`) resolves it fine (verified locally: builds clean).
- Things to verify specifically for this library once the Android TFM exists: touch input routing (the library already has touch handling in `Screen.cs`/`Control.cs` used for the DesktopGL demo's touch paths — confirm it behaves correctly for real Android touch events, not just simulated), on-screen keyboard interaction (`ShowKeyboard`/`HideKeyboard`/`SurfaceFocusManager`), clipboard service (there's a `WindowsClipboardService` — Android needs its own `IClipboardService` implementation or `NullClipboardService` fallback), and app lifecycle (pause/resume, `GraphicsDevice` reset) since `ScreenEngine`/`FontManager` hold static state.
- `samples/MonoGame.PortableUI.Demo` is DesktopGL-only; consider whether it's worth adding a minimal Android sample project (mirroring what `SampleClient.Android` used to be) to exercise this in CI, or whether the downstream Terra Contracts Android client is sufficient as a real-world test bed.

## 2. Data grid / table control (lower priority, not urgent)

Terra Contracts is a tycoon/economy game with a lot of tabular UI (market prices, ship lists, contracts, balance sheets). The library currently has `ListBox` and the layout `Grid`, but no dedicated sortable/scrollable data-grid-style control with columns. Not needed immediately — flagging so it's not forgotten if/when Terra Contracts needs it; `ListBox` + manually laid out `Grid` rows may be enough for a while.

## 3. .NET 10

Not urgent, but noting since it came up: the library is pinned to `net8.0` today. A consuming `net10.0` project can already reference it fine (forward TFM compatibility, verified in Terra Contracts' `TerraContracts.Game` → `MonoGame.PortableUI` reference), so there's no hard requirement to move off net8.0 immediately. Worth revisiting once item 1 (Android) is settled, so the Android TFM choice and a potential net10.0 move happen together rather than twice.
