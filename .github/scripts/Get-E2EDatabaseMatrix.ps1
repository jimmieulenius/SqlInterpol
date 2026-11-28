param(
    [string]$Target,
    [string]$ComposeFile = "docker-compose.e2e.yml",
    [string]$E2ETestsProject = "tests/e2e/SqlInterpol.E2E.Tests/SqlInterpol.E2E.Tests.csproj",
    [string]$OutputFile = $env:GITHUB_OUTPUT
)

$targetClean = ($Target ?? '').Trim()

if ([string]::IsNullOrWhiteSpace($targetClean) -or $targetClean -ieq 'all') {
    $rawServices = & docker compose -f $ComposeFile config --services
    $dialects = @($rawServices | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() })
}
else {
    $dialects = @($targetClean.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

# Build E2E test project once so xUnit can list actual discovered test cases per dialect
$env:RUN_E2E = 'true'
Write-Host "Building $E2ETestsProject for test discovery..."
& dotnet build $E2ETestsProject -c Debug --nologo -v quiet
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to build $E2ETestsProject during matrix discovery."
    exit 1
}

$matrix = foreach ($dialect in $dialects) {
    $listOutput = & dotnet test $E2ETestsProject -c Debug --no-build --nologo --list-tests --filter "Category=$dialect" 2>&1
    $outputText = $listOutput -join "`n"

    # When tests exist, VSTest prints "The following Tests are available:" followed by indented test names,
    # and does NOT print "No test matches the given testcase filter".
    $hasTests = ($outputText -match "The following Tests are available:") -and
                ($outputText -notmatch "No test matches the given testcase filter")

    if (-not $hasTests) {
        Write-Host "::warning title=Missing E2E Tests::No runnable E2E tests found for Category=$dialect. Job will be skipped."
    }

    [PSCustomObject]@{
        dialect  = $dialect
        hasTests = [bool]$hasTests
    }
}

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