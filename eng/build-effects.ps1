param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root "src/MonoGame.PortableUI/Effects/src"
$compiled = Join-Path $root "src/MonoGame.PortableUI/Effects/compiled"

dotnet tool restore
New-Item -ItemType Directory -Force -Path $compiled | Out-Null

foreach ($name in @("Primitives", "Blur", "PostFx")) {
    $input = Join-Path $source "$name.fx"
    $output = Join-Path $compiled "$name.ogl.mgfxo"
    dotnet mgfxc $input $output /Profile:OpenGL
}
