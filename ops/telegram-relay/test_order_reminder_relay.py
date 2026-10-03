import importlib.util
import json
import os
import sqlite3
import tempfile
import threading
import unittest
import urllib.error
import urllib.request
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("relay", Path(__file__).with_name("order_reminder_relay.py"))
relay = importlib.util.module_from_spec(spec)
spec.loader.exec_module(relay)
KEY = "order:0123456789abcdef0123456789abcdef:1"


class DeliveryTests(unittest.TestCase):
    def test_concurrent_delivery_and_restart_receipts(self):
        with tempfile.TemporaryDirectory() as directory:
            database = str(Path(directory) / "receipts.sqlite3")
            # Initialize before competing writes to test the production transaction.
            calls = []
            with ThreadPoolExecutor(4) as pool:
                results = list(pool.map(lambda _: relay.deliver_once(database, KEY, "یادآوری", calls.append), range(4)))
            self.assertEqual(results.count("sent"), 1)
            self.assertEqual(results.count("already-sent"), 3)
            self.assertEqual(calls, ["یادآوری"])
            self.assertEqual(relay.deliver_once(database, KEY, "بعد از ری‌استارت", calls.append), "already-sent")

    def test_failed_send_does_not_consume_milestone(self):
        with tempfile.TemporaryDirectory() as directory:
            database = str(Path(directory) / "receipts.sqlite3")
            def fail(text):
                raise OSError("offline")
            with self.assertRaises(OSError):
                relay.deliver_once(database, KEY, "پیام", fail)
            self.assertEqual(relay.deliver_once(database, KEY, "پیام", lambda _: None), "sent")

    def test_http_auth_size_validation_and_confirmed_delivery(self):
        with tempfile.TemporaryDirectory() as directory, patch.dict(os.environ, {
            "RELAY_SECRET": "test-only-secret", "ORDER_RELAY_DB": str(Path(directory) / "receipts.sqlite3")
        }), patch.object(relay, "deliver_once", return_value="sent") as delivery:
            server = relay.ThreadingHTTPServer(("127.0.0.1", 0), relay.Handler)
            thread = threading.Thread(target=server.serve_forever, daemon=True)
            thread.start()
            try:
                url = f"http://127.0.0.1:{server.server_port}/order-reminder"
                def send(payload, secret="test-only-secret"):
                    request = urllib.request.Request(url, json.dumps(payload).encode(),
                        {"Content-Type": "application/json", "X-Relay-Secret": secret})
                    return urllib.request.urlopen(request)
                for payload, secret, status in [({"deliveryKey": KEY, "text": "پیام"}, "wrong", 401),
                    ({"deliveryKey": "invalid", "text": "پیام"}, "test-only-secret", 400),
                    ({"deliveryKey": KEY, "text": "x" * 18000}, "test-only-secret", 413)]:
                    with self.assertRaises(urllib.error.HTTPError) as error:
                        send(payload, secret)
                    self.assertEqual(error.exception.code, status)
                delivery.assert_not_called()
                with send({"deliveryKey": KEY, "text": "پیام"}) as response:
                    self.assertEqual(json.load(response), {"status": "sent"})
                delivery.assert_called_once()
            finally:
                server.shutdown()
                server.server_close()


if __name__ == "__main__":
    unittest.main()
