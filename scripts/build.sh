#!/usr/bin/env bash
set -e

# Build .NET application
dotnet restore ./ADWebApplication/ADWebApplication.csproj
dotnet build ./ADWebApplication/ADWebApplication.csproj --no-restore

# Run JavaScript tests if Node.js is available
if command -v node &> /dev/null; then
  echo "Running JavaScript tests..."
  cd ADWebApplication.Tests/JavaScript
  if [[ ! -d "node_modules" ]]; then
    npm install --ignore-scripts
  fi
  npm test -- --coverage
  cd ../..
else
  echo "Node.js not found. Skipping JavaScript tests."
fi
