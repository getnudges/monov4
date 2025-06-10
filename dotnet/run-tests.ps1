$reportDir = "./test-reports"
$solutionFile = "$reportDir/AllTests.sln"

# Ensure test-reports directory exists & clean old reports
Write-Host "📂 Setting up test-reports directory..."
if (Test-Path $reportDir) {
    Get-ChildItem -Path $reportDir -Exclude ".gitignore" | Remove-Item -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $reportDir | Out-Null
}

# Create a new test-only solution
Write-Host "📝 Creating new test solution..."
dotnet new sln -n AllTests -o $reportDir

# Find all test projects and add them to the solution
Write-Host "🔍 Finding test projects..."
$testProjects = Get-ChildItem -Path . -Recurse -Filter "*.Tests.Unit.csproj"

foreach ($proj in $testProjects) {
    Write-Host "➕ Adding $($proj.FullName) to solution..."
    dotnet sln $solutionFile add $proj.FullName
}

# Run tests with code coverage
Write-Host "🚀 Running tests with code coverage..."
dotnet test $solutionFile `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=opencover `
    /p:CoverletOutput=$reportDir/coverage.xml

# Generate a single unified HTML report
Write-Host "📊 Generating combined test coverage report..."
reportgenerator `
    -reports:$reportDir/coverage.xml `
    -targetdir:$reportDir/report `
    -reporttypes:Html

Write-Host "✅ All tests completed! Open the report: $reportDir/report/index.html"
