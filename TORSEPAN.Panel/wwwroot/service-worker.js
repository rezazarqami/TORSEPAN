const CACHE = "torsepan-static-v6";
const STATIC_ASSETS = new Set([
  "/images/brand/torsepan-splash.png",
  "/icons/torsepan-logo-v3-192.png",
  "/icons/torsepan-logo-v3-512.png",
  "/icons/apple-touch-icon-v3.png"
]);

self.addEventListener("install", event => {
  event.waitUntil(
    caches.open(CACHE)
      .then(cache => cache.addAll([...STATIC_ASSETS]))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener("activate", event => {
  event.waitUntil(
    caches.keys()
      .then(keys => Promise.all(keys.filter(key => key !== CACHE).map(key => caches.delete(key))))
      .then(() => self.clients.claim())
  );
});

self.addEventListener("fetch", event => {
  if (event.request.method !== "GET") return;

  const url = new URL(event.request.url);
  if (url.origin !== self.location.origin || !STATIC_ASSETS.has(url.pathname)) return;

  event.respondWith(
    caches.match(event.request).then(cached => cached || fetch(event.request))
  );
});

self.addEventListener("push", event => {
  event.waitUntil((async () => {
    let data = {};
    try { data = event.data?.json() || {}; } catch { }
    await self.registration.showNotification("تورسپن · پیام جدید", {
      body: "پیام جدیدی در کارگاه دارید. برای مشاهده لمس کنید.",
      icon: "/icons/torsepan-logo-v3-192.png", badge: "/icons/torsepan-logo-v3-192.png",
      tag: typeof data.tag === "string" ? data.tag : "workshop-message",
      data: { url: "/notifications", userId: data.userId },
    });
    for (const client of await self.clients.matchAll({ type: "window", includeUncontrolled: true }))
      client.postMessage({ type: "workshop-push", userId: data.userId, totalIncoming: data.totalIncoming });
  })());
});
self.addEventListener("notificationclick", event => {
  event.notification.close();
  event.waitUntil((async () => {
    const url = new URL("/notifications", self.location.origin).href;
    const clients = await self.clients.matchAll({ type: "window", includeUncontrolled: true });
    for (const client of clients) {
      if (new URL(client.url).origin === self.location.origin) {
        await client.navigate(url); await client.focus(); return;
      }
    }
    await self.clients.openWindow(url);
  })());
});
