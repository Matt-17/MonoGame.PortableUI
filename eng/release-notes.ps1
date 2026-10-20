<#
.SYNOPSIS
    Prints (or writes) the CHANGELOG.md section of one version: the text under "## <Version>" up to the next "## ".
    Used by the Publish NuGet workflow for the GitHub release; fails when the section is missing or empty.
#>
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$Changelog = (Join-Path $PSScriptRoot '..' 'CHANGELOG.md'),
    [string]$OutFile
)

$lines = Get-Content -Path $Changelog -Encoding utf8
$start = [array]::IndexOf($lines, "## $Version")
if ($start -lt 0) { throw "CHANGELOG.md has no section '## $Version'." }

$end = $start + 1
while ($end -lt $lines.Count -and -not $lines[$end].StartsWith('## ')) { $end++ }
$notes = if ($end -gt $start + 1) { ($lines[($start + 1)..($end - 1)] -join "`n").Trim() } else { '' }
if (-not $notes) { throw "The CHANGELOG.md section '## $Version' is empty." }

if ($OutFile) { Set-Content -Path $OutFile -Value $notes -Encoding utf8 } else { $notes }
