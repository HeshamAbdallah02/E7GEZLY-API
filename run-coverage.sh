#!/bin/bash
# run-coverage.sh

echo -e "\033[32mE7GEZLY API - Code Coverage Report\033[0m"
echo -e "\033[32m==================================\033[0m"

# Clean previous results
echo -e "\n\033[33mCleaning previous test results...\033[0m"
rm -rf ./TestResults
rm -rf ./E7GEZLY_API.Tests/TestResults

# Run tests with coverage
echo -e "\n\033[33mRunning tests with code coverage...\033[0m"
dotnet test ./E7GEZLY_API.Tests/E7GEZLY_API.Tests.csproj \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=cobertura \
    /p:CoverletOutput=./TestResults/ \
    /p:Exclude="[*]*.Migrations.*,[*]*.Tests.*,[*]Program.*,[*]*Extensions.*" \
    /p:ExcludeByAttribute="GeneratedCodeAttribute,ExcludeFromCodeCoverageAttribute"

# Check if coverage file was generated
if [ -f "./E7GEZLY_API.Tests/TestResults/coverage.cobertura.xml" ]; then
    echo -e "\n\033[33mGenerating HTML coverage report...\033[0m"
    
    # Generate HTML report
    reportgenerator \
        -reports:"./E7GEZLY_API.Tests/TestResults/coverage.cobertura.xml" \
        -targetdir:"./TestResults/CoverageReport" \
        -reporttypes:Html \
        -title:"E7GEZLY API Coverage Report" \
        -assemblyfilters:"+E7GEZLY_API;-E7GEZLY_API.Tests"
    
    echo -e "\n\033[32mCoverage report generated successfully!\033[0m"
    echo -e "\033[33mOpening coverage report...\033[0m"
    
    # Open report based on OS
    if [[ "$OSTYPE" == "darwin"* ]]; then
        open ./TestResults/CoverageReport/index.html
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        xdg-open ./TestResults/CoverageReport/index.html
    fi
else
    echo -e "\n\033[31mError: Coverage file not found!\033[0m"
fi