#!/bin/sh
set -eux

echo ">> creating databases if missing"
createdb -U "" AuthService   2>/dev/null || true
createdb -U "" UserProgress  2>/dev/null || true
createdb -U "" PatternsMinIO 2>/dev/null || true

echo ">> applying schema to UserProgress"
psql -v ON_ERROR_STOP=1 -U "" -d UserProgress \
     -f /docker-entrypoint-initdb.d/20-schema.psql

echo ">> done"