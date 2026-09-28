param(
    [ValidateSet('start', 'stop', 'status', 'play', 'stop-play')]
    [string]$Action = 'status'
)
$ErrorActionPreference = 'Stop'
$taskScriptName = if ($Action -in @('play', 'stop-play')) { 'game-preview.sh' } else { 'local-preview.sh' }
$taskScript = Join-Path $PSScriptRoot $taskScriptName
$taskBody = [System.IO.File]::ReadAllText($taskScript).Replace("`r`n", "`n")
if ($Action -eq 'status') {
    $taskBody += "`n" + [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'game-preview.sh')).Replace("`r`n", "`n")
}
$taskInfo = [System.Diagnostics.ProcessStartInfo]::new()
$taskInfo.FileName = 'wsl.exe'
$taskInfo.Arguments = '-d DWSG-Preview --exec /bin/sh -s -- ' + $Action
$taskInfo.UseShellExecute = $false
$taskInfo.CreateNoWindow = $true
$taskInfo.RedirectStandardInput = $true
$taskInfo.RedirectStandardOutput = $true
$taskInfo.RedirectStandardError = $true
$taskInfo.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
$taskInfo.StandardErrorEncoding = [System.Text.UTF8Encoding]::new($false)
$taskProcess = [System.Diagnostics.Process]::Start($taskInfo)
$taskOutput = $taskProcess.StandardOutput.ReadToEndAsync()
$taskError = $taskProcess.StandardError.ReadToEndAsync()
# Windows PowerShell 5.1 lacks StandardInputEncoding; send shell text as UTF-8 bytes.
$taskBytes = [System.Text.UTF8Encoding]::new($false).GetBytes($taskBody)
$taskProcess.StandardInput.BaseStream.Write($taskBytes, 0, $taskBytes.Length)
$taskProcess.StandardInput.BaseStream.Flush()
$taskProcess.StandardInput.Close()
$taskProcess.WaitForExit()
Write-Output $taskOutput.GetAwaiter().GetResult()
if ($taskProcess.ExitCode -ne 0) {
    throw ('The isolated preview command failed: ' + $taskError.GetAwaiter().GetResult())
}
