# CitizenSoul -- one-shot release helper for a Windows machine with Cities: Skylines II.
# Builds Release, stages dist\CitizenSoul, and launches the Paradox Mod Publisher.
#
#   powershell -ExecutionPolicy Bypass -File Tools\Publish.ps1
#   powershell -ExecutionPolicy Bypass -File Tools\Publish.ps1 -Deploy           # also copy into local Mods to play now
#   powershell -ExecutionPolicy Bypass -File Tools\Publish.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Cities Skylines II"
param(
    [string]$GameDir = "",
    [string]$Configuration = "Release",
    [switch]$SkipBuild,
    [switch]$Deploy
)
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo
Write-Host "repo: $repo"

# -- 1. find the game -------------------------------------------------------
function Test-GameDir([string]$p) { return (Test-Path (Join-Path $p "Cities2_Data\Managed\Game.dll")) }
if (-not $GameDir) {
    $candidates = @(
        "C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II",
        "C:\Program Files\Steam\steamapps\common\Cities Skylines II",
        "D:\Steam\steamapps\common\Cities Skylines II",
        "D:\SteamLibrary\steamapps\common\Cities Skylines II",
        "E:\SteamLibrary\steamapps\common\Cities Skylines II",
        "C:\XboxGames\Cities Skylines II"
    )
    foreach ($c in $candidates) { if (Test-GameDir $c) { $GameDir = $c; break } }
}
if (-not $GameDir -or -not (Test-GameDir $GameDir)) {
    throw "Game not found. Re-run with -GameDir 'X:\path\to\Cities Skylines II' (the folder containing Cities2_Data)."
}
Write-Host "game: $GameDir"

# -- 2. build ---------------------------------------------------------------
if (-not $SkipBuild) {
    dotnet build CitizenSoul.csproj -c $Configuration -p:GameDir="$GameDir"
    if ($LASTEXITCODE -ne 0) { throw "build failed" }
}
$built = Get-ChildItem -Path (Join-Path $repo "bin\$Configuration") -Recurse -Filter CitizenSoul.dll -ErrorAction SilentlyContinue |
         Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $built) { throw "CitizenSoul.dll not found under bin\$Configuration" }
Write-Host "built: $($built.FullName)"

# -- 3. stage ---------------------------------------------------------------
$dist  = Join-Path $repo "dist"
$stage = Join-Path $dist "CitizenSoul"
Remove-Item $dist -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Copy-Item $built.FullName $stage
Copy-Item (Join-Path $repo "lang") (Join-Path $stage "lang") -Recurse
Copy-Item (Join-Path $repo "Properties\PublishConfiguration.xml") $dist
Copy-Item (Join-Path $repo "Properties\Thumbnail.png") $dist
Write-Host "staged: $stage"

# -- 4. optional deploy for local play --------------------------------------
if ($Deploy) {
    $mods = Join-Path $env:USERPROFILE "AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\CitizenSoul"
    New-Item -ItemType Directory -Path $mods -Force | Out-Null
    Copy-Item (Join-Path $stage "*") $mods -Recurse -Force
    Write-Host "deployed: $mods  (start the game and enable the mod)"
}

# -- 5. publish via the CS2 Modding Toolchain -------------------------------
$mp = @()
foreach ($root in @("$env:LOCALAPPDATA\Programs", "$env:LOCALAPPDATA", "C:\Program Files", "C:\Program Files (x86)")) {
    if (Test-Path $root) {
        $mp += Get-ChildItem -Path $root -Recurse -Filter "ModPublisher.exe" -ErrorAction SilentlyContinue -Depth 7
    }
}
$mp = $mp | Select-Object -First 1
if ($mp) {
    Write-Host "launching ModPublisher: $($mp.FullName)"
    Write-Host "In ModPublisher: open CitizenSoul, point it at $stage, then Publish."
    Start-Process $mp.FullName
} else {
    Write-Host "ModPublisher.exe not found automatically."
    Write-Host "Open your CS2 Modding Toolchain and run its ModPublisher against: $stage"
}
Write-Host "done."
