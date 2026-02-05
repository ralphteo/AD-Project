#!/usr/bin/env bash
set -e

# Build main project
dotnet build ./ADWebApplication/ADWebApplication.csproj

# Run tests and collect coverage
dotnet test ./ADWebApplication.Tests/ADWebApplication.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

# Convert coverage to Cobertura format for SonarCloud
dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.5.1
reportgenerator \
  -reports:./TestResults/*/coverage.cobertura.xml \
  -targetdir:./TestResults/CoverageReport \
  -reporttypes:Cobertura

# Start SonarCloud scan
dotnet sonarscanner begin \
  /k:"GDipSA-Team-5_AD-Project" \
  /o:"${SONAR_ORG}" \
  /d:sonar.host.url="https://sonarcloud.io" \
  /d:sonar.login="${SONAR_TOKEN}" \
  /d:sonar.sources="./ADWebApplication" \
  /d:sonar.cs.cobertura.reportsPaths="./TestResults/CoverageReport/Cobertura.xml" \
  /d:sonar.coverage.exclusions="**/Program.cs"

# End SonarCloud scan
dotnet sonarscanner end \
  /d:sonar.login="${SONAR_TOKEN}"
