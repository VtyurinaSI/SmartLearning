#!/bin/sh
set -e

DLL="${SERVICE_DLL}"
if [ -z "$DLL" ]; then
  DLL="$(ls /app/*.dll 2>/dev/null | head -n1)"
fi

if [ -z "$DLL" ]; then
  echo "ERROR: .dll не найден и SERVICE_DLL не задан"
  exit 1
fi

exec dotnet "$DLL"
