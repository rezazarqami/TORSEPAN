#!/usr/bin/env python3
"""Deploy one planned app, verify health, and write a credential-free receipt."""
import argparse
from datetime import datetime, timezone
import re
import json
import os
from pathlib import Path
import subprocess
import time
import urllib.request


def migration_inputs(plan, product):
    if product != 'api':
        return []
    return sorted(Path(p).stem for p in plan['products'][product].get('changed_inputs', [])
                  if p.startswith('TORSEPAN.Infrastructure/') and '/Migrations/' in p
                  and re.match(r'^\d{14}_.+\.cs$', Path(p).name) and not p.endswith('.Designer.cs'))


def verify_backup(payload, now=None):
    backup = payload.get('databaseBackup') or {}
    stamp = backup.get('lastSuccessUtc')
    try:
        last = datetime.fromisoformat(stamp.replace('Z', '+00:00'))
        age = ((now or datetime.now(timezone.utc)) - last).total_seconds()
    except (AttributeError, TypeError, ValueError):
        raise RuntimeError('Schema update blocked: no verified successful database backup timestamp')
    if (backup.get('state') != 'succeeded' or backup.get('stage') != 'completed'
            or not isinstance(backup.get('archiveBytes'), int) or backup['archiveBytes'] <= 0
            or backup.get('transportError') or backup.get('error') or not 0 <= age <= 7200):
        raise RuntimeError('Schema update blocked: a successfully delivered database backup within two hours is required')
    return {'last_success_utc': stamp, 'archive_bytes': backup['archiveBytes'],
            'mode': backup.get('mode'), 'recovery_plan': 'docs/sales-profit-sharing.md'}


def deploy(product):
    plan = json.loads(Path('.liara-release-plan.json').read_text())
    settings = json.loads(Path('ops/deployment/baselines.json').read_text())[product]
    sha = subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()
    if plan['head'] != sha or not plan['products'][product]['deploy']:
        raise RuntimeError('Source differs from the successful-CI release plan, or product was not selected')
    token = os.environ.get('LIARA_API_TOKEN')
    if not token:
        raise RuntimeError('Missing LIARA_API_TOKEN GitHub Actions secret')
    app = {'panel': 'torsepan', 'api': 'torsepan-api'}[product]
    if settings['app'] != app:
        raise RuntimeError('Unexpected Liara app in deployment settings')
    health_url = settings.get('health_url')
    if not isinstance(health_url, str) or not health_url.startswith('https://') or not health_url.endswith('/health'):
        raise RuntimeError('A verified HTTPS health URL is required')
    migrations = migration_inputs(plan, product)
    backup_receipt = None
    if migrations:
        with urllib.request.urlopen(health_url, timeout=20) as response:
            backup_receipt = verify_backup(json.load(response))
        print('Pre-migration backup verified: ' + backup_receipt['last_success_utc'], flush=True)
    command = ['liara', 'deploy', '--app', app, '--liara-json', f'liara.{product}.json',
               '--dockerfile', f'Dockerfile.{product}', '--api-token', token,
               '--message', f'GitHub verified {product} {sha}', '--no-app-logs']
    if settings.get('team_id'):
        command.extend(['--team-id', settings['team_id']])
    print(f'Deploying {app} from tested commit {sha}', flush=True)
    # Never print a command line containing the token; redact all child output too.
    process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    for line in process.stdout:
        print(line.replace(token, '***'), end='', flush=True)
    if process.wait() != 0:
        raise RuntimeError(f'Liara deployment failed for {app}; baseline was not advanced')
    healthy = False
    for attempt in range(12):
        try:
            with urllib.request.urlopen(health_url, timeout=15) as response:
                payload = json.load(response)
                healthy = response.status == 200 and payload.get('status') == 'healthy'
                if migrations:
                    healthy = healthy and payload.get('databaseMigration') == migrations[-1]
            if healthy:
                break
        except (OSError, ValueError):
            pass
        if attempt < 11:
            time.sleep(5)
    if not healthy:
        raise RuntimeError(f'{app} health verification failed; baseline was not advanced')
    Path(f'.liara-{product}-receipt.json').write_text(json.dumps({
        'product': product, 'app': app, 'sha': sha, 'health_url': health_url, 'healthy': True,
        'database_migration': migrations[-1] if migrations else None, 'backup': backup_receipt,
        'verification': 'Liara CLI completed deployment of pinned checkout; HTTPS health check passed'
    }))
    if migrations:
        print('Verified applied database migration: ' + migrations[-1])
    print(f'Verified successful deployment and health for {app}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('product', choices=('panel', 'api'))
    deploy(parser.parse_args().product)
