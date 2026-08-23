#!/usr/bin/env python3
import json
import os
import subprocess
import tempfile
from email.parser import BytesParser
from email.policy import default
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

HOST = "127.0.0.1"
PORT = 5051
MAX_BODY = 49 * 1024 * 1024


class RelayHandler(BaseHTTPRequestHandler):
    server_version = "TorsepanTelegramRelay/1.0"

    def do_GET(self):
        if self.path != "/health":
            self.send_error(404)
            return
        self._json(200, {"status": "healthy"})

    def do_POST(self):
        if self.path not in ("/database-backup", "/inventory-alert", "/payroll-report"):
            self.send_error(404)
            return
        if self.headers.get("X-Relay-Secret") != os.environ["RELAY_SECRET"]:
            self.send_error(401)
            return

        try:
            length = int(self.headers.get("Content-Length", "0"))
        except ValueError:
            self.send_error(400)
            return
        if length <= 0 or length > MAX_BODY:
            self.send_error(413)
            return

        raw = self.rfile.read(length)
        if self.path == "/inventory-alert":
            self._send_inventory_alert(raw)
            return

        message = BytesParser(policy=default).parsebytes(
            b"Content-Type: " + self.headers["Content-Type"].encode() + b"\r\n\r\n" + raw
        )
        field_name = "report" if self.path == "/payroll-report" else "backup"
        upload = next(
            (part for part in message.iter_parts()
             if part.get_param("name", header="content-disposition") == field_name),
            None,
        )
        if upload is None:
            self.send_error(400, f"{field_name} file is required")
            return

        filename = os.path.basename(upload.get_filename() or ("TORSEPAN-report.pdf" if self.path == "/payroll-report" else "TORSEPAN.dump"))
        caption = "گزارش عملکرد تولید TORSEPAN" if self.path == "/payroll-report" else "پشتیبان شبانه دیتابیس TORSEPAN"
        with tempfile.TemporaryDirectory(prefix="torsepan-backup-") as directory:
            path = os.path.join(directory, filename)
            with open(path, "wb") as stream:
                stream.write(upload.get_payload(decode=True))

            result = subprocess.run(
                [
                    "curl", "--fail-with-body", "--silent", "--show-error",
                    "--connect-timeout", "20", "--max-time", "300",
                    "-F", f"chat_id={os.environ['TELEGRAM_CHAT_ID']}",
                    "-F", f"caption={caption}",
                    "-F", f"document=@{path}",
                    f"https://api.telegram.org/bot{os.environ['TELEGRAM_BOT_TOKEN']}/sendDocument",
                ],
                capture_output=True,
                text=True,
                timeout=330,
            )
        if result.returncode != 0:
            raise RuntimeError(result.stderr.strip() or "Telegram upload failed")
        self._json(200, {"status": "sent"})

    def _send_inventory_alert(self, raw):
        alert = json.loads(raw.decode("utf-8"))
        text = (
            "⚠️ هشدار موجودی انبار مواد اولیه\n"
            f"{alert['itemName']} - {alert['stockType']}\n"
            f"موجودی فعلی: {alert['quantity']}\n"
            f"حد هشدار: {alert['threshold']}"
        )
        payload = json.dumps({
            "chat_id": os.environ["TELEGRAM_CHAT_ID"],
            "text": text,
        }, ensure_ascii=False)
        result = subprocess.run(
            [
                "curl", "--fail-with-body", "--silent", "--show-error",
                "--connect-timeout", "20", "--max-time", "60",
                "-H", "Content-Type: application/json",
                "--data-binary", payload,
                f"https://api.telegram.org/bot{os.environ['TELEGRAM_BOT_TOKEN']}/sendMessage",
            ],
            capture_output=True,
            text=True,
            timeout=75,
        )
        if result.returncode != 0:
            raise RuntimeError(result.stderr.strip() or "Telegram alert failed")
        self._json(200, {"status": "sent"})

    def log_message(self, fmt, *args):
        print(f"{self.address_string()} - {fmt % args}", flush=True)

    def _json(self, status, value):
        body = json.dumps(value).encode()
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)


if __name__ == "__main__":
    required = ("RELAY_SECRET", "TELEGRAM_BOT_TOKEN", "TELEGRAM_CHAT_ID")
    missing = [name for name in required if not os.environ.get(name)]
    if missing:
        raise RuntimeError("Missing configuration: " + ", ".join(missing))
    ThreadingHTTPServer((HOST, PORT), RelayHandler).serve_forever()
