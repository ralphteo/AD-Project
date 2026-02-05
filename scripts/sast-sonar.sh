#!/usr/bin/env bash
set -e

# Start SonarCloud scan
dotnet sonarscanner begin \
  /k:"GDipSA-Team-5_AD-Project" \
  /o:"${SONAR_ORG}" \
  /d:sonar.host.url="https://sonarcloud.io" \
  /d:sonar.login="${SONAR_TOKEN}" \
  /d:sonar.projectBaseDir="." \
  /d:sonar.cs.opencover.reportsPaths="./TestResults/**/coverage.opencover.xml" \
  /d:sonar.coverage.exclusions="**/Program.cs"

# Build main project
dotnet build ./ADWebApplication/ADWebApplication.csproj

# Run tests and collect coverage
dotnet test ./ADWebApplication.Tests/ADWebApplication.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

# Convert coverage to a (cobertura) format for SonarCloud
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:./TestResults/*/coverage.cobertura.xml \
  -targetdir:./TestResults/CoverageReport \
  -reporttypes:Cobertura

# End SonarCloud scan
dotnet sonarscanner end \
  /d:sonar.login="${SONAR_TOKEN}"
