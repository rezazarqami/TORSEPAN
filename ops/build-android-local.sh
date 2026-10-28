#!/usr/bin/env bash
# Gradle is required to package FCM's transitive classes and merged manifest.
set -euo pipefail
repo=$(cd "$(dirname "$0")/.." && pwd)
sdk=${1:?Pass Android SDK directory}
key=${2:?Pass the existing signing keystore; preserve it for updates}
out=${3:-"$repo/android/app/build/manual"}
export ANDROID_HOME="$sdk" ANDROID_SDK_ROOT="$sdk" TORSEPAN_ANDROID_KEYSTORE="$key"
[[ -f "$key" ]] || { echo 'Existing signing keystore not found.' >&2; exit 1; }
mkdir -p "$out"
(cd "$repo/android" && "${GRADLE:-gradle}" :app:assembleDebug --no-daemon)
cp "$repo/android/app/build/outputs/apk/debug/app-debug.apk" "$out/torsepan-2.10.0-message-push.apk"
"$sdk/build-tools/35.0.0/apksigner" verify --verbose "$out/torsepan-2.10.0-message-push.apk"
