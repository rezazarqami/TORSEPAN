#!/usr/bin/env python3
"""Classify runtime impact against each app's verified deployed commit."""
import argparse
import json
import subprocess

PRODUCT_PREFIXES = {
    'panel': ('TORSEPAN.Panel/',),
    'api': ('TORSEPAN.API/', 'TORSEPAN.Infrastructure/'),
}
SHARED_PREFIXES = ('TORSEPAN.Application/', 'TORSEPAN.Domain/')
NON_RUNTIME_PREFIXES = ('tests/', 'ops/', '.github/', 'android/', 'TORSEPAN.Panel.Offline/', 'TORSEPAN.Web/')
PRODUCT_FILES = {
    'Dockerfile.panel': 'panel', 'Dockerfile.panel.dockerignore': 'panel', 'liara.panel.json': 'panel',
    'Dockerfile.api': 'api', 'Dockerfile.api.dockerignore': 'api', 'liara.api.json': 'api',
}
SHARED_FILES = {'Dockerfile', '.dockerignore', 'start-service.sh', 'liara.json', 'NuGet.Config', 'nuget.config', 'global.json'}


def affected_products(path):
    if path in PRODUCT_FILES:
        return {PRODUCT_FILES[path]}
    if path in SHARED_FILES or path.startswith(SHARED_PREFIXES):
        return {'panel', 'api'}
    for product, prefixes in PRODUCT_PREFIXES.items():
        if path.startswith(prefixes):
            return {product}
    if path.startswith(NON_RUNTIME_PREFIXES) or path.endswith(('.md', '.sln', '.slnx', '.slnLaunch')) or path in {'.gitignore', '.gitattributes'}:
        return set()
    # Unknown inputs must never silently skip a product.
    return {'panel', 'api'}


def changed_paths(base, head):
    for ref in (base, head):
        subprocess.run(['git', 'rev-parse', '--verify', ref + '^{commit}'], check=True, stdout=subprocess.DEVNULL)
    subprocess.run(['git', 'merge-base', '--is-ancestor', base, head], check=True)
    data = subprocess.check_output(['git', 'diff', '--name-only', '--no-renames', '-z', base, head])
    return data.decode('utf-8').rstrip('\0').split('\0') if data else []


def plan(panel_base, api_base, head):
    result = {'head': head, 'products': {}}
    for product, base in (('panel', panel_base), ('api', api_base)):
        reasons = [p for p in changed_paths(base, head) if product in affected_products(p)]
        result['products'][product] = {'base': base, 'deploy': bool(reasons), 'changed_inputs': reasons}
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--panel-base', required=True, help='Last verified deployed panel SHA')
    parser.add_argument('--api-base', required=True, help='Last verified deployed API SHA')
    parser.add_argument('--head', default='HEAD')
    args = parser.parse_args()
    print(json.dumps(plan(args.panel_base, args.api_base, args.head), indent=2))
