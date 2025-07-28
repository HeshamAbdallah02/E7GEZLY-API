cd "D:\E7GEZLY\E7GEZLY API"

# Clean
Remove-Item -Path "./TestResults" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "./E7GEZLY_API.Tests/TestResults" -Recurse -Force -ErrorAction SilentlyContinue

# Run only unit tests with coverage
dotnet test ./E7GEZLY_API.Tests/E7GEZLY_API.Tests.csproj `
    --filter "TestCategory=Unit" `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=cobertura `
    /p:CoverletOutput=./TestResults/

# Generate report
reportgenerator `
    -reports:"./E7GEZLY_API.Tests/TestResults/coverage.cobertura.xml" `
    -targetdir:"./TestResults/CoverageReport" `
    -reporttypes:Html

# Open report
Start-Process "./TestResults/CoverageReport/index.html"