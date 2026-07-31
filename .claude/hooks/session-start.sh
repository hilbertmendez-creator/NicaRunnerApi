#!/bin/bash
set -euo pipefail

# Only needed on Claude Code on the web: each remote session starts from a
# fresh, ephemeral container, so anything installed outside the git repo
# (binaries, ~/.claude global config) is gone by the next session.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

export PATH="$HOME/go/bin:$PATH"

# The export above only affects this hook's own subprocess. engram's MCP
# server is registered as a Claude Code plugin (installed by gentle-ai below)
# whose .mcp.json uses a bare `command: "engram"` — resolved via PATH at
# spawn time by the *main* Claude Code process, which never sees this
# subprocess's exported PATH. Without persisting it to $CLAUDE_ENV_FILE, the
# binary lands in $HOME/go/bin correctly but the running session (both
# later Bash tool calls and the engram MCP server spawn) can't find it,
# silently breaking engram memory for the rest of the session even though
# this hook reports success.
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  echo "export PATH=\"$HOME/go/bin:\$PATH\"" >> "$CLAUDE_ENV_FILE"
fi

# --- engram (persistent memory) -------------------------------------------
# Install first and put it on PATH: gentle-ai's own installer tries to fetch
# engram's release from the GitHub API, which 403s on this shared egress IP.
# When `engram` is already resolvable on PATH, gentle-ai skips that fetch.
if ! command -v engram >/dev/null 2>&1; then
  go install github.com/Gentleman-Programming/engram/cmd/engram@latest
fi

# engram's actual memory database lives at ~/.engram/engram.db (home dir),
# not in the repo, so it's just as ephemeral as everything else here. Reload
# whatever was last committed to the project's .engram/ export directory
# (see the UserPromptSubmit export step in .claude/settings.json) so past
# decisions/bugfixes/conventions are searchable again this session.
cd "${CLAUDE_PROJECT_DIR:-$PWD}"
if [ -d .engram ]; then
  engram sync --import || true
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

# --- gga (AI pre-commit review, installed globally by gentle-ai above) -----
# The git hook itself lives under .git/hooks/, which git never tracks, so it
# has to be re-linked into this checkout every session. The .gga config
# (provider, file patterns, rules file) is committed and persists on its own.
if command -v gga >/dev/null 2>&1; then
  gga install || true
fi

# --- impeccable (design critique skill) -------------------------------------
# The npx installer (`npx impeccable install`) pulls its skill bundle from
# impeccable.style, which this environment's egress policy blocks outright.
# The plugin-marketplace route only needs GitHub, so use that instead. Both
# the marketplace clone and the plugin cache live under ~/.claude/plugins/
# (not the repo), so they need reinstalling each session even though
# .claude/settings.json already records "impeccable@impeccable" as enabled.
claude plugin marketplace add pbakaus/impeccable || true
claude plugin install impeccable@impeccable --scope project || true
