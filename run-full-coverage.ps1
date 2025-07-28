Write-Host "E7GEZLY API - Full Coverage Report" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green

# Clean
Write-Host "`nCleaning previous results..." -ForegroundColor Yellow
Remove-Item -Path "./TestResults" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "./E7GEZLY_API.Tests/TestResults" -Recurse -Force -ErrorAction SilentlyContinue

# Build
Write-Host "`nBuilding projects..." -ForegroundColor Yellow
dotnet build --configuration Debug
cd
# Copy integration test files
Write-Host "`nCopying integration test files..." -ForegroundColor Yellow
Copy-Item "E7GEZLY API\bin\Debug\net8.0\E7GEZLY API.deps.json" -Destination "E7GEZLY_API.Tests\bin\Debug\net8.0\testhost.deps.json" -Force
Copy-Item "E7GEZLY API\bin\Debug\net8.0\E7GEZLY API.runtimeconfig.json" -Destination "E7GEZLY_API.Tests\bin\Debug\net8.0\testhost.runtimeconfig.json" -Force

# Run all tests with coverage
Write-Host "`nRunning all tests with coverage..." -ForegroundColor Yellow
dotnet test ./E7GEZLY_API.Tests/E7GEZLY_API.Tests.csproj `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=cobertura `
    /p:CoverletOutput=./TestResults/ `
    /p:Exclude="[*]*.Migrations.*%2c[*]*.Tests.*%2c[*]Program.*%2c[*]*Extensions.*"

# Generate report
Write-Host "`nGenerating coverage report..." -ForegroundColor Yellow
reportgenerator `
    -reports:"./E7GEZLY_API.Tests/TestResults/coverage.cobertura.xml" `
    -targetdir:"./TestResults/CoverageReport" `
    -reporttypes:"Html;Badges;TextSummary" `
    -title:"E7GEZLY API Coverage Report"

# Display summary
Write-Host "`nCoverage Summary:" -ForegroundColor Green
Get-Content "./TestResults/CoverageReport/Summary.txt" -ErrorAction SilentlyContinue

# Open report
Write-Host "`nOpening coverage report..." -ForegroundColor Yellow
Start-Process "./TestResults/CoverageReport/index.html"