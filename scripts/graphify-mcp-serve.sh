#!/usr/bin/env bash
# Starts the graphify MCP stdio server against this repo's graph.json.
# Resolves the graphify interpreter the same way the graphify skill does
# (uv tool / pipx / venv shebang), so it works regardless of how each
# developer installed `graphifyy` locally.
set -euo pipefail

ROOT="$(git rev-parse --show-toplevel)"
PY="python3"

GRAPHIFY_BIN="$(command -v graphify || true)"
if [ -n "$GRAPHIFY_BIN" ]; then
  SHEBANG="$(head -1 "$GRAPHIFY_BIN" | tr -d '#!')"
  if "$SHEBANG" -c "import graphify" 2>/dev/null; then
    PY="$SHEBANG"
  fi
fi

exec "$PY" -m graphify.serve "$ROOT/graphify-out/graph.json"
