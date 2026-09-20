# Downloads the BASS audio libraries (x64) into lib\bass\. Run once after cloning.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'lib\bass'
New-Item -ItemType Directory -Force $out | Out-Null
$tmp = Join-Path ([IO.Path]::GetTempPath()) ('bass-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $tmp | Out-Null
$packages = @(
  @{ zip = 'bass24.zip';     dll = 'bass.dll' },
  @{ zip = 'bass_fx24.zip';  dll = 'bass_fx.dll'; url = 'https://www.un4seen.com/files/z/0/bass_fx24.zip' },
  @{ zip = 'bassflac24.zip'; dll = 'bassflac.dll' },
  @{ zip = 'bassopus24.zip'; dll = 'bassopus.dll' },
  @{ zip = 'basswv24.zip';   dll = 'basswv.dll' }
)
foreach ($p in $packages) {
  $zip = Join-Path $tmp $p.zip
  $url = if ($p.url) { $p.url } else { "https://www.un4seen.com/files/" + $p.zip }
  Invoke-WebRequest -Uri $url -OutFile $zip
  $dir = Join-Path $tmp ([IO.Path]::GetFileNameWithoutExtension($p.zip))
  Expand-Archive -Path $zip -DestinationPath $dir -Force
  $dll = Get-ChildItem -Path $dir -Recurse -Filter $p.dll | Where-Object { $_.FullName -match 'x64' } | Select-Object -First 1
  if (-not $dll) { throw "x64 $($p.dll) not found in $($p.zip)" }
  Copy-Item $dll.FullName (Join-Path $out $p.dll) -Force
  Write-Host "Installed $($p.dll)"
}
Remove-Item $tmp -Recurse -Force
