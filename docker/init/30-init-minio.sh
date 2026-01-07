#!/bin/sh

set -e

: "${MINIO_ENDPOINT:=http://minio:9000}"
: "${MINIO_ROOT_USER:=minioadmin}"
: "${MINIO_ROOT_PASSWORD:=minioadmin}"
: "${MINIO_ALIAS:=local}"
: "${MINIO_BUCKET:=my-bucket}"
: "${MINIO_BUCKETS:=$MINIO_BUCKET}"
: "${MINIO_PATTERNS_BUCKET:=patterns}"
: "${MINIO_SEED_PATTERNS_DIR:=/seed/patterns}"

mc alias set "$MINIO_ALIAS" "$MINIO_ENDPOINT" "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD"


i=0
until mc ls "$MINIO_ALIAS" >/dev/null 2>&1; do
  i=$((i+1)); [ $i -gt 120 ] && echo "MinIO not ready" && exit 1
  sleep 1
done

for bucket in $(echo "$MINIO_BUCKETS" | tr ',' ' '); do
  [ -n "$bucket" ] || continue
  mc mb --ignore-existing "$MINIO_ALIAS/$bucket"
  mc anonymous set download "$MINIO_ALIAS/$bucket"
done

if [ -d "$MINIO_SEED_PATTERNS_DIR" ]; then
  mc mirror "$MINIO_SEED_PATTERNS_DIR" "$MINIO_ALIAS/$MINIO_PATTERNS_BUCKET"
fi
