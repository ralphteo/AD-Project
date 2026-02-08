#!/usr/bin/env bash
set -e

# Start SonarCloud scan
dotnet sonarscanner begin \
  /k:"GDipSA-Team-5_AD-Project" \
  /o:"${SONAR_ORG}" \
  /d:sonar.host.url="https://sonarcloud.io" \
  /d:sonar.login="${SONAR_TOKEN}" \
  /d:sonar.sources="./ADWebApplication" \
  /d:sonar.tests="./ADWebApplication.Tests" \
  /d:sonar.javascript.lcov.reportPaths="./ADWebApplication.Tests/JavaScript/coverage/lcov.info" \
  /d:sonar.cs.cobertura.reportsPaths="./TestResults/CoverageReport/Cobertura.xml" \
  /d:sonar.coverage.exclusions="**/Program.cs,**/wwwroot/lib/**,**/*.min.js,**/wwwroot/js/collector-dashboard.js" \
  /d:sonar.exclusions="**/wwwroot/lib/**,**/*.min.js" \
  /d:sonar.test.inclusions="**/*.test.js"

# Build solution
dotnet build ./AD-Project.sln

# Run .NET tests and collect coverage
dotnet test ./ADWebApplication.Tests/ADWebApplication.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --settings coverlet.runsettings \
  --results-directory ./TestResults

# Convert coverage to Cobertura format for SonarCloud
dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.5.1
reportgenerator \
  -reports:./TestResults/**/coverage.cobertura.xml \
  -targetdir:./TestResults/CoverageReport \
  -reporttypes:Cobertura

# Run JavaScript tests with coverage if Node.js is available
if command -v node &> /dev/null; then
  echo "Running JavaScript tests with coverage..."
  cd ADWebApplication.Tests/JavaScript
  if [[ ! -d "node_modules" ]]; then
    npm install --ignore-scripts
  fi
  npm test -- --coverage
  cd ../..
else
  echo "Node.js not found. Skipping JavaScript tests."
fi

# End SonarCloud scan
dotnet sonarscanner end \
  /d:sonar.login="${SONAR_TOKEN}"
