#!/usr/bin/env bash
# Build and verify an update for the existing online application. Never deploys.
set -euo pipefail
repo=$(cd "$(dirname "$0")/.." && pwd)
export ANDROID_HOME=${1:?Pass Android SDK directory}
export ANDROID_SDK_ROOT="$ANDROID_HOME"
export TORSEPAN_ANDROID_KEYSTORE=${2:?Pass the existing signing keystore}
previous=${3:?Pass the previously shipped signed online APK}
out=${4:-"$repo/TORSEPAN.Panel/AppPackages/torsepan-online-1.0.2.apk"}
signer="$ANDROID_HOME/build-tools/35.0.0/apksigner"
[[ -f "$TORSEPAN_ANDROID_KEYSTORE" && -f "$previous" ]] || { echo 'Signing keystore or previous APK not found.' >&2; exit 1; }
(cd "$repo/android" && "${GRADLE:-gradle}" :online:assembleRelease --no-daemon)
apk="$repo/android/online/build/outputs/apk/release/online-release.apk"
"$signer" verify --verbose "$apk"
(cd "$repo" && python3 tests/online-apk.py "$apk" --release)
python3 - "$signer" "$previous" "$apk" <<'PY'
import re,subprocess,sys
signer,previous,current=sys.argv[1:]
def certificate(apk):
    text=subprocess.check_output([signer,'verify','--print-certs',apk],text=True)
    values=re.findall(r'Signer #\d+ certificate SHA-256 digest: (\w+)',text)
    if len(values)!=1: raise SystemExit('Expected one verified signer.')
    return values[0]
if certificate(previous)!=certificate(current):
    raise SystemExit('Signing identity changed; refusing to replace the downloadable APK.')
print('PASS signing identity preserved for the installed online application')
PY
mkdir -p "$(dirname "$out")"
cp "$apk" "$out"
printf 'Verified APK: %s\n' "$out"
