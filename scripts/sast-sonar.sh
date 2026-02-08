#!/usr/bin/env bash
set -e

# Start SonarCloud scan
echo "=== Starting SonarCloud Scan ==="
dotnet sonarscanner begin \
  /k:"GDipSA-Team-5_AD-Project" \
  /o:"gdipsa-team-5" \
  /d:sonar.host.url="https://sonarcloud.io" \
  /d:sonar.login="${SONAR_TOKEN}" \
  /d:sonar.projectBaseDir="$(pwd)" \
  /d:sonar.sources="ADWebApplication" \
  /d:sonar.tests="ADWebApplication.Tests" \
  /d:sonar.javascript.lcov.reportPaths="ADWebApplication.Tests/JavaScript/coverage/lcov.info" \
  /d:sonar.cs.opencover.reportsPaths="TestResults/**/coverage.opencover.xml" \
  /d:sonar.cs.vstest.reportsPaths="TestResults/**/*.trx" \
  /d:sonar.coverage.exclusions="**/Program.cs,**/Migrations/**,**/wwwroot/lib/**,**/*.min.js,**/wwwroot/js/collector-dashboard.js,**/Models/Entities/**,**/Models/DTOs/**" \
  /d:sonar.exclusions="**/wwwroot/lib/**,**/*.min.js" \
  /d:sonar.test.inclusions="**/*.test.js" \
  /d:sonar.verbose=true

# Restore main project
echo "=== Restoring Main Project ==="
dotnet restore ./ADWebApplication/ADWebApplication.csproj

# Build main project
echo "=== Building Main Project ==="
dotnet build ./ADWebApplication/ADWebApplication.csproj --no-restore

# Build test project
echo "=== Building Test Project ==="
dotnet build ./ADWebApplication.Tests/ADWebApplication.Tests.csproj

# Clean previous test results
echo "=== Cleaning Previous Test Results ==="
rm -rf ./TestResults

# Run .NET tests with coverage
echo "=== Running .NET Tests with Coverage ==="
dotnet test ./ADWebApplication.Tests/ADWebApplication.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --settings coverlet.runsettings \
  --results-directory ./TestResults \
  --logger "trx;LogFileName=test-results.trx"

# Debug: Show coverage files
echo "=== Coverage Files Found ==="
find TestResults -name "coverage.opencover.xml" -o -name "*.trx" || echo "No coverage files found"

# Run JavaScript tests with coverage
if command -v node &> /dev/null; then
  echo "=== Installing JavaScript Dependencies ==="
  cd ADWebApplication.Tests/JavaScript
  if [[ ! -d "node_modules" ]]; then
    npm install --ignore-scripts
  fi
  
  echo "=== Running JavaScript Tests with Coverage ==="
  npm test -- --coverage
  
  echo "=== JavaScript Coverage Files ==="
  find coverage -type f || echo "No JavaScript coverage files found"
  
  cd ../..
else
  echo "Node.js not found. Skipping JavaScript tests."
fi

# End SonarCloud scan
echo "=== Ending SonarCloud Scan ==="
dotnet sonarscanner end \
  /d:sonar.login="${SONAR_TOKEN}"

echo "=== SonarCloud Scan Complete ==="