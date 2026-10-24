#!/bin/bash
set -u

if (( $# < 3 )); then exit 2; fi
buddy="$1"
shift
payload="${!#}"
"$buddy" --hook codex "$payload" >/dev/null 2>&1 || true
exec "$@"
