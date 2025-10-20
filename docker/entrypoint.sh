#!/bin/sh
set -e

pick_from_runtimeconfig() {
  rc="$(ls /app/*.runtimeconfig.json 2>/dev/null | head -n1 || true)"
  if [ -n "$rc" ]; then
    echo "$(basename "$rc" .runtimeconfig.json).dll"
  fi
}

DLL=""
if [ -n "$APP_DLL" ]; then
  DLL="$APP_DLL"
elif [ -n "$SERVICE_DLL" ]; then
  DLL="$SERVICE_DLL"
else
  cand="$(pick_from_runtimeconfig || true)"
  if [ -n "$cand" ]; then
    DLL="$cand"
  else
    DLL="$(ls /app/*.dll 2>/dev/null | head -n1)"
  fi
fi

if [ -z "$DLL" ]; then
  echo "ERROR: .dll не найден и APP_DLL/SERVICE_DLL не заданы"
  ls -la /app
  exit 1
fi

exec dotnet "$DLL"
