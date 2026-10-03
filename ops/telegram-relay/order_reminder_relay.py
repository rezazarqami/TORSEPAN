#!/usr/bin/env python3
"""Authenticated, durable delivery receipts for Torsepan's order reminders.

Run alongside the existing backup relay; no changes to backup/photo handling.
Requires only Python's standard library. Destination and credentials come from
the existing root-owned environment file, never from request bodies.
"""
import hmac
import json
import os
import re
import sqlite3
import time
import urllib.error
import urllib.request
from contextlib import closing
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer


def env(*names):
    return next((os.environ[n] for n in names if os.environ.get(n)), "")


def telegram_send(text):
    token = env("TELEGRAM_BOT_TOKEN", "BOT_TOKEN", "TELEGRAM_TOKEN")
    chat = env("TELEGRAM_CHAT_ID", "CHAT_ID")
    if not token or not chat:
        raise RuntimeError("Telegram destination is not configured")
    payload = json.dumps({"chat_id": chat, "text": text}, ensure_ascii=False).encode("utf-8")
    request = urllib.request.Request(
        f"https://api.telegram.org/bot{token}/sendMessage", data=payload,
        headers={"Content-Type": "application/json"}, method="POST")
    with urllib.request.urlopen(request, timeout=20) as response:
        body = json.load(response)
    if body.get("ok") is not True:
        raise RuntimeError("Telegram did not confirm delivery")


def deliver_once(database, key, text, send=telegram_send):
    # SQLite serializes concurrent retries, including after API rolling deployments.
    with closing(sqlite3.connect(database, timeout=23)) as db:
        db.execute("CREATE TABLE IF NOT EXISTS deliveries (delivery_key TEXT PRIMARY KEY, sent_at INTEGER NOT NULL)")
        db.execute("BEGIN IMMEDIATE")
        if db.execute("SELECT 1 FROM deliveries WHERE delivery_key=?", (key,)).fetchone():
            return "already-sent"
        send(text)
        db.execute("INSERT INTO deliveries VALUES (?, ?)", (key, int(time.time())))
        db.commit()
        return "sent"


class Handler(BaseHTTPRequestHandler):
    def log_message(self, format, *args):
        # Do not log bodies, credentials, request URLs, or Telegram exceptions.
        pass

    def respond(self, status, payload):
        data = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        if self.path == "/health":
            configured = bool(env("RELAY_SECRET", "TELEGRAM_RELAY_SECRET") and
                              env("TELEGRAM_BOT_TOKEN", "BOT_TOKEN", "TELEGRAM_TOKEN") and
                              env("TELEGRAM_CHAT_ID", "CHAT_ID"))
            self.respond(200 if configured else 503, {"status": "healthy" if configured else "not-configured"})
        else:
            self.respond(404, {"error": "Not found"})

    def do_POST(self):
        secret = env("RELAY_SECRET", "TELEGRAM_RELAY_SECRET")
        supplied = self.headers.get("X-Relay-Secret", "")
        if not secret or not hmac.compare_digest(supplied.encode(), secret.encode()):
            self.respond(401, {"error": "Unauthorized"})
            return
        if self.path != "/order-reminder":
            self.respond(404, {"error": "Not found"})
            return
        try:
            length = int(self.headers.get("Content-Length", "0"))
            if not 0 < length <= 16384:
                self.respond(413, {"error": "Invalid request size"})
                return
            body = json.loads(self.rfile.read(length))
            key, text = body.get("deliveryKey"), body.get("text")
            if not isinstance(key, str) or not re.fullmatch(r"order:[0-9a-f]{32}:[1-4]", key):
                raise ValueError()
            if not isinstance(text, str) or not 0 < len(text.strip()) <= 4000:
                raise ValueError()
        except (ValueError, TypeError, AttributeError):
            self.respond(400, {"error": "Invalid reminder"})
            return
        try:
            database = os.environ.get("ORDER_RELAY_DB", "/var/lib/torsepan-order-relay/deliveries.sqlite3")
            self.respond(200, {"status": deliver_once(database, key, text)})
        except Exception:
            self.respond(502, {"error": "Reminder delivery failed; retry required"})


if __name__ == "__main__":
    os.umask(0o077)
    server = ThreadingHTTPServer(("127.0.0.1", int(os.environ.get("ORDER_RELAY_PORT", "5052"))), Handler)
    server.serve_forever()
