# Builds the installer and update packages into Releases/, and uploads them to GitHub Releases when given a token.
param(
    # Defaults to the Version in the csproj.
    [string]$Version,
    # A SemVer prerelease label, like ci.42. Makes it a GitHub prerelease, which installed copies don't update to.
    [string]$Prerelease,
    [string]$GitHubToken)

$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '..'
$project = Join-Path $root 'ResourceHunterBonusScout'
$publish = Join-Path $root 'publish'
$releases = Join-Path $root 'Releases'

if (-not $Version) { $Version = dotnet msbuild $project -getProperty:Version }
if ($Prerelease) { $Version = "$Version-$Prerelease" }

dotnet tool restore
dotnet publish $project -c Release -o $publish -p:Version=$Version
dotnet vpk pack --packId ResourceHunterBonusScout --packVersion $Version --packDir $publish --mainExe ResourceHunterBonusScout.exe `
    --packTitle 'Resource Hunter Bonus Scout' --icon (Join-Path $project 'Assets/Active.ico') `
    --outputDir $releases

if ($GitHubToken) {
    $pre = if ($Prerelease) { '--pre' } else { $null }
    dotnet vpk upload github --repoUrl https://github.com/JayBazuzi/ResourceHunterBonusScout --token $GitHubToken `
        --outputDir $releases --publish $pre --releaseName $Version --tag "v$Version" --targetCommitish (git rev-parse HEAD)
}
