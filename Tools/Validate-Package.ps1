param(
    [string]$RegistryRoot = $env:DEUCARIAN_PACKAGE_REGISTRY_ROOT,
    [string]$Config = "deucarian-package.json"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$candidates = @()
if ($RegistryRoot) {
    $candidates += $RegistryRoot
}
$candidates += @(
    (Join-Path $root "..\Package-Registry"),
    (Join-Path $root "..\..\Package-Registry"),
    "C:\Repositories\Package-Registry"
)

$resolvedRegistryRoot = $null
foreach ($candidate in $candidates) {
    if (-not $candidate) {
        continue
    }

    $validator = Join-Path $candidate "Tools\deucarian_package_validator.py"
    if (Test-Path -LiteralPath $validator -PathType Leaf) {
        $resolvedRegistryRoot = (Resolve-Path -LiteralPath $candidate).Path
        break
    }
}

if (-not $resolvedRegistryRoot) {
    throw "Could not find Package Registry shared validator. Set DEUCARIAN_PACKAGE_REGISTRY_ROOT."
}

$validatorPath = Join-Path $resolvedRegistryRoot "Tools\deucarian_package_validator.py"
$configPath = Join-Path $root $Config
python $validatorPath --registry-root $resolvedRegistryRoot --repository-root $root --config $configPath
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
