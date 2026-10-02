# Android APK size

Measured on the Android demo (`samples/MonoGame.PortableUI.Demo.Android`, Release, MonoGame 3.8.5.1,
.NET 10, Pixel 5). Sizes are compressed bytes inside the APK, i.e. roughly the download.

| Step | APK |
|---|---|
| Default (arm64 + x86_64) | 12.8 MB (6.2 MB per ABI) |
| arm64 only | 6.2 MB |
| + no native HTTP handler, trimmed FontStashSharp/BMFont, no OpenAL | 4.9 MB |
| + no zlib / crypto native libraries | **4.4 MB** |
| (+ no AOT: 3.6 MB, but cold start 270 ms -> 395 ms - kept on) | |

What is left per device: Mono runtime (~1.35 MB), app + framework assemblies (~1.9 MB),
AOT images (~0.8 MB), monodroid (~0.46 MB), small native glue.

## What each measure costs an app

- **arm64 only** - no x86_64 emulators for Release builds (Debug keeps both). App bundles (.aab)
  deliver one ABI per device anyway.
- **`UseNativeHttpHandler=false`** - `HttpClient` still works with the managed handler; without HTTP
  use, `System.Net.Http`, crypto and ASN.1 are trimmed away.
- **`TrimmableAssembly` for FontStashSharp/Cyotek** - drops the unused BMFont (XML) loader. Do not mark
  `MonoGame.Framework` trimmable without preserving its content readers (created by reflection).
- **Dropping `libopenal.so`** - MonoGame audio (`SoundEffect`, `Song`) then fails. Only for silent apps.
- **Dropping `libSystem.IO.Compression.Native.so` / `libSystem.Security.Cryptography.Native.Android.so`**
  - `DeflateStream`/`ZipArchive`, TLS and crypto APIs then fail. Only when none of them is used.

See the demo's `.csproj` for the MSBuild snippets.
