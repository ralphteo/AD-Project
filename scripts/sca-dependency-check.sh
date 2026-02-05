#!/usr/bin/env bash
set -e

# Directory for cached data (NVD, CISA, RetireJS)
DATA_DIR="./dependency-check-data"
mkdir -p "$DATA_DIR"

# Check if the DB exists
if [ -z "$(ls -A "$DATA_DIR")" ]; then
  echo "No cached DB found, performing initial update..."
  /usr/local/dependency-check/bin/dependency-check.sh \
    --project ADWebApplication \
    --scan . \
    --format ALL \
    --out ./dependency-check-report \
    --data "$DATA_DIR" \
    --nvdApiKey "${NVD_API_KEY}" \
    --failOnCVSS 9
else
  echo "Using cached DB..."
  /usr/local/dependency-check/bin/dependency-check.sh \
    --project ADWebApplication \
    --scan . \
    --format ALL \
    --out ./dependency-check-report \
    --data "$DATA_DIR" \
    -n \
    --nvdApiKey "${NVD_API_KEY}" \
    --failOnCVSS 9
fi
