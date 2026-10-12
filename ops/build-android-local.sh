#!/usr/bin/env bash
# Reproducible dependency-free build for this native WebView app.
set -euo pipefail
repo=$(cd "$(dirname "$0")/.." && pwd)
sdk=${1:?Pass Android SDK directory}
key=${2:?Pass existing signing keystore; preserve it for updates}
out=${3:-"$repo/android/app/build/manual"}
tools="$sdk/build-tools/35.0.0"
mkdir -p "$out/classes" "$out/dex"
python3 - "$repo/android/app/src/main/AndroidManifest.xml" "$out/AndroidManifest.xml" <<'PY'
import sys,xml.etree.ElementTree as E
E.register_namespace('android','http://schemas.android.com/apk/res/android');t=E.parse(sys.argv[1]);r=t.getroot();r.set('package','ir.torsepan.mobile');r.set('{http://schemas.android.com/apk/res/android}versionCode','4');r.set('{http://schemas.android.com/apk/res/android}versionName','2.0-offline');u=E.SubElement(r,'uses-sdk');u.set('{http://schemas.android.com/apk/res/android}minSdkVersion','29');u.set('{http://schemas.android.com/apk/res/android}targetSdkVersion','34');t.write(sys.argv[2],encoding='utf-8',xml_declaration=True)
PY
"$tools/aapt2" compile --dir "$repo/android/app/src/main/res" -o "$out/resources.zip"
"$tools/aapt2" link -I "$sdk/platforms/android-35/android.jar" --manifest "$out/AndroidManifest.xml" -o "$out/base.apk" --java "$out/generated" -A "$repo/android/app/src/main/assets" "$out/resources.zip"
find "$repo/android/app/src/main/java" "$out/generated" -name '*.java' > "$out/sources.txt"
javac -source 8 -target 8 -bootclasspath "$tools/core-lambda-stubs.jar:$sdk/platforms/android-35/android.jar" -d "$out/classes" @"$out/sources.txt"
jar cf "$out/classes.jar" -C "$out/classes" .
"$tools/d8" --lib "$sdk/platforms/android-35/android.jar" --min-api 29 --output "$out/dex" "$out/classes.jar"
cp "$out/base.apk" "$out/unsigned.apk"
(cd "$out/dex" && zip -q "$out/unsigned.apk" classes.dex)
"$tools/zipalign" -p -f 4 "$out/unsigned.apk" "$out/aligned.apk"
"$tools/apksigner" sign --ks "$key" --ks-key-alias androiddebugkey --ks-pass pass:android --key-pass pass:android --out "$out/torsepan-offline-2.0.apk" "$out/aligned.apk"
"$tools/apksigner" verify --verbose "$out/torsepan-offline-2.0.apk"
