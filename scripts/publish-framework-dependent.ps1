Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src/Ra3Trainer.App/Ra3Trainer.App.csproj"
$output = Join-Path $repoRoot "artifacts/publish/Ra3Trainer.App-win-x86-framework-dependent"

dotnet publish $project `
    -c Release `
    -r win-x86 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:PublishSelfContained=false `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $output
