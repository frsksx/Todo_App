param(
    [string]$Filter = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$results = Join-Path $PSScriptRoot "_results"
New-Item -ItemType Directory -Force -Path $results | Out-Null

$args = @(
    "test",
    (Join-Path $repoRoot "Todo-App.sln"),
    "--logger", "trx;LogFilePrefix=test-results",
    "--logger", "console;verbosity=normal",
    "--results-directory", $results
)

if (-not [string]::IsNullOrWhiteSpace($Filter)) {
    $args += @("--filter", $Filter)
}

dotnet @args
