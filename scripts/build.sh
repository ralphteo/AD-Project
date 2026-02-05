#!/usr/bin/env bash
set -e

dotnet restore ./ADWebApplication/ADWebApplication.csproj
dotnet build ./ADWebApplication/ADWebApplication.csproj --no-restore
