#!/usr/bin/env bash
set -e

# Directory for cached data (NVD, CISA, RetireJS)
DATA_DIR="./dependency-check-data"
mkdir -p "$DATA_DIR"

# Determine if database already exists
if [ -d "$DATA_DIR/nvd" ]; then
    echo "Using cached Dependency-Check database"
    UPDATE_FLAG="-n"  # don't auto-update
else
    echo "No cached database found, allowing update"
    UPDATE_FLAG=""    # allow update for first run
fi

# Run Dependency-Check
dependency-check.sh \
  --project ADWebApplication \
  --scan . \
  --format ALL \
  --out ./dependency-check-report \
  --data "$DATA_DIR" \
  --nvdApiKey "${NVD_API_KEY}" \
  --failOnCVSS 9 \
  $UPDATE_FLAG
