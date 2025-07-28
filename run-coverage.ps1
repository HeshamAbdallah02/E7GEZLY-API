# run-coverage.ps1
Write-Host "E7GEZLY API - Code Coverage Report" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Green

# Clean previous results
Write-Host "`nCleaning previous test results..." -ForegroundColor Yellow
if (Test-Path "./TestResults") {
    Remove-Item -Path "./TestResults" -Recurse -Force
}
if (Test-Path "./E7GEZLY_API.Tests/TestResults") {
    Remove-Item -Path "./E7GEZLY_API.Tests/TestResults" -Recurse -Force
}

# Run tests with coverage
Write-Host "`nRunning tests with code coverage..." -ForegroundColor Yellow
dotnet test ./E7GEZLY_API.Tests/E7GEZLY_API.Tests.csproj `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=cobertura `
    /p:CoverletOutput=./TestResults/ `
    /p:Exclude="[*]*.Migrations.*%2c[*]*.Tests.*%2c[*]Program.*%2c[*]*Extensions.*" `
    /p:ExcludeByAttribute="GeneratedCodeAttribute%2cExcludeFromCodeCoverageAttribute"

# Check if coverage file was generated
if (Test-Path "./E7GEZLY_API.Tests/TestResults/coverage.cobertura.xml") {
    Write-Host "`nGenerating HTML coverage report..." -ForegroundColor Yellow
    
    # Generate HTML report
    reportgenerator `
        -reports:"./E7GEZLY_API.Tests/TestResults/coverage.cobertura.xml" `
        -targetdir:"./TestResults/CoverageReport" `
        -reporttypes:Html `
        -title:"E7GEZLY API Coverage Report" `
        -assemblyfilters:"+E7GEZLY_API;-E7GEZLY_API.Tests"
    
    Write-Host "`nCoverage report generated successfully!" -ForegroundColor Green
    Write-Host "Opening coverage report..." -ForegroundColor Yellow
    Start-Process "./TestResults/CoverageReport/index.html"
} else {
    Write-Host "`nError: Coverage file not found!" -ForegroundColor Red
}