#!/usr/bin/env bash
set -e

dependency-check \
  --project ADWebApplication \
  --scan . \
  --format ALL \
  --out ./dependency-check-report \
  --failOnCVSS 7 \
  -n # Tells Dependency-Check not to try updating the NVD database. No NVD API key.