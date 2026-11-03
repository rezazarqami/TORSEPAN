#!/usr/bin/env bash
# Prepare a signed offline install or update; never publishes or deploys.
set -euo pipefail
repo=$(cd "$(dirname "$0")/.." && pwd)
export ANDROID_HOME=${1:?Pass Android SDK directory}
export ANDROID_SDK_ROOT="$ANDROID_HOME"
export TORSEPAN_ANDROID_KEYSTORE=${2:?Pass existing signing keystore}
previous=${3:?Pass previous independent APK or --fresh-install}
out=${4:-"$repo/android/app/build/release/torsepan-offline-3.0.3.apk"}
signer="$ANDROID_HOME/build-tools/35.0.0/apksigner"
aapt="$ANDROID_HOME/build-tools/35.0.0/aapt"
[[ -f "$TORSEPAN_ANDROID_KEYSTORE" ]] || { echo 'Existing signing keystore is required.' >&2; exit 1; }
[[ "$previous" == --fresh-install || -f "$previous" ]] || { echo 'Previous independent APK or --fresh-install is required.' >&2; exit 1; }
bash "$repo/ops/build-shared-offline.sh"
(cd "$repo" && node tests/SharedOffline/package.cjs)
(cd "$repo/android" && "${GRADLE:-gradle}" :app:assembleRelease --no-daemon)
apk="$repo/android/app/build/outputs/apk/release/app-release.apk"
if [[ "$previous" == --fresh-install ]]; then
    python3 "$repo/tests/offline-apk.py" --fresh-install "$signer" "$aapt" "$apk"
else
    python3 "$repo/tests/offline-apk.py" "$signer" "$aapt" "$previous" "$apk"
fi
mkdir -p "$(dirname "$out")"
cp "$apk" "$out"
printf 'Verified offline APK: %s\n' "$out"
