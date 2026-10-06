import http.client
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import threading
import unittest
from http.server import ThreadingHTTPServer
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('backup_relay', Path(__file__).with_name('relay.py'))
relay = importlib.util.module_from_spec(spec)
spec.loader.exec_module(relay)

class BackupRelayTests(unittest.TestCase):
    def send(self, result=None, error=None, secret='fixture-secret'):
        server = ThreadingHTTPServer(('127.0.0.1', 0), relay.RelayHandler)
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        body = b'--fixture\r\nContent-Disposition: form-data; name="backup"; filename="data.dump"\r\nContent-Type: application/octet-stream\r\n\r\nPGDMP-fixture\r\n--fixture--\r\n'
        try:
            with patch.dict(os.environ, RELAY_SECRET='fixture-secret', TELEGRAM_CHAT_ID='fixture-chat', TELEGRAM_BOT_TOKEN='private-token'), patch.object(relay.subprocess, 'run', return_value=result, side_effect=error) as run:
                client = http.client.HTTPConnection('127.0.0.1', server.server_port, timeout=5)
                client.request('POST', '/database-backup', body, {'Content-Type': 'multipart/form-data; boundary=fixture', 'X-Relay-Secret': secret})
                response = client.getresponse()
                data = response.read().decode()
                code = response.status
                client.close()
                return code, data, run.call_count
        finally:
            server.shutdown()
            server.server_close()
            thread.join()

    def test_success_requires_telegram_confirmation(self):
        result = subprocess.CompletedProcess([], 0, '{"ok": true}', '')
        code, body, calls = self.send(result=result)
        self.assertEqual((code, calls), (200, 1))
        self.assertEqual(json.loads(body)['status'], 'sent')

    def test_upload_failure_returns_retryable_error_without_credentials(self):
        result = subprocess.CompletedProcess([], 22, '', 'https://api.telegram.org/botprivate-token/error')
        code, body, calls = self.send(result=result)
        self.assertEqual((code, calls), (502, 1))
        self.assertNotIn('private-token', body)

    def test_timeout_is_reported(self):
        code, body, _ = self.send(error=subprocess.TimeoutExpired('curl', 330))
        self.assertEqual(code, 504)
        self.assertIn('timed out', body)

    def test_rejected_telegram_response_is_not_success(self):
        code, _, _ = self.send(result=subprocess.CompletedProcess([], 0, '{"ok": false}', ''))
        self.assertEqual(code, 502)

    def test_wrong_secret_does_not_send(self):
        code, _, calls = self.send(secret='wrong')
        self.assertEqual((code, calls), (401, 0))

if __name__ == '__main__':
    unittest.main()
