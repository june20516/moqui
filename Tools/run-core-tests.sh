#!/usr/bin/env sh
# Git Bash 래퍼. 정본은 run-core-tests.ps1 (plan/decisions.md D-022).
exec powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(dirname "$0")/run-core-tests.ps1" "$@"
