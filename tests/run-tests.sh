#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
RESULTS="$SCRIPT_DIR/_results"
mkdir -p "$RESULTS"

ARGS=(
  test
  "$REPO_ROOT/Todo-App.sln"
  --logger "trx;LogFilePrefix=test-results"
  --logger "console;verbosity=normal"
  --results-directory "$RESULTS"
)

if [[ "${1:-}" != "" ]]; then
  ARGS+=(--filter "$1")
fi

dotnet "${ARGS[@]}"
