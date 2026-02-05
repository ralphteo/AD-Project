#!/usr/bin/env bash
set -e

# Start Sonar analysis
dotnet sonarscanner begin \
  /k:"GDipSA-Team-5_AD-Project" \
  /o:"gdipsa-team-5" \
  /d:sonar.host.url="https://sonarcloud.io" \
  /d:sonar.login="$SONAR_TOKEN" \
  /d:sonar.projectBaseDir="." \
  /d:sonar.cs.cobertura.reportsPaths="**/coverage.cobertura.xml" \
  /d:sonar.coverage.exclusions="**/Program.cs"

# Build (required)
dotnet build ./ADWebApplication/ADWebApplication.csproj

# Run tests WITH coverage (this is what you asked about)
dotnet test ./ADWebApplication/ADWebApplication.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

# End Sonar analysis (uploads results)
dotnet sonarscanner end \
  /d:sonar.login="$SONAR_TOKEN"
