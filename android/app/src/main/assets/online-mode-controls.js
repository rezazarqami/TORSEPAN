(() => {
  // Injected only by the Android shell. The server decides account/device eligibility.
  const device = window.torsepanOfflineDevice;
  if (!device || window.torsepanOfflineMenu) return;
  window.torsepanOfflineMenu = true;
  document.querySelectorAll('.app-mode-controls').forEach(node => node.remove());
  document.getElementById('app-mode-style')?.remove();
  let token = '', allowed = false, generation = 0;
  const render = () => {
    const existing = document.getElementById('offline-mode-menu');
    const menu = document.querySelector('.sidebar-menu');
    if (!allowed || !menu) { existing?.remove(); return; }
    if (existing) return;
    const link = document.createElement('a');
    link.id = 'offline-mode-menu'; link.href = 'torsepan-app://offline';
    const icon = document.createElement('span'); icon.textContent = '⇩'; icon.setAttribute('aria-hidden', 'true');
    const label = document.createElement('span'); label.textContent = 'حالت آفلاین / صف عملیات';
    link.append(icon, label);
    menu.insertBefore(link, menu.querySelector('.sidebar-logout'));
  };
  const refresh = async () => {
    const current = localStorage.getItem('access_token') || '';
    const version = ++generation;
    if (current !== token || !current) { allowed = false; token = current; render(); }
    if (!current) return;
    try {
      const response = await fetch('/api/internal/mobile-offline/access/' + encodeURIComponent(device), {
        headers: { Authorization: 'Bearer ' + current }, cache: 'no-store'
      });
      const access = response.ok ? await response.json() : { allowed: false };
      if (version !== generation || current !== (localStorage.getItem('access_token') || '')) return;
      allowed = access.allowed === true; render();
    } catch { if (version === generation) { allowed = false; render(); } }
  };
  new MutationObserver(() => {
    if (token !== (localStorage.getItem('access_token') || '')) refresh();
    else render();
  }).observe(document.body, { childList: true, subtree: true });
  window.addEventListener('storage', refresh);
  setInterval(refresh, 15000);
  refresh();
})();
