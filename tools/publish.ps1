# Builds a self-contained release into dist\WindowsAudiobookPlayer. Run tools\get-bass.ps1 first.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'dist\WindowsAudiobookPlayer'
if (-not (Test-Path (Join-Path $root 'lib\bass\bass.dll'))) { throw 'Run tools\get-bass.ps1 first.' }
dotnet publish (Join-Path $root 'WindowsAudiobookPlayer') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $out
Copy-Item (Join-Path $root 'lib\bass\*.dll') $out -Force
Write-Host "Published to $out"
