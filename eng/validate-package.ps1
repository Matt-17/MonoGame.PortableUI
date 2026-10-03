param(
    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory,

    [string]$ExpectedVersion,

    # Every package the release must contain; a missing one fails the build instead of shipping
    # a partial release.
    [string[]]$ExpectedPackageIds = @()
)

$ErrorActionPreference = 'Stop'

$packages = Get-ChildItem -LiteralPath $PackageDirectory -Filter '*.nupkg'
$symbols = Get-ChildItem -LiteralPath $PackageDirectory -Filter '*.snupkg'

if ($packages.Count -eq 0) {
    throw "No .nupkg files found in $PackageDirectory."
}

if ($symbols.Count -eq 0) {
    throw "No .snupkg files found in $PackageDirectory."
}

foreach ($id in $ExpectedPackageIds) {
    $name = if ([string]::IsNullOrWhiteSpace($ExpectedVersion)) { "$id.*.nupkg" } else { "$id.$ExpectedVersion.nupkg" }
    if (-not (Get-ChildItem -LiteralPath $PackageDirectory -Filter $name)) {
        throw "Expected package $name was not built."
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

foreach ($package in $packages) {
    $zip = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        $nuspec = $zip.Entries | Where-Object { $_.FullName.EndsWith('.nuspec') } | Select-Object -First 1
        if ($null -eq $nuspec) {
            throw "No nuspec found in $($package.Name)."
        }

        $reader = New-Object System.IO.StreamReader($nuspec.Open())
        try {
            $content = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        if ($content.Contains('<iconUrl>')) {
            throw "$($package.Name) contains deprecated NuGet metadata."
        }

        if ($content.Contains('<licenseUrl>') -and -not $content.Contains('<licenseUrl>https://licenses.nuget.org/MIT</licenseUrl>')) {
            throw "$($package.Name) contains legacy licenseUrl metadata."
        }

        if (-not $content.Contains('<license type="expression">MIT</license>')) {
            throw "$($package.Name) does not contain the MIT license expression."
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedVersion)) {
            [xml]$document = $content
            $versionNode = $document.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='version']")

            if ($null -eq $versionNode) {
                throw "$($package.Name) does not contain a package version."
            }

            if ($versionNode.InnerText -ne $ExpectedVersion) {
                throw "$($package.Name) has version $($versionNode.InnerText), expected $ExpectedVersion."
            }
        }
    }
    finally {
        $zip.Dispose()
    }
}
