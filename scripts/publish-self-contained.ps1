Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src/Ra3Trainer.App/Ra3Trainer.App.csproj"
$output = Join-Path $repoRoot "artifacts/publish/Ra3Trainer.App-win-x86-self-contained"

dotnet publish $project `
    -c Release `
    -r win-x86 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishSelfContained=true `
    -p:PublishTrimmed=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $output
