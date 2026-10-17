#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
preview_dotnet="${THEME_DOTNET:-dotnet}"
preview_panel="${THEME_PANEL_PROJECT:-TORSEPAN.Panel}"
preview_logs="${THEME_LOG_DIR:-/tmp}"
mkdir -p "$preview_logs"
node tests/theme-preview-fixture.cjs > "$preview_logs/torsepan-theme-fixture.log" 2>&1 &
fixture_pid=$!
Api__BaseUrl=http://127.0.0.1:5169 ASPNETCORE_URLS=http://127.0.0.1:5170 ASPNETCORE_ENVIRONMENT=Development "$preview_dotnet" run --project "$preview_panel" --no-build -c Release --no-launch-profile > "$preview_logs/torsepan-theme-panel.log" 2>&1 &
panel_pid=$!
cleanup() {
    kill "$fixture_pid" "$panel_pid" 2>/dev/null || true
    wait "$fixture_pid" 2>/dev/null || true
    wait "$panel_pid" 2>/dev/null || true
}
trap cleanup EXIT
for attempt in $(seq 1 60); do
    if curl -sf http://127.0.0.1:5170/ > /dev/null; then break; fi
    sleep .5
done
read -r -a preview_scripts <<< "${THEME_BROWSER_SCRIPTS:-${THEME_BROWSER_SCRIPT:-tests/theme-browser.cjs}}"
for preview_script in "${preview_scripts[@]}"; do
    node "$preview_script"
done
