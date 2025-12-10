# build.ps1 - Automated Build and Test Script
Write-Host "========================================"
Write-Host "DDD System - Automated Build Pipeline" -ForegroundColor Cyan
Write-Host "========================================"
Write-Host ""

# Get current date/time for logging
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
Write-Host "Build started: $timestamp" -ForegroundColor Green
Write-Host ""

# 1. Clean previous builds
Write-Host "1. Cleaning solution..." -ForegroundColor Yellow
dotnet clean
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Clean failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Clean completed" -ForegroundColor Green
Write-Host ""

# 2. Restore NuGet packages
Write-Host "2. Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Restore failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Restore completed" -ForegroundColor Green
Write-Host ""

# 3. Build solution
Write-Host "3. Building solution..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build completed" -ForegroundColor Green
Write-Host ""

# 4. Run tests
Write-Host "4. Running unit tests..." -ForegroundColor Yellow
$testOutput = dotnet test --no-build --verbosity normal --logger "trx;LogFileName=TestResults.trx"
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Tests failed" -ForegroundColor Red
    
    # Parse test results
    if (Test-Path "TestResults.trx") {
        $testResults = [xml](Get-Content "TestResults.trx")
        $failedTests = $testResults.TestRun.Results.UnitTestResult | Where-Object { $_.outcome -eq "Failed" }
        Write-Host ""
        Write-Host "Failed tests:" -ForegroundColor Red
        foreach ($test in $failedTests) {
            Write-Host "  - $($test.testName)" -ForegroundColor Red
        }
    }
    exit 1
}

Write-Host "✅ All tests passed!" -ForegroundColor Green
Write-Host ""

# 5. Generate test report
Write-Host "5. Generating test report..." -ForegroundColor Yellow
if (Test-Path "TestResults.trx") {
    $testResults = [xml](Get-Content "TestResults.trx")
    $totalTests = $testResults.TestRun.Results.UnitTestResult.Count
    $passedTests = ($testResults.TestRun.Results.UnitTestResult | Where-Object { $_.outcome -eq "Passed" }).Count
    $failedTests = ($testResults.TestRun.Results.UnitTestResult | Where-Object { $_.outcome -eq "Failed" }).Count
    
    Write-Host ""
    Write-Host "========================================"
    Write-Host "TEST REPORT SUMMARY" -ForegroundColor Cyan
    Write-Host "========================================"
    Write-Host "Total tests: $totalTests"
    Write-Host "Passed: $passedTests" -ForegroundColor Green
    Write-Host "Failed: $failedTests" -ForegroundColor $(if ($failedTests -gt 0) { "Red" } else { "Green" })
    Write-Host "Success rate: $([math]::Round(($passedTests/$totalTests)*100, 1))%"
    Write-Host "========================================"
    Write-Host ""
}

# 6. Final summary
$endTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
Write-Host "========================================"
Write-Host "BUILD COMPLETED SUCCESSFULLY" -ForegroundColor Green
Write-Host "Completed at: $endTime" -ForegroundColor Green
Write-Host "========================================"

exit 0
