#!/usr/bin/env bash
set -e

dotnet test ./ADWebApplication.Tests/ADWebApplication.Tests.csproj \
  --collect:"XPlat Code Coverage"
