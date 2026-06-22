$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$requiredFiles = @(
    "package.json",
    "README.md",
    "CHANGELOG.md",
    "LICENSE.md",
    "Runtime/Deucarian.UIFlow.asmdef",
    "Runtime.UGUI/Deucarian.UIFlow.UGUI.asmdef",
    "Editor/Deucarian.UIFlow.Editor.asmdef",
    "Tests/EditMode/Deucarian.UIFlow.Tests.EditMode.asmdef",
    "Tests/PlayMode/Deucarian.UIFlow.Tests.PlayMode.asmdef",
    "Samples~/BasicFlow/Deucarian.UIFlow.Samples.BasicFlow.asmdef"
)

$requiredDirectories = @(
    "Runtime",
    "Runtime.UGUI",
    "Editor",
    "Tests/EditMode",
    "Tests/PlayMode",
    "Samples~/BasicFlow",
    "Documentation~",
    "Tools"
)

foreach ($directory in $requiredDirectories) {
    $path = Join-Path $root $directory
    if (-not (Test-Path -LiteralPath $path -PathType Container)) {
        throw "Missing required directory: $directory"
    }
}

foreach ($file in $requiredFiles) {
    $path = Join-Path $root $file
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing required file: $file"
    }
}

$package = Get-Content -LiteralPath (Join-Path $root "package.json") -Raw | ConvertFrom-Json
if ($package.name -ne "com.deucarian.ui-flow") {
    throw "Unexpected package name: $($package.name)"
}

if ($package.version -notmatch "^\d+\.\d+\.\d+$") {
    throw "Package version must be semver MAJOR.MINOR.PATCH: $($package.version)"
}

if ($package.dependencies."com.unity.ugui" -ne "1.0.0") {
    throw "Expected dependency com.unity.ugui version 1.0.0"
}

if ($package.dependencies."com.deucarian.logging" -ne "1.0.1") {
    throw "Expected dependency com.deucarian.logging version 1.0.1"
}

$runtimeAsmdef = Get-Content -LiteralPath (Join-Path $root "Runtime/Deucarian.UIFlow.asmdef") -Raw | ConvertFrom-Json
if ($runtimeAsmdef.name -ne "Deucarian.UIFlow") {
    throw "Unexpected runtime asmdef name: $($runtimeAsmdef.name)"
}

if ($runtimeAsmdef.references.Count -ne 1 -or $runtimeAsmdef.references -notcontains "Deucarian.Logging") {
    throw "Runtime assembly must reference only Deucarian.Logging."
}

$uguiAsmdef = Get-Content -LiteralPath (Join-Path $root "Runtime.UGUI/Deucarian.UIFlow.UGUI.asmdef") -Raw | ConvertFrom-Json
if ($uguiAsmdef.references -notcontains "Unity.ugui") {
    throw "UGUI assembly must reference Unity.ugui"
}

$forbiddenDependencies = @(
    "com.deucarian.core-state",
    "com.deucarian.api",
    "com.deucarian.session",
    "com.deucarian.ui-binding",
    "com.deucarian.object-selection"
)

foreach ($dependency in $forbiddenDependencies) {
    if ($package.dependencies.PSObject.Properties.Name -contains $dependency) {
        throw "UI Flow must not depend on internal package $dependency"
    }
}

$forbiddenProjectScaffolding = @("Assets", "Packages", "ProjectSettings")
foreach ($directory in $forbiddenProjectScaffolding) {
    $path = Join-Path $root $directory
    if (Test-Path -LiteralPath $path -PathType Container) {
        throw "Package repository should not contain Unity project scaffolding directory: $directory"
    }
}

$generatedArtifacts = Get-ChildItem -LiteralPath $root -Recurse -Force -File |
    Where-Object { $_.Name -match "\.(unitypackage|zip|tar|tgz)$" }
if ($generatedArtifacts.Count -gt 0) {
    throw "Generated artifacts are present in the package repository."
}

Write-Host "Deucarian UI Flow package validation passed."
