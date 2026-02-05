#!/usr/bin/env bash
set -e

dependency-check.sh \
  --project ADWebApplication \
  --scan . \
  --format ALL \
  --out ./dependency-check-report \
  --failOnCVSS 7
