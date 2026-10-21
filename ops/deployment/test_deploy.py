import contextlib
import importlib.util
import io
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('selective_deploy', Path(__file__).with_name('deploy.py'))
deploy = importlib.util.module_from_spec(spec)
spec.loader.exec_module(deploy)
SHA = 'a' * 40


class FakeProcess:
    def __init__(self, code):
        self.stdout = ['CLI output with fixture-token\n']
        self.code = code
    def wait(self):
        return self.code


class DeployTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.cwd = os.getcwd()
        os.chdir(self.temp.name)
        Path('ops/deployment').mkdir(parents=True)
        Path('ops/deployment/baselines.json').write_text(json.dumps({'panel': {'app': 'torsepan', 'health_url': 'https://fixture.invalid/health'}}))
        self.plan = {'head': SHA, 'products': {'panel': {'deploy': True}}}
        self.write_plan()
    def tearDown(self):
        os.chdir(self.cwd)
        self.temp.cleanup()
    def write_plan(self):
        Path('.liara-release-plan.json').write_text(json.dumps(self.plan))
    def test_failed_deploy_never_records_receipt(self):
        with patch.dict(os.environ, {'LIARA_API_TOKEN': 'fixture-token'}, clear=True), patch.object(deploy.subprocess, 'check_output', return_value=SHA+'\n'), patch.object(deploy.subprocess, 'Popen', return_value=FakeProcess(1)), contextlib.redirect_stdout(io.StringIO()):
            with self.assertRaises(RuntimeError): deploy.deploy('panel')
        self.assertFalse(Path('.liara-panel-receipt.json').exists())
    def test_unselected_product_cannot_deploy(self):
        self.plan['products']['panel']['deploy'] = False
        self.write_plan()
        with patch.object(deploy.subprocess, 'check_output', return_value=SHA+'\n'), patch.object(deploy.subprocess, 'Popen') as process:
            with self.assertRaises(RuntimeError): deploy.deploy('panel')
            process.assert_not_called()
    def test_successful_cli_and_health_required_and_token_redacted(self):
        response = io.BytesIO(b'{"status":"healthy"}'); response.status = 200
        output = io.StringIO()
        with patch.dict(os.environ, {'LIARA_API_TOKEN': 'fixture-token'}, clear=True), patch.object(deploy.subprocess, 'check_output', return_value=SHA+'\n'), patch.object(deploy.subprocess, 'Popen', return_value=FakeProcess(0)) as process, patch.object(deploy.urllib.request, 'urlopen', return_value=response), contextlib.redirect_stdout(output):
            deploy.deploy('panel')
        receipt = json.loads(Path('.liara-panel-receipt.json').read_text())
        self.assertTrue(receipt['healthy']); self.assertEqual(receipt['sha'], SHA)
        self.assertNotIn('fixture-token', output.getvalue())
        command = process.call_args.args[0]
        self.assertIn('Dockerfile.panel', command); self.assertIn('torsepan', command)
        self.assertNotIn('torsepan-api', command)


if __name__ == '__main__':
    unittest.main()
