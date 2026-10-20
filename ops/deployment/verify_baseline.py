#!/usr/bin/env python3
"""Check live source preservation before compiling a migration candidate."""
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
manifest = json.loads((ROOT / 'ops/deployment/live-baseline.json').read_text())


def git(*args):
    return subprocess.check_output(['git', '-C', str(ROOT), *args])


def verify_tree(ref, prefix):
    expected = set(git('ls-tree', '-r', '--name-only', ref, '--', prefix).decode().splitlines())
    actual = {p.relative_to(ROOT).as_posix() for p in (ROOT / prefix).rglob('*') if p.is_file() and 'bin' not in p.parts and 'obj' not in p.parts}
    extra = actual - expected
    if prefix == 'TORSEPAN.Panel':
        extra.discard('TORSEPAN.Panel/Services/ILocalDataRefresh.cs')
    assert not extra, f'Unreviewed extra files: {extra}'
    for path in expected:
        assert (ROOT / path).is_file(), f'Missing live file: {path}'
        assert (ROOT / path).read_bytes().replace(b'\r\n', b'\n') == git('show', f'{ref}:{path}').replace(b'\r\n', b'\n'), f'Live source changed: {path}'


for prefix in ('TORSEPAN.API', 'TORSEPAN.Application', 'TORSEPAN.Domain', 'TORSEPAN.Infrastructure'):
    verify_tree(manifest['api']['sha'], prefix)
verify_tree(manifest['panel']['sha'], 'TORSEPAN.Panel')
for service in ('panel', 'api'):
    assert (ROOT / f'Dockerfile.{service}').read_bytes() == git('show', manifest[service]['sha'] + ':Dockerfile')
for path in manifest['offline_overrides']:
    assert (ROOT / 'TORSEPAN.Panel.Offline' / path).read_bytes() == git('show', manifest['offline_preserved_from'] + ':TORSEPAN.Panel/' + path)
print('PASS: live panel, live API, shared server source, build recipes and offline variants preserved')
