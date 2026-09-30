param(
    [string]$DotNet = 'dotnet',
    [string]$OutputDirectory = ''
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo 'audit/rk3568-publish' }
$target = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$audit = [IO.Path]::GetFullPath((Join-Path $repo 'audit')).TrimEnd('\')
if (-not $target.StartsWith($audit + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Release output must be a directory inside the repository audit directory.'
}
# Resolve every existing ancestor before touching the output. A junction must
# never turn an ignored build directory into source or deployed state.
for ($candidate = $target; $candidate; $candidate = Split-Path -Parent $candidate) {
    if ((Test-Path -LiteralPath $candidate) -and
        ((Get-Item -Force -LiteralPath $candidate).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw 'Release output must not traverse a symbolic link or junction.'
    }
}
$privateConfig = Join-Path $target 'admin/include/config.php'
if ((Test-Path -LiteralPath $privateConfig) -and (Get-Item -LiteralPath $privateConfig).Length -gt 0) {
    throw 'Output contains a private PHP config; choose a fresh output directory.'
}
# Reuse one output directory, but remove only files declared by a previous run.
# Unknown legacy output or user data is left untouched for operator review.
$manifest = Join-Path $target '.dwsg-release-files.json'
$owned = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
if (Test-Path -LiteralPath $manifest) {
    if ((Get-Item -Force -LiteralPath $manifest).Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw 'Release ownership manifest must not be a symbolic link.'
    }
    $previous = Get-Content -Raw -LiteralPath $manifest | ConvertFrom-Json
    if ($previous.version -ne 1) { throw 'Unsupported release ownership manifest.' }
    foreach ($relative in $previous.files) {
        $file = [IO.Path]::GetFullPath((Join-Path $target $relative))
        if (-not ($file.StartsWith((Join-Path $target 'host') + '\', [StringComparison]::OrdinalIgnoreCase) -or
                  $file.StartsWith((Join-Path $target 'admin') + '\', [StringComparison]::OrdinalIgnoreCase))) {
            throw 'Release ownership manifest contains an invalid path.'
        }
        [void]$owned.Add($file)
    }
}
foreach ($folder in @('host', 'admin')) {
    $directory = Join-Path $target $folder
    if (Test-Path -LiteralPath $directory) {
        $entries = @((Get-Item -Force -LiteralPath $directory)) + @(Get-ChildItem -Force -LiteralPath $directory -Recurse)
        foreach ($entry in $entries) {
            if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Release contains a symbolic link or junction.' }
            if (-not $entry.PSIsContainer -and -not $owned.Contains($entry.FullName)) {
                throw "Release contains a file not owned by this builder; left untouched: $($entry.FullName)"
            }
        }
    }
}
foreach ($file in $owned) {
    if (Test-Path -LiteralPath $file -PathType Leaf) { Remove-Item -Force -LiteralPath $file }
}
New-Item -ItemType Directory -Force -Path $target | Out-Null
$completed = $false
try {
    & $DotNet publish (Join-Path $repo 'server/Host/Dwsg.Host.csproj') -c Release -r linux-arm64 --self-contained true -p:UseSharedCompilation=false -o (Join-Path $target 'host')
    if ($LASTEXITCODE -ne 0) { throw 'ARM64 server publish failed.' }
    $adminTarget = Join-Path $target 'admin'
    New-Item -ItemType Directory -Force -Path $adminTarget | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $repo 'admin') -File -Recurse | ForEach-Object {
        $relative = $_.FullName.Substring((Join-Path $repo 'admin').Length + 1).Replace('\', '/')
        if ($relative -notin @('include/config.php', 'install/install.lock')) {
            $destination = Join-Path $adminTarget $relative
            New-Item -ItemType Directory -Force -Path (Split-Path $destination) | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination
        }
    }
    # The service binds its private config over this empty mount target.
    [IO.File]::WriteAllText($privateConfig, '')
    [IO.File]::WriteAllText((Join-Path $adminTarget 'install/install.lock'), 'Configured by the deployment operator.')
    $completed = $true
}
finally {
    $files = @(foreach ($folder in @('host', 'admin')) {
        $directory = Join-Path $target $folder
        if (Test-Path -LiteralPath $directory) {
            Get-ChildItem -Force -LiteralPath $directory -File -Recurse | ForEach-Object { $_.FullName.Substring($target.TrimEnd('\').Length + 1) }
        }
    })
    @{ version = 1; completed = $completed; files = $files } | ConvertTo-Json | Set-Content -Encoding UTF8 -LiteralPath $manifest
}
Write-Output "ARM64 release prepared: $target"
