param(
    [Parameter(Mandatory = $true)][string]$ClientPath,
    [Parameter(Mandatory = $true)][string]$ServerAddress,
    [ValidateRange(1, 65535)][int]$AuthPort = 18080,
    [ValidateRange(1, 65535)][int]$GamePort = 18081,
    [string]$WorldId = 'main',
    [switch]$Headless
)
$ErrorActionPreference = 'Stop'
$client = (Resolve-Path -LiteralPath $ClientPath).Path
if ([Uri]::CheckHostName($ServerAddress) -eq [UriHostNameType]::Unknown) { throw 'Invalid server address.' }
if ([string]::IsNullOrWhiteSpace($WorldId)) { throw 'World ID is required.' }
$server = if ($ServerAddress.Contains(':')) { "[$ServerAddress]" } else { $ServerAddress }
$launch = New-Object System.Diagnostics.ProcessStartInfo
$launch.FileName = $client
$launch.WorkingDirectory = Split-Path $client
$launch.UseShellExecute = $false
$launch.CreateNoWindow = $true
$launch.EnvironmentVariables['DWSG_AUTH_URL'] = "http://${server}:${AuthPort}/api.php?appid=1"
$launch.EnvironmentVariables['DWSG_GAME_URL'] = "http://${server}:${GamePort}"
$launch.EnvironmentVariables['DWSG_WORLD_ID'] = $WorldId
# Bypass proxies only in this child process; do not change the user's system settings.
foreach ($key in @('HTTP_PROXY','HTTPS_PROXY','ALL_PROXY','http_proxy','https_proxy','all_proxy')) { $launch.EnvironmentVariables.Remove($key) }
$launch.EnvironmentVariables['NO_PROXY'] = "$ServerAddress,127.0.0.1,localhost"
if ($Headless) {
    $launch.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $launch.Arguments = '-batchmode -nographics'
}
$process = [Diagnostics.Process]::Start($launch)
Write-Output "Client PID: $($process.Id); server: ${server}:${GamePort}; world: $WorldId"
