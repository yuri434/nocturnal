$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$dist = Join-Path $projectRoot 'dist'
if (!(Test-Path -LiteralPath (Join-Path $dist 'Nocturnal.exe'))) { throw 'Run scripts/build.ps1 first.' }
if (Get-Process -Name Nocturnal -ErrorAction SilentlyContinue) { throw 'Close Nocturnal before installing.' }
$destination = Join-Path $env:LOCALAPPDATA 'Programs\Nocturnal'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
$backup = Join-Path $destination ('backups\' + (Get-Date -Format yyyyMMdd-HHmmss) + '-' + [Guid]::NewGuid().ToString('N').Substring(0,6))
New-Item -ItemType Directory -Force -Path $backup | Out-Null
foreach ($name in @('Nocturnal.exe','nocturnal.ico','nocturnal.png')) {
 $existing = Join-Path $destination $name
 if (Test-Path -LiteralPath $existing) { Copy-Item -LiteralPath $existing -Destination (Join-Path $backup $name) }
 Copy-Item -LiteralPath (Join-Path $dist $name) -Destination $existing -Force
}
$queue = Join-Path $env:LOCALAPPDATA 'Nocturnal\queue.json'
if (Test-Path -LiteralPath $queue) { Copy-Item -LiteralPath $queue -Destination (Join-Path $backup 'queue.json') }
$scheme = 'HKCU:\Software\Classes\nocturnal'
New-Item -Path "$scheme\shell\open\command" -Force | Out-Null
Set-Item -Path $scheme -Value 'URL:Nocturnal download'
New-ItemProperty -Path $scheme -Name 'URL Protocol' -Value '' -PropertyType String -Force | Out-Null
$executable = Join-Path $destination 'Nocturnal.exe'
Set-Item -Path "$scheme\shell\open\command" -Value ('"' + $executable + '" "%1"')
New-Item -Path "$scheme\DefaultIcon" -Force | Out-Null
Set-Item -Path "$scheme\DefaultIcon" -Value ('"' + (Join-Path $destination 'nocturnal.ico') + '",0')
$shell = New-Object -ComObject WScript.Shell
foreach ($folder in @([Environment]::GetFolderPath('Desktop'),[Environment]::GetFolderPath('Programs'))) {
 $shortcut = $shell.CreateShortcut((Join-Path $folder 'Nocturnal.lnk'))
 $shortcut.TargetPath = $executable
 $shortcut.WorkingDirectory = $destination
 $shortcut.IconLocation = (Join-Path $destination 'nocturnal.ico') + ',0'
 $shortcut.Save()
}
Write-Output "Installed to $destination. Media requires the optional tools described in README.md."
