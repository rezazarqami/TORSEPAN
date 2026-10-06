window.workshopMessages = {
  isVisible() { return document.visibilityState === 'visible'; },
  atBottom(el) { return !!el && el.scrollHeight - el.scrollTop - el.clientHeight < 72; },
  scrollBottom(el) { if (el) el.scrollTop = el.scrollHeight; },
  saveScroll(el) { if (el) el._workshopScroll = { height: el.scrollHeight, top: el.scrollTop }; },
  restoreScroll(el) { if (el?._workshopScroll) { el.scrollTop = el._workshopScroll.top + el.scrollHeight - el._workshopScroll.height; delete el._workshopScroll; } },
  visibleIncoming(el) {
    if (!el || document.visibilityState !== 'visible') return [];
    const box = el.getBoundingClientRect(), top = Math.max(0, box.top), bottom = Math.min(innerHeight, box.bottom);
    return [...el.querySelectorAll('[data-incoming-id]')].filter(node => {
      const rect = node.getBoundingClientRect();
      return rect.top < bottom && rect.bottom > top;
    }).map(node => node.dataset.incomingId);
  }
};

// Browsers unlock audio only after a real tap/key interaction. Keep it local to this device.
(() => {
  let audio;
  const sounded = new Map();
  const observed = new Map();
  async function unlock() {
    try {
      const Context = window.AudioContext || window.webkitAudioContext;
      if (!Context) return;
      audio ||= new Context();
      if (audio.state !== 'running') await audio.resume();
      // A short silent buffer, started inside the real gesture, unlocks audio on mobile Safari.
      if (audio.createBuffer && audio.createBufferSource) {
        const source = audio.createBufferSource(); source.buffer = audio.createBuffer(1, 1, 22050);
        source.connect(audio.destination); source.start(0);
      }
    } catch { /* A muted device or blocked audio must never interrupt messaging. */ }
  }
  document.addEventListener('pointerdown', unlock, { passive: true });
  document.addEventListener('keydown', unlock, { passive: true });
  document.addEventListener('touchend', unlock, { passive: true });
  document.addEventListener('click', unlock, { passive: true });
  async function ding() {
    if (!audio) return false;
    if (audio.state !== 'running') await audio.resume();
    if (audio.state !== 'running') return false;
    const now = audio.currentTime;
    for (const [frequency, offset] of [[880, 0], [1174.66, 0.12]]) {
      const tone = audio.createOscillator(), gain = audio.createGain();
      tone.type = 'sine'; tone.frequency.value = frequency;
      gain.gain.setValueAtTime(0, now + offset);
      gain.gain.linearRampToValueAtTime(0.12, now + offset + 0.012);
      gain.gain.exponentialRampToValueAtTime(0.001, now + offset + 0.25);
      tone.connect(gain); gain.connect(audio.destination);
      tone.onended = () => { tone.disconnect(); gain.disconnect(); };
      tone.start(now + offset); tone.stop(now + offset + 0.26);
    }
    return true;
  }
  window.workshopMessages.playIncoming = async (userId, total) => {
    if (!userId || !Number.isSafeInteger(total) || total < 1) return;
    // A background tab must not consume the active tab's sound; background alerts use Web Push.
    if (document.visibilityState === 'hidden') return;
    const key = `workshop-sound:${userId}`;
    if (total <= (observed.get(key) || 0)) return;
    observed.set(key, total);
    const play = async () => {
      let previous = sounded.get(key) || 0;
      try { previous = Math.max(previous, Number(localStorage.getItem(key)) || 0); } catch { }
      if (total <= previous) return;
      window.workshopMessages.showBanner?.();
      let enabled = true;
      try { enabled = localStorage.getItem('workshop-audio') !== 'off'; } catch { }
      try {
        if (enabled && await ding()) {
          sounded.set(key, total);
          try { localStorage.setItem(key, String(total)); } catch { }
        }
      } catch { }
    };
    try {
      if (navigator.locks) await navigator.locks.request(key, play);
      else await play();
    } catch { await play(); }
  };
  window.workshopMessages.testSound = async () => {
    await unlock();
    try { localStorage.setItem('workshop-audio', 'on'); } catch { }
    return await ding();
  };
})();

