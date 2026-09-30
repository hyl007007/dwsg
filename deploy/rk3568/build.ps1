param(
    [string]$DotNet = 'dotnet',
    [string]$OutputDirectory = ''
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo 'audit/rk3568-publish' }
$target = [IO.Path]::GetFullPath($OutputDirectory)
$privateConfig = Join-Path $target 'admin/include/config.php'
if ((Test-Path -LiteralPath $privateConfig) -and (Get-Item -LiteralPath $privateConfig).Length -gt 0) {
    throw 'Output contains a private PHP config; choose a fresh output directory.'
}
New-Item -ItemType Directory -Force -Path $target | Out-Null
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
Write-Output "ARM64 release prepared: $target"
