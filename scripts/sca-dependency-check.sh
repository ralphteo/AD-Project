#!/usr/bin/env bash
set -e

# Directory for cached data (NVD, CISA, RetireJS)
DATA_DIR="./dependency-check-data"
mkdir -p "$DATA_DIR"

# Run Dependency-Check with API key and local cache
dependency-check \
  --project ADWebApplication \
  --scan . \
  --format ALL \
  --out ./dependency-check-report \
  --data "$DATA_DIR" \
  --nvdApiKey "${NVD_API_KEY}" \
  --failOnCVSS 7
