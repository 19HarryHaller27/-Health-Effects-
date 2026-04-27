# Build HealthEffects; MSBuild may deploy to
# %AppData%\Roaming\VintagestoryData\Mods\HealthEffects
# Usage: .\build.ps1
#   .\build.ps1 -PruneSrcFromDeploy   # optional notes only
# Optional: $env:VINTAGESTORY = path to game folder (contains VintagestoryAPI.dll).
# If unset, use Directory.Build.props in this directory (set VintageStoryPath there).
param(
    [switch] $PruneSrcFromDeploy
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$args = @("build", ".\HealthEffects.csproj", "-c", "Release")
if ($env:VINTAGESTORY) {
  $api = Join-Path $env:VINTAGESTORY.TrimEnd('\') "VintagestoryAPI.dll"
  if (-not (Test-Path -LiteralPath $api)) {
    throw "VINTAGESTORY does not contain VintagestoryAPI.dll: $api"
  }
  $args += @("-p:VintageStoryPath=$($env:VINTAGESTORY.TrimEnd('\'))")
}

& dotnet @args
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Done. If deploy ran, the mod is also in %AppData%\Roaming\VintagestoryData\Mods\HealthEffects\"
Write-Host "For a clean hand-install, copy the contents of .\out\ForMods\  (HealthEffects.dll + modinfo only)."
if ($PruneSrcFromDeploy) { Write-Host "PruneSrcFromDeploy: see README; avoid copying dev \src\ into Mods for production." }
Write-Host "Tip: or remove / avoid copying the dev \src\ folder into Mods so the game loads the prebuilt assembly only."
