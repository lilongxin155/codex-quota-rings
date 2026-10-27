param(
    [string]$Destination = (Join-Path $env:LOCALAPPDATA 'CodexQuotaRings'),
    [string]$TaskName = 'CodexQuotaRings'
)
$ErrorActionPreference = 'Stop'
$exe = Join-Path $Destination 'CodexQuotaWidget.exe'
$existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($existing -and $existing.Actions.Execute -ne $exe) { throw "A different task named '$TaskName' exists. It was not changed." }
if ($existing) { Stop-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue; Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false }
Get-CimInstance Win32_Process -Filter "Name='CodexQuotaWidget.exe'" | Where-Object { $_.ExecutablePath -eq $exe } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
if ($TaskName -eq 'CodexQuotaRings' -and $Destination -eq (Join-Path $env:LOCALAPPDATA 'CodexQuotaRings')) {
    $shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Codex Quota Rings.lnk'
    if (Test-Path -LiteralPath $shortcut) { Remove-Item -LiteralPath $shortcut }
}
foreach ($name in @('CodexQuotaWidget.exe','CodexQuotaWidget.exe.config','Launch.vbs','Uninstall.ps1','Uninstall.cmd','paused.flag','position.txt','status.json','window-status.json','last-error.txt','monitor-error.txt','fatal-error.txt')) {
    $path = Join-Path $Destination $name
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
}
if ((Test-Path -LiteralPath $Destination) -and @(Get-ChildItem -LiteralPath $Destination -Force).Count -eq 0) { Remove-Item -LiteralPath $Destination }
Write-Host 'Codex Quota Rings has been removed.'
