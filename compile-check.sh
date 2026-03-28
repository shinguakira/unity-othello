#!/bin/bash
# Unity C# compile check script
# Usage: bash compile-check.sh

LOG_FILE="e:/tmp/unity-build.log"

echo "Compiling Unity project..."
"C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" \
  -projectPath "e:/workspace/unity-othello" \
  -batchmode -quit -logFile "$LOG_FILE" 2>&1

EXIT_CODE=$?

# Check for compile errors
ERRORS=$(grep -iE "error CS[0-9]" "$LOG_FILE" 2>/dev/null)

if [ -n "$ERRORS" ]; then
  echo "COMPILE ERRORS FOUND:"
  echo "$ERRORS"
  exit 1
elif [ $EXIT_CODE -ne 0 ]; then
  echo "Unity exited with code $EXIT_CODE. Check $LOG_FILE for details."
  exit 1
else
  echo "Compilation successful! No errors."
  exit 0
fi
