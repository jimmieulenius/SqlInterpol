param(
    [string]$Target,
    [string]$ComposeFile = "docker-compose.e2e.yml",
    [string]$E2ETestsPath = "tests/e2e/SqlInterpol.E2E.Tests",
    [string]$OutputFile = $env:GITHUB_OUTPUT
)

$targetClean = ($Target ?? '').Trim()

# Resolve dialects: if target is empty, whitespace, or "all", query docker-compose for defined services
if ([string]::IsNullOrWhiteSpace($targetClean) -or $targetClean -ieq 'all') {
    $rawServices = & docker compose -f $ComposeFile config --services
    $dialects = @($rawServices | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() })
}
else {
    # Split comma-separated targets and trim whitespace
    $dialects = @($targetClean.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

# Scan E2E test sources once for [Trait("Category", "<Dialect>")]
$csFiles = @(Get-ChildItem -Path $E2ETestsPath -Filter "*.cs" -Recurse -ErrorAction SilentlyContinue)

$matrix = foreach ($dialect in $dialects) {
    $escaped = [regex]::Escape($dialect)
    $pattern = "\[Trait\(\s*`"Category`"\s*,\s*`"$escaped`"\s*\)\]"
    $hasTests = ($csFiles.Count -gt 0) -and [bool]($csFiles | Select-String -Pattern $pattern -Quiet)

    if (-not $hasTests) {
        Write-Host "::warning title=Missing E2E Tests::No E2E tests found with [Trait(`"Category`", `"$dialect`")]. Job will be skipped."
    }

    [PSCustomObject]@{
        dialect  = $dialect
        hasTests = $hasTests
    }
}

# Force JSON array even when $matrix has 0 or 1 items
$jsonArray = ConvertTo-Json -InputObject @($matrix) -Compress

if (-not [string]::IsNullOrWhiteSpace($OutputFile)) {
    "dialects=$jsonArray" | Out-File -FilePath $OutputFile -Encoding utf8 -Append
}

if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) {
    $summary = @(
        "### E2E Dialect Matrix"
        ""
        "| Dialect | Status |"
        "| :--- | :--- |"
    )
    foreach ($item in $matrix) {
        $status = if ($item.hasTests) { "Queued" } else { "Skipped (no E2E tests found)" }
        $summary += "| **$($item.dialect)** | $status |"
    }
    $summary | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Encoding utf8 -Append
}

Write-Host "Resolved E2E Test Matrix: $jsonArray"