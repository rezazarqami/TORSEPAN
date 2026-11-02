"""Verify shipped offline APK identity, monotonic version and complete local bundle."""
import re, subprocess, sys, zipfile
from pathlib import Path
bundle_only = len(sys.argv) == 3 and sys.argv[1] == "--bundle-only"
fresh_install = len(sys.argv) == 5 and sys.argv[1] == "--fresh-install"
if bundle_only: current = sys.argv[2]
elif fresh_install: signer, aapt, current = sys.argv[2:]
else: signer, aapt, previous, current = sys.argv[1:]
def certificate(apk):
    output = subprocess.check_output([signer, 'verify', '--print-certs', apk], text=True)
    values = re.findall(r'Signer #\d+ certificate SHA-256 digest: (\w+)', output)
    assert len(values) == 1, 'Expected one verified APK signer'
    return values[0]
def package(apk):
    output = subprocess.check_output([aapt, 'dump', 'badging', apk], text=True)
    match = re.search(r"package: name='([^']+)' versionCode='(\d+)' versionName='([^']+)'", output)
    assert match, 'Missing Android package metadata'
    return match[1], int(match[2]), match[3], output
if not bundle_only:
    new = package(current)
    assert new[0] == 'ir.torsepan.offline', 'Independent offline application identity is incorrect'
    assert new[1] >= 19, 'Independent offline version is incorrect'
    if fresh_install:
        certificate(current)
    else:
        old = package(previous)
        assert old[0] == new[0], 'Refuse to describe a different package as an in-place update'
        assert new[1] > old[1], 'Update version code must increase'
        assert certificate(previous) == certificate(current), 'Signing identity changed; refuse update'
    assert "application-label:'Torsepan Offline'" in new[3], 'Offline launcher name is incorrect'
    assert 'application-debuggable' not in new[3], 'Release must not be debuggable'
with zipfile.ZipFile(current) as archive:
    names = archive.namelist()
    for name in ['index.html', 'offline-panel.js', 'offline-workspace.js', 'offline-model.js', 'images/flags/lb.svg']:
        assert 'assets/panel/' + name in names, 'Missing bundled offline asset: ' + name
    assert any(n.startswith('assets/panel/_framework/') and n.endswith('.wasm') for n in names), 'Missing offline WebAssembly runtime'
    source = Path(__file__).resolve().parents[1] / 'android/app/src/main/assets/panel'
    for file in source.rglob('*'):
        if not file.is_file(): continue
        name = 'assets/panel/' + file.relative_to(source).as_posix()
        assert name in names, 'Bundled asset omitted by Android packaging: ' + name
        assert archive.read(name) == file.read_bytes(), 'Stale bundled asset: ' + name
    assert b'workPlanDay'  in archive.read('assets/panel/offline-panel.js'), 'Stale offline adapter'
if bundle_only: print('PASS complete APK offline bundle including every WebAssembly runtime asset')
else: print(f'PASS verified {"independent offline installation" if fresh_install else "in-place offline update"}: {new[0]} {new[2]} ({new[1]}); verified signer; complete offline bundle; release build')
