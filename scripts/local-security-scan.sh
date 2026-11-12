#!/usr/bin/env bash
set -u

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
HOOK_MODE="false"

if [ "${1:-}" = "--hook" ]; then
  HOOK_MODE="true"
fi

FAILURES=0

print_header() {
  printf "\n[security] %s\n" "$1"
}

pass() {
  printf "[PASS] %s\n" "$1"
}

warn() {
  printf "[WARN] %s\n" "$1"
}

fail() {
  printf "[FAIL] %s\n" "$1"
  printf "       fix: %s\n" "$2"
  FAILURES=$((FAILURES + 1))
}

check_dockerfile_non_root() {
  local file="$1"
  if [ ! -f "$file" ]; then
    warn "$file not found; skipping"
    return
  fi

  local final_stage user_line user_value
  final_stage="$(awk 'BEGIN {capture=0; block=""} /^[[:space:]]*FROM[[:space:]]/ {capture=1; block=""} {if (capture) block = block $0 "\n"} END {printf "%s", block}' "$file")"
  user_line="$(printf "%s" "$final_stage" | grep -Ei '^[[:space:]]*USER[[:space:]]+' | tail -n 1 || true)"

  if [ -z "$user_line" ]; then
    fail "$file final stage has no USER instruction (container runs as root by default)." "Add a non-root user in final stage, then set USER <name>. Example: adduser -D appuser && USER appuser"
    return
  fi

  user_value="$(printf "%s" "$user_line" | sed -E 's/^[[:space:]]*USER[[:space:]]+//I' | awk '{print $1}')"
  if [ "$user_value" = "root" ] || [ "$user_value" = "0" ]; then
    fail "$file final stage runs as root (USER $user_value)." "Create/use a dedicated non-root user and replace USER $user_value with that account."
    return
  fi

  pass "$file final stage uses non-root user ($user_value)."
}

check_nginx_internal_proxy() {
  local file="$1"
  if [ ! -f "$file" ]; then
    warn "$file not found; skipping"
    return
  fi

  if awk '
    /location[[:space:]]+\/api\/[[:space:]]*\{/ {in_api=1; found_api=1; next}
    in_api && /^[[:space:]]*\}/ {in_api=0}
    in_api && /proxy_pass/ {unsafe_public_proxy=1}
    in_api && /@api_gateway/ {delegates_to_internal=1}
    /location[[:space:]]+@api_gateway[[:space:]]*\{/ {in_gateway=1; found_gateway=1; next}
    in_gateway && /^[[:space:]]*\}/ {in_gateway=0}
    in_gateway && /^[[:space:]]*internal;[[:space:]]*$/ {gateway_internal=1}
    in_gateway && /proxy_pass/ {gateway_proxy=1}
    END {
      if (!found_api || !found_gateway) exit 2
      if (unsafe_public_proxy || !delegates_to_internal || !gateway_internal || !gateway_proxy) exit 1
    }
  ' "$file"; then
    pass "$file /api/ delegates to an internal upstream proxy location"
    return
  fi

  local status=$?
  if [ $status -eq 2 ]; then
    fail "$file is missing the public /api/ or internal @api_gateway location." "Define both locations and delegate /api/ to @api_gateway."
  else
    fail "$file exposes an unsafe nginx upstream proxy pattern." "Keep proxy_pass only in internal @api_gateway and delegate /api/ to it."
  fi
}

run_npm_audit_high() {
  local dir="$1"
  local label="$2"

  if [ ! -f "$dir/package.json" ]; then
    warn "$label package.json not found; skipping npm audit"
    return
  fi

  if ! command -v npm >/dev/null 2>&1; then
    warn "npm not available; cannot run $label npm audit. Install Node.js/npm to enable this required check."
    return
  fi

  local out_file
  out_file="$(mktemp)"

  if (cd "$dir" && npm audit --audit-level=high --omit=dev >"$out_file" 2>&1); then
    pass "$label npm audit found no high/critical vulnerabilities in production deps."
  else
    fail "$label npm audit detected high/critical vulnerabilities." "Run: (cd $label && npm audit) then (cd $label && npm audit fix). Review unresolved advisories before commit."
    tail -n 6 "$out_file" | sed 's/^/       /'
  fi

  rm -f "$out_file"
}

run_dotnet_vuln_check() {
  local sln="$1"

  if [ ! -f "$sln" ]; then
    warn "$sln not found; skipping dotnet vulnerability check"
    return
  fi

  if ! command -v dotnet >/dev/null 2>&1; then
    warn "dotnet CLI not available; cannot run vulnerable package check."
    return
  fi

  local out_file
  out_file="$(mktemp)"

  if ! dotnet list "$sln" package --vulnerable --include-transitive >"$out_file" 2>&1; then
    warn "dotnet vulnerable package scan command failed. Re-run manually: dotnet list $sln package --vulnerable --include-transitive"
    tail -n 6 "$out_file" | sed 's/^/       /'
    rm -f "$out_file"
    return
  fi

  if grep -Eiq '\b(Critical|High)\b' "$out_file"; then
    fail "backend vulnerable package scan reported High/Critical issues." "Run: dotnet list $sln package --vulnerable --include-transitive. Upgrade affected packages with dotnet add package <Package> --version <SafeVersion>."
    grep -Ein '\b(Critical|High)\b' "$out_file" | head -n 8 | sed 's/^/       /'
  else
    pass "backend vulnerable package scan found no High/Critical issues."
  fi

  rm -f "$out_file"
}

run_optional_semgrep() {
  if command -v semgrep >/dev/null 2>&1; then
    warn "Optional: semgrep is installed. For deeper static checks run: semgrep scan --config auto"
  else
    warn "Optional: semgrep not installed. To enable extra checks: brew install semgrep (macOS) or pipx install semgrep (Linux/macOS)."
  fi
}

print_header "Local security scan"
check_dockerfile_non_root "$REPO_ROOT/frontend/Dockerfile"
check_dockerfile_non_root "$REPO_ROOT/mcp/context-broker/Dockerfile"
check_dockerfile_non_root "$REPO_ROOT/backend/src/MigrationRunner/Dockerfile"
check_nginx_internal_proxy "$REPO_ROOT/frontend/nginx.conf"
run_npm_audit_high "$REPO_ROOT/frontend" "frontend"
run_npm_audit_high "$REPO_ROOT/mcp/context-broker" "mcp/context-broker"
run_dotnet_vuln_check "$REPO_ROOT/backend/mow2.sln"
run_optional_semgrep

if [ "$FAILURES" -gt 0 ]; then
  printf "\n[security] Completed with %s blocking issue(s).\n" "$FAILURES"
  printf "[security] Commit blocked. Apply suggested fixes or bypass once with: git commit --no-verify\n"
  exit 1
fi

printf "\n[security] Completed with no blocking issues.\n"
if [ "$HOOK_MODE" = "true" ]; then
  printf "[security] Commit checks passed.\n"
fi
exit 0
