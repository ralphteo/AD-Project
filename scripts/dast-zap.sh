#!/usr/bin/env bash
set -e

docker run --network=host \
  -v "$(pwd)":/zap/wrk \
  zaproxy/zap-baseline \
  zap-baseline.py \
  -t http://localhost:5000 \
  -r zap-report.html
