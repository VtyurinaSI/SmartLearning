#!/bin/sh

set -e

: "${MINIO_ENDPOINT:=http://minio:9000}"
: "${MINIO_ROOT_USER:=minioadmin}"
: "${MINIO_ROOT_PASSWORD:=minioadmin}"
: "${MINIO_ALIAS:=local}"
: "${MINIO_BUCKET:=my-bucket}"

mc alias set "$MINIO_ALIAS" "$MINIO_ENDPOINT" "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD"


i=0
until mc ls "$MINIO_ALIAS" >/dev/null 2>&1; do
  i=$((i+1)); [ $i -gt 60 ] && echo "MinIO not ready" && exit 1
  sleep 1
done

mc mb --ignore-existing "$MINIO_ALIAS/$MINIO_BUCKET"
mc anonymous set download "$MINIO_ALIAS/$MINIO_BUCKET"