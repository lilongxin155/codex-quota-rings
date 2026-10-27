param(
    [string]$Destination = (Join-Path $env:LOCALAPPDATA 'CodexQuotaRings'),
    [string]$TaskName = 'CodexQuotaRings',
    [switch]$NoStart
)
$ErrorActionPreference = 'Stop'
$source = $PSScriptRoot
$exe = Join-Path $Destination 'CodexQuotaWidget.exe'
foreach ($name in @('CodexQuotaWidget.exe', 'CodexQuotaWidget.exe.config', 'Launch.vbs', 'Uninstall.ps1', 'Uninstall.cmd')) {
    if (-not (Test-Path -LiteralPath (Join-Path $source $name))) { throw "Missing release file: $name. Extract the complete release ZIP first." }
}
$existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($existing -and $existing.Actions.Execute -ne $exe) { throw "A different task named '$TaskName' already exists. It was not changed." }
if ($existing) { Stop-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue }
Get-CimInstance Win32_Process -Filter "Name='CodexQuotaWidget.exe'" | Where-Object { $_.ExecutablePath -eq $exe } | ForEach-Object {
    $process = Get-Process -Id $_.ProcessId -ErrorAction SilentlyContinue
    if ($process) { Stop-Process -InputObject $process -Force -ErrorAction SilentlyContinue; if (-not $process.WaitForExit(5000)) { throw 'The previous widget process did not exit.' }; $process.Dispose() }
}
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
foreach ($name in @('CodexQuotaWidget.exe', 'CodexQuotaWidget.exe.config', 'Launch.vbs', 'Uninstall.ps1', 'Uninstall.cmd')) {
    Copy-Item -LiteralPath (Join-Path $source $name) -Destination (Join-Path $Destination $name) -Force
}
$user = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
$action = New-ScheduledTaskAction -Execute $exe -WorkingDirectory $Destination
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $user
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force | Out-Null
if ($TaskName -eq 'CodexQuotaRings' -and $Destination -eq (Join-Path $env:LOCALAPPDATA 'CodexQuotaRings')) {
    $shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Codex Quota Rings.lnk'
    $shell = New-Object -ComObject WScript.Shell
    $link = $shell.CreateShortcut($shortcut)
    $link.TargetPath = Join-Path $env:WINDIR 'System32\wscript.exe'
    $link.Arguments = '"' + (Join-Path $Destination 'Launch.vbs') + '"'
    $link.WorkingDirectory = $Destination
    $link.Save()
}
if (-not $NoStart) {
    $paused = Join-Path $Destination 'paused.flag'
    if (Test-Path -LiteralPath $paused) { Remove-Item -LiteralPath $paused }
    Start-ScheduledTask -TaskName $TaskName
}
Write-Host "Installed to $Destination. The widget follows Codex when its window is open."
