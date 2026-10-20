#!/usr/bin/env python3
"""Report affected products; this command never publishes or deploys anything."""
import argparse
import json
import subprocess


def classify(paths):
    targets = set()
    for path in paths:
        if path.startswith(('TORSEPAN.Application/', 'TORSEPAN.Domain/')):
            targets.update(('panel', 'api', 'android'))
        elif path.startswith(('TORSEPAN.API/', 'TORSEPAN.Infrastructure/')) or path in ('Dockerfile.api', 'Dockerfile.api.dockerignore', 'liara.api.json'):
            targets.add('api')
        elif path.startswith('TORSEPAN.Panel/'):
            # The offline Android project also links panel source and assets.
            targets.update(('panel', 'android'))
        elif path.startswith(('TORSEPAN.Panel.Offline/', 'android/')) or path == 'ops/build-shared-offline.sh':
            targets.add('android')
        elif path in ('Dockerfile.panel', 'Dockerfile.panel.dockerignore', 'liara.panel.json'):
            targets.add('panel')
        elif path in ('Dockerfile', 'start-service.sh', '.dockerignore', 'liara.json', 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', 'NuGet.Config', 'nuget.config') or path.startswith(('.github/workflows/', 'ops/deployment/')):
            targets.update(('panel', 'api', 'android'))
        elif not path.startswith(('docs/', 'tests/')) and path not in ('AGENTS.md', 'README.md', '.gitignore', '.gitattributes'):
            # Unknown build inputs must be reviewed, never silently skipped.
            targets.update(('panel', 'api', 'android'))
    return {name: name in targets for name in ('panel', 'api', 'android')}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base', required=True, help='Last verified deployed commit, not the preceding commit')
    parser.add_argument('--head', default='HEAD')
    args = parser.parse_args()
    paths = subprocess.check_output(['git', 'diff', '--name-only', '--no-renames', args.base, args.head], text=True).splitlines()
    print(json.dumps({'base': args.base, 'head': args.head, 'targets': classify(paths), 'files': paths}, indent=2))