// These actions run directly in the browser click, not after a Blazor Server round trip.
(() => {
  let config, receiver;
  window.workshopMessages.showBanner = () => {
    if (document.visibilityState !== 'visible' || !document.body) return;
    document.getElementById('workshop-message-banner')?.remove();
    const link = document.createElement('a'); link.id = 'workshop-message-banner'; link.href = '/notifications';
    link.textContent = '✉ پیام جدید دریافت شد · مشاهده'; link.setAttribute('role', 'status'); link.setAttribute('dir', 'rtl');
    link.style.cssText = 'position:fixed;top:calc(env(safe-area-inset-top,0px) + 12px);left:16px;right:16px;max-width:420px;margin:auto;z-index:11000;padding:16px;background:#123d62;color:white;text-align:center;border-radius:14px;box-shadow:0 8px 28px #0005;text-decoration:none';
    document.body.appendChild(link); setTimeout(() => link.remove(), 6000);
  };
  const supported = () => window.isSecureContext && 'Notification' in window && 'serviceWorker' in navigator && 'PushManager' in window;
  const ios = () => /iPad|iPhone|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
  const standalone = () => window.matchMedia('(display-mode: standalone)').matches || navigator.standalone === true;
  async function registration() {
    await navigator.serviceWorker.register('/service-worker.js', { updateViaCache: 'none' });
    let timeout;
    try {
      return await Promise.race([navigator.serviceWorker.ready, new Promise((_, reject) => { timeout = setTimeout(() => reject(new Error('Service worker is not ready')), 8000); })]);
    } finally { clearTimeout(timeout); }
  }
  const serialize = sub => { const s = sub.toJSON(); return { endpoint: s.endpoint, p256dh: s.keys.p256dh, auth: s.keys.auth }; };
  const decode = value => Uint8Array.from(atob(value.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(value.length / 4) * 4, '=')), c => c.charCodeAt(0));
  async function report(message) {
    if (receiver) { try { await receiver.invokeMethodAsync('AlertStatus', message); } catch { } }
    return message;
  }
  window.workshopMessages.configureAlerts = async (settings, callback) => {
    config = settings; if (callback) receiver = callback;
    if (ios() && !standalone()) return 'در آیفون، برنامه را از آیکون صفحهٔ اصلی باز کنید؛ iOS 16.4 یا جدیدتر لازم است.';
    if (!supported()) return 'این مرورگر اعلان پس‌زمینه را پشتیبانی نمی‌کند؛ صدا در صفحهٔ باز قابل استفاده است.';
    if (Notification.permission === 'denied') return 'اجازهٔ اعلان بسته است؛ آن را از تنظیمات اعلان گوشی یا مرورگر فعال کنید.';
    if (Notification.permission !== 'granted') return 'برای دریافت اعلان، «فعال‌سازی اعلان این دستگاه» را لمس کنید.';
    const sub = await (await registration()).pushManager.getSubscription();
    if (!sub) return 'اجازهٔ اعلان داده شده؛ برای اتصال این دستگاه دکمهٔ فعال‌سازی را لمس کنید.';
    return { subscription: serialize(sub) };
  };
  window.workshopMessages.bindAlertSettings = callback => { receiver = callback; };
  window.workshopMessages.pushSubscription = async () => {
    if (!supported()) return null;
    const sub = await (await registration()).pushManager.getSubscription();
    return sub ? serialize(sub) : null;
  };
  window.workshopMessages.clearAlerts = async () => {
    config = undefined; receiver = undefined;
    if (!supported()) return;
    const reg = await registration(); const sub = await reg.pushManager.getSubscription();
    if (sub) await sub.unsubscribe();
    for (const notification of await reg.getNotifications()) notification.close();
  };
  document.addEventListener('click', async event => {
    const button = event.target.closest?.('[data-message-alerts-action]'); if (!button || button.disabled) return;
    const action = button.dataset.messageAlertsAction;
    if (action === 'sound') {
      try { await report(await workshopMessages.testSound() ? 'صدای تست پخش شد. اگر نمی‌شنوید، صدای رسانهٔ گوشی و حالت بی‌صدا را بررسی کنید.' : 'مرورگر صدا را فعال نکرد؛ دوباره لمس کنید و تنظیمات صدای گوشی را بررسی کنید.'); }
      catch { await report('پخش صدا در این مرورگر انجام نشد.'); } return;
    }
    if (action === 'mute') {
      try { localStorage.setItem('workshop-audio', 'off'); } catch { }
      await report('صدای داخل برنامه روی این دستگاه خاموش شد.'); return;
    }
    if (action !== 'push' || !config || !receiver) return;
    if (ios() && !standalone()) { await report('در آیفون، ابتدا برنامه را از آیکون صفحهٔ اصلی باز کنید.'); return; }
    if (!supported()) { await report('اعلان پس‌زمینه در این مرورگر پشتیبانی نمی‌شود.'); return; }
    // Request permission immediately in the trusted click, before any async work.
    const permission = Notification.permission === 'granted' ? Promise.resolve('granted') : Notification.requestPermission();
    button.disabled = true;
    try {
      if (await permission !== 'granted') { await report('اجازهٔ اعلان داده نشد؛ تنظیمات اعلان دستگاه را بررسی کنید.'); return; }
      const reg = await registration();
      const sub = await reg.pushManager.getSubscription() || await reg.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: decode(config.publicKey) });
      await receiver.invokeMethodAsync('SavePushSubscription', serialize(sub));
    } catch { await report('فعال‌سازی اعلان انجام نشد؛ اتصال را بررسی و دوباره تلاش کنید.'); }
    finally { button.disabled = false; }
  });
  navigator.serviceWorker?.addEventListener('message', event => {
    const data = event.data;
    if (data?.type === 'workshop-push' && config?.userId === data.userId)
      window.workshopMessages.playIncoming(data.userId, data.totalIncoming);
  });
})();
