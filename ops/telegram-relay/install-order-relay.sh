#!/usr/bin/env bash
set -euo pipefail

# Run on the existing ParsPack relay server from a reviewed checkout.
if [[ $(id -u) != 0 ]]; then
    echo 'Run this installer as root on the existing relay server.' >&2
    exit 1
fi
relay_source=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
existing_snippet=/etc/nginx/snippets/torsepan-relay.conf
test -f /etc/torsepan-telegram-relay.env
test -f "$existing_snippet"
python3 -m py_compile "$relay_source/order_reminder_relay.py"
nginx -t

if ! id torsepan-order-relay >/dev/null 2>&1; then
    useradd --system --no-create-home --shell /usr/sbin/nologin torsepan-order-relay
fi
install -d -m 0755 /opt/torsepan-order-relay
install -m 0644 "$relay_source/order_reminder_relay.py" /opt/torsepan-order-relay/order_reminder_relay.py
install -m 0644 "$relay_source/torsepan-order-relay.service" /etc/systemd/system/torsepan-order-relay.service
install -m 0644 "$relay_source/torsepan-order-relay.nginx.conf" /etc/nginx/snippets/torsepan-order-relay.conf
rollback_file="${existing_snippet}.pre-orders-$(date -u +%Y%m%dT%H%M%S)"
cp -a "$existing_snippet" "$rollback_file"
if ! grep -Fq 'include /etc/nginx/snippets/torsepan-order-relay.conf;' "$existing_snippet"; then
    printf '\ninclude /etc/nginx/snippets/torsepan-order-relay.conf;\n' >> "$existing_snippet"
fi
if ! nginx -t; then
    cp -a "$rollback_file" "$existing_snippet"
    exit 1
fi
systemctl daemon-reload
systemctl enable torsepan-order-relay
systemctl restart torsepan-order-relay
curl --fail --silent --show-error --retry 3 --retry-connrefused --retry-delay 1 http://127.0.0.1:5052/health
systemctl reload nginx
echo
echo 'Order reminder relay installed. Existing backup service was not restarted.'
