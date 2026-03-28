#!/bin/bash

UNITY="C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe"
PROJECT="$(cd "$(dirname "$0")" && pwd -W)"
LOG="/tmp/unity-build.log"

case "$1" in
  compile)
    echo "Compiling..."
    "$UNITY" -projectPath "$PROJECT" -batchmode -quit -logFile "$LOG" 2>&1
    if grep -q "error CS" "$LOG" 2>/dev/null; then
      echo "COMPILE ERRORS:"
      grep "error CS" "$LOG"
      exit 1
    else
      echo "OK - No errors"
    fi
    ;;
  *)
    echo "Opening Unity Editor..."
    cmd.exe /c start "" "$UNITY" -projectPath "$PROJECT"
    ;;
esac
