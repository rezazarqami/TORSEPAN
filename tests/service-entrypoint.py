import os
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]


class ServiceEntrypointTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        for name in ('panel', 'api', 'bin'):
            (self.root / name).mkdir()
        dotnet = self.root / 'bin/dotnet'
        dotnet.write_text('#!/bin/sh\nprintf "%s\\n" "$PWD" "$@"\n')
        dotnet.chmod(0o755)

    def run_service(self, service):
        env = {'PATH': str(self.root / 'bin') + os.pathsep + os.defpath,
               'TORSEPAN_APP_ROOT': str(self.root), 'TORSEPAN_SERVICE': service}
        return subprocess.run(['/bin/sh', str(ROOT / 'start-service.sh'), '--test-argument'], env=env, text=True, capture_output=True)

    def test_each_app_launches_its_own_assembly_and_working_directory(self):
        for service, assembly in [('api', 'TORSEPAN.API.dll'), ('panel', 'TORSEPAN.Panel.dll')]:
            with self.subTest(service=service):
                result = self.run_service(service)
                self.assertEqual(result.returncode, 0)
                self.assertEqual(result.stdout.splitlines(), [str(self.root / service), assembly, '--test-argument'])

    def test_unknown_or_missing_selector_starts_neither_product(self):
        for service in ['', 'wrong', '../panel']:
            result = self.run_service(service)
            self.assertEqual(result.returncode, 64)
            self.assertEqual(result.stdout, '')


if __name__ == '__main__':
    unittest.main()
