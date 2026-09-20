# Builds a self-contained release into dist\WindowsAudiobookPlayer and, when Inno Setup is installed, a setup program in dist\. Run tools\get-bass.ps1 first.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'dist\WindowsAudiobookPlayer'
if (-not (Test-Path (Join-Path $root 'lib\bass\bass.dll'))) { throw 'Run tools\get-bass.ps1 first.' }
dotnet publish (Join-Path $root 'WindowsAudiobookPlayer') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $out
Copy-Item (Join-Path $root 'lib\bass\*.dll') $out -Force
Write-Host "Published to $out"
$iscc = @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe", "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($iscc) {
  & $iscc (Join-Path $PSScriptRoot 'installer.iss') | Select-Object -Last 1
  Write-Host "Installer written to $(Join-Path $root 'dist')"
} else {
  Write-Host 'Inno Setup not found; skipped the installer. Install it with: winget install JRSoftware.InnoSetup'
}
