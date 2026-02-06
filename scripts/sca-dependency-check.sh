#!/usr/bin/env bash
set -e

DATA_DIR="./dependency-check-data"
mkdir -p "$DATA_DIR"

dependency-check.sh \
  --project ADWebApplication \
  --scan . \
  --format ALL \
  --out ./dependency-check-report \
  --data "$DATA_DIR" \
  --nvdApiKey "${NVD_API_KEY}" \
  --failOnCVSS 9