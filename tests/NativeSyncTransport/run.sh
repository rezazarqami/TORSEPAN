#!/usr/bin/env bash
set -euo pipefail
repo=$(cd "$(dirname "$0")/../.." && pwd)
json=${TORSEPAN_JSON_JAR:?Set TORSEPAN_JSON_JAR to org.json jar}
out=${TORSEPAN_NATIVE_TEST_OUTPUT:-"$repo/android/app/build/transport-test"}
mkdir -p "$out"
find "$repo/tests/NativeSyncTransport" -name '*.java' > "$out/sources.txt"
printf '%s\n' "$repo/android/app/src/main/java/ir/torsepan/mobile/OfflineSync.java" >> "$out/sources.txt"
"${JAVAC:-javac}" -cp "$json" -d "$out" @"$out/sources.txt"
"${JAVA:-java}" -cp "$out:$json" ir.torsepan.mobile.TransportChecks
