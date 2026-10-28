#!/usr/bin/env python3
"""Verify the shipped online package cannot contain the offline workshop implementation."""
import os
import pathlib
import subprocess
import sys
import zipfile

apk = pathlib.Path(sys.argv[1])
sdk = pathlib.Path(os.environ['ANDROID_HOME'])
aapt = sdk / 'build-tools/35.0.0/aapt'
manifest = subprocess.check_output([str(aapt), 'dump', 'xmltree', str(apk), 'AndroidManifest.xml'], text=True)
assert 'package="ir.torsepan.mobile.online"' in manifest
assert 'ir.torsepan.mobile.online.firebaseinitprovider' in manifest
assert 'FOREGROUND_SERVICE' not in manifest
assert 'OfflineSyncJob' not in manifest
assert 'POST_NOTIFICATIONS' in manifest
if '--release' in sys.argv:
    assert 'android:debuggable' not in manifest
with zipfile.ZipFile(apk) as contents:
    assert not any(name.startswith('assets/') for name in contents.namelist())
    dex = [contents.read(name) for name in contents.namelist() if name.endswith('.dex')]
    for forbidden in (b'Lir/torsepan/mobile/OfflineSync;', b'Lir/torsepan/mobile/OfflineStore;', b'OfflineNative', b'appassets.androidplatform.net'):
        assert all(forbidden not in body for body in dex), forbidden
    assert any(b'config?app=online' in body for body in dex)
    assert any(b'MessageCredentialStore' in body for body in dex)
    assert not any(name.endswith(('.keystore', '.jks')) for name in contents.namelist())
print('PASS independent online APK: live panel, native message enrollment, no offline workshop or foreground service')
