#!/usr/bin/env bash
set -e

dotnet sonarscanner begin \
  /k:"GDipSA-Team-5_AD-Project" \
  /o:"gdipsa-team-5" \
  /d:sonar.host.url="https://sonarcloud.io" \
  /d:sonar.cs.cobertura.reportsPaths="**/coverage.cobertura.xml" \
  /d:sonar.coverage.exclusions="**/Program.cs"

dotnet build ./ADWebApplication/ADWebApplication.csproj

dotnet sonarscanner end
