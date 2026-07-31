#!/bin/bash
set -euo pipefail

# Only needed on Claude Code on the web: each remote session starts from a
# fresh, ephemeral container, so anything installed outside the git repo
# (binaries, ~/.claude global config) is gone by the next session.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

export PATH="$HOME/go/bin:$PATH"

# --- engram (persistent memory) -------------------------------------------
# Install first and put it on PATH: gentle-ai's own installer tries to fetch
# engram's release from the GitHub API, which 403s on this shared egress IP.
# When `engram` is already resolvable on PATH, gentle-ai skips that fetch.
if ! command -v engram >/dev/null 2>&1; then
  go install github.com/Gentleman-Programming/engram/cmd/engram@latest
fi

# --- gentle-ai (persona, skills, SDD workflow, review agents, engram wiring)
if ! command -v gentle-ai >/dev/null 2>&1; then
  go install github.com/gentleman-programming/gentle-ai/v2/cmd/gentle-ai@latest
fi
gentle-ai install --agents claude-code --scope workspace </dev/null || true

# --- codegraph (local code knowledge graph + MCP server) -------------------
if ! command -v codegraph >/dev/null 2>&1; then
  npm i -g @colbymchenry/codegraph
fi
codegraph install -y --location local --target claude || true

cd "${CLAUDE_PROJECT_DIR:-$PWD}"
if [ -d .codegraph ]; then
  codegraph sync || true
else
  codegraph init || true
fi
