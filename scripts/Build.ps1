$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$output = Join-Path $projectRoot 'release'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$arguments = @('/nologo','/target:winexe','/optimize+','/debug-','/codepage:65001',('/win32manifest:' + (Join-Path $projectRoot 'src\widget.manifest')),('/out:' + (Join-Path $output 'CodexQuotaWidget.exe')),('/reference:' + (Join-Path $framework 'WPF\PresentationFramework.dll')),('/reference:' + (Join-Path $framework 'WPF\PresentationCore.dll')),('/reference:' + (Join-Path $framework 'WPF\WindowsBase.dll')),('/reference:' + (Join-Path $framework 'WPF\UIAutomationClient.dll')),('/reference:' + (Join-Path $framework 'WPF\UIAutomationTypes.dll')),'/reference:System.Xaml.dll','/reference:System.Web.Extensions.dll',(Join-Path $projectRoot 'src\Widget.cs'))
& (Join-Path $framework 'csc.exe') @arguments
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\CodexQuotaWidget.exe.config') -Destination $output -Force
Write-Host "Built: $output"
