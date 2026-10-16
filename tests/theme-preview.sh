#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
preview_dotnet="${THEME_DOTNET:-dotnet}"
node tests/theme-preview-fixture.cjs > /tmp/torsepan-theme-fixture.log 2>&1 &
fixture_pid=$!
Api__BaseUrl=http://127.0.0.1:5169 ASPNETCORE_URLS=http://127.0.0.1:5170 ASPNETCORE_ENVIRONMENT=Development "$preview_dotnet" run --project TORSEPAN.Panel --no-build -c Release --no-launch-profile > /tmp/torsepan-theme-panel.log 2>&1 &
panel_pid=$!
trap 'kill "$fixture_pid" "$panel_pid" 2>/dev/null || true' EXIT
for attempt in $(seq 1 60); do
    if curl -sf http://127.0.0.1:5170/ > /dev/null; then break; fi
    sleep .5
done
node "${THEME_BROWSER_SCRIPT:-tests/theme-browser.cjs}"
