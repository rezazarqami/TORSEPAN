#!/usr/bin/env bash
set -euo pipefail
repo=$(cd "$(dirname "$0")/.." && pwd)
# Hosts that cannot launch MSBuild task-host processes can opt in after restore:
# python3 ops/prepare-wasm-inprocess.py /tmp/torsepan-wasm.targets
# TORSEPAN_WASM_TARGETS=/tmp/torsepan-wasm.targets ops/build-shared-offline.sh
args=(-c Release -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false -p:PublishTrimmed=false -p:BlazorEnableCompression=false)
if [[ -n "${TORSEPAN_WASM_TARGETS:-}" ]]; then args+=("-p:CustomAfterMicrosoftCommonTargets=$TORSEPAN_WASM_TARGETS"); fi
"${DOTNET:-dotnet}" publish "$repo/TORSEPAN.Panel.Offline" "${args[@]}"
python3 - "$repo" <<'COPY'
import pathlib,shutil,sys
root=pathlib.Path(sys.argv[1]);target=root/'android/app/src/main/assets/panel'
if target.exists():shutil.rmtree(target)
shutil.copytree(root/'TORSEPAN.Panel.Offline/bin/Release/net10.0/publish/wwwroot',target,ignore=shutil.ignore_patterns('*.pdb','*.gz','*.br','*.map'))
COPY
if [[ $# -ge 2 ]]; then bash "$repo/ops/build-android-local.sh" "$@"; fi
