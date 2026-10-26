$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (!(Test-Path -LiteralPath $compiler)) { throw '.NET Framework C# compiler not found.' }
$dist = Join-Path $projectRoot 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$sourceFiles = @('Engine.cs','ClassicWindow.cs','TransferFeatures.cs','CategoryFolders.cs','AppBrand.cs','DownloadErrors.cs','MediaConverter.cs') | ForEach-Object { Join-Path $projectRoot ('src\' + $_) }
$icon = Join-Path $projectRoot 'assets\nocturnal.ico'
$output = Join-Path $dist 'Nocturnal.exe'
& $compiler /nologo /target:winexe /main:Program "/out:$output" "/win32icon:$icon" "/resource:$icon,logo" /r:System.Net.Http.dll /r:System.Web.Extensions.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll @sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Copy-Item -LiteralPath $icon -Destination (Join-Path $dist 'nocturnal.ico') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets\logo.png') -Destination (Join-Path $dist 'nocturnal.png') -Force
Compress-Archive -Path (Join-Path $projectRoot 'extension-firefox\*') -DestinationPath (Join-Path $dist 'Nocturnal-Firefox-v0.7.0.zip') -Force
Write-Output "Built $output"
