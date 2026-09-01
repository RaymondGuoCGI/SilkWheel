param(
    [string]$Configuration = "Release",
    [ValidateSet("win-x64")]
    [string]$Runtime = "win-x64",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$projectPath = Join-Path $repoRoot "SilkWheel.csproj"
$installerScript = Join-Path $repoRoot "installer\SilkWheel.iss"
$artifactRoot = Join-Path $repoRoot "artifacts"
$publishDirectory = Join-Path $artifactRoot "publish\$Runtime"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $artifactRoot "installer"
}

$resolvedArtifactRoot = [System.IO.Path]::GetFullPath($artifactRoot)
$resolvedPublishDirectory = [System.IO.Path]::GetFullPath($publishDirectory)
$artifactBoundary = $resolvedArtifactRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
if (-not $resolvedPublishDirectory.StartsWith($artifactBoundary, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Publish directory must stay inside the repository artifact directory."
}

[xml]$project = Get-Content -Raw $projectPath
$properties = $project.Project.PropertyGroup | Select-Object -First 1
$appVersion = [string]$properties.Version
$fileVersion = [string]$properties.FileVersion

if ([string]::IsNullOrWhiteSpace($appVersion) -or [string]::IsNullOrWhiteSpace($fileVersion)) {
    throw "Version and FileVersion must be set in SilkWheel.csproj."
}

$innoCandidates = @(
    (Get-Command "ISCC.exe" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -First 1),
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

$innoCompiler = $innoCandidates | Select-Object -First 1
if (-not $innoCompiler) {
    throw "Inno Setup 6 compiler was not found. Install JRSoftware.InnoSetup first."
}

New-Item -ItemType Directory -Force -Path $publishDirectory, $OutputDirectory | Out-Null

dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $publishDirectory `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:DebugType=None `
    /p:DebugSymbols=false `
    --nologo

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$publishedExe = Join-Path $publishDirectory "SilkWheel.exe"
if (-not (Test-Path -LiteralPath $publishedExe)) {
    throw "Published executable was not found at $publishedExe."
}

& $innoCompiler `
    "/DMyAppVersion=$appVersion" `
    "/DMyFileVersion=$fileVersion" `
    "/DPublishDir=$publishDirectory" `
    "/DOutputDir=$OutputDirectory" `
    $installerScript

if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed with exit code $LASTEXITCODE."
}

$installerPath = Join-Path $OutputDirectory "SilkWheel-Setup-$appVersion-win-x64.exe"
if (-not (Test-Path -LiteralPath $installerPath)) {
    throw "Installer was not found at $installerPath."
}

$installer = Get-Item -LiteralPath $installerPath
$hash = Get-FileHash -LiteralPath $installerPath -Algorithm SHA256

Write-Host "Installer: $($installer.FullName)"
Write-Host "Size: $($installer.Length) bytes"
Write-Host "SHA256: $($hash.Hash)"
