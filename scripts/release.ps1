# Builds the installer and update packages into Releases/.
# Needs the Velopack CLI: dotnet tool install -g vpk
param([Parameter(Mandatory)][string]$Version)

$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '..'
$publish = Join-Path $root 'publish'

dotnet publish (Join-Path $root 'ResourceHunterBonusScout') -c Release -o $publish -p:Version=$Version
vpk pack --packId ResourceHunterBonusScout --packVersion $Version --packDir $publish --mainExe ResourceHunterBonusScout.exe `
    --packTitle 'Resource Hunter Bonus Scout' --icon (Join-Path $root 'ResourceHunterBonusScout/Assets/Active.ico') `
    --outputDir (Join-Path $root 'Releases')

# To publish: vpk upload github --repoUrl <repo> --token <token> --publish --releaseName $Version --tag v$Version
