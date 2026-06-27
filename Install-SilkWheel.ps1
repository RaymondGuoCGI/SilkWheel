param(
    [switch]$NoStart
)

$ErrorActionPreference = "Stop"

$source = Join-Path $PSScriptRoot "bin\Release\net8.0-windows\win-x64\publish\SilkWheel.exe"
if (-not (Test-Path $source)) {
    throw "SilkWheel.exe was not found. Run dotnet publish first."
}

$installDir = Join-Path $env:LOCALAPPDATA "Programs\SilkWheel"
$target = Join-Path $installDir "SilkWheel.exe"
$startMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\SilkWheel"
$shortcut = Join-Path $startMenuDir "SilkWheel.lnk"

Get-Process SilkWheel -ErrorAction SilentlyContinue | Stop-Process -Force
New-Item -ItemType Directory -Force -Path $installDir, $startMenuDir | Out-Null
Copy-Item -Force $source $target

$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut($shortcut)
$link.TargetPath = $target
$link.WorkingDirectory = $installDir
$link.IconLocation = "$target,0"
$link.Description = "SilkWheel smooth mouse wheel scrolling"
$link.Save()

if (-not $NoStart) {
    Start-Process $target
}

Write-Host "SilkWheel installed to: $target"
Write-Host "Start Menu shortcut: $shortcut"
