#!/usr/bin/env bash
set -eu

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

cd "$REPO_ROOT"
git config --local core.hooksPath .githooks

echo "Configured local git hooks path: $(git config --local core.hooksPath)"
echo "Hooks directory: $REPO_ROOT/.githooks"
