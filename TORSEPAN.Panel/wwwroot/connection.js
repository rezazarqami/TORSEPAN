(() => {
    const box = document.getElementById("torsepan-connection");
    const message = document.getElementById("torsepan-connection-text");
    const reloadButton = document.getElementById("torsepan-connection-reload");
    let connected = true, stopped = false, inFlight = false;
    let timer, reveal, hiddenAt = 0, wakeVersion = 0, attempts = 0;
    function cancelTimers() { clearTimeout(timer); clearTimeout(reveal); }
    function hasPendingUpload() { return Boolean(window.handpanPhotos?.hasPending); }
    function clearRecovery() {
        try { sessionStorage.removeItem("torsepan-recovery"); } catch { }
    }
    function reloadOnce() {
        if (hasPendingUpload()) return;
        try {
            const key = "torsepan-recovery";
            const previous = JSON.parse(sessionStorage.getItem(key) || "null");
            const samePage = previous?.url === location.href;
            const recent = previous?.at && Date.now() - previous.at < 30000;
            const count = samePage && recent ? Number(previous.count || 0) : 0;
            if (count >= 2) return;
            sessionStorage.setItem(key, JSON.stringify({ url: location.href, at: Date.now(), count: count + 1 }));
            location.reload();
        } catch { /* Storage may be unavailable. Keep the explicit recovery button. */ }
    }
    function expired() {
        connected = false; stopped = true; cancelTimers();
        box.hidden = false; reloadButton.hidden = false;
        message.textContent = "ارتباط صفحه منقضی شده است. برای ادامه صفحه را دوباره باز کنید.";
        reloadOnce();
    }
    function up() {
        connected = true; stopped = false; attempts = 0; cancelTimers();
        box.hidden = true; reloadButton.hidden = true;
        clearRecovery();
        window.dispatchEvent(new Event("torsepan-connected"));
    }
    function schedule(delay) {
        clearTimeout(timer);
        if (!connected && !stopped && !document.hidden) timer = setTimeout(attempt, delay);
    }
    async function attempt() {
        if (connected || stopped || inFlight || document.hidden) return;
        if (!navigator.onLine) { message.textContent = "اینترنت قطع است؛ پس از اتصال خودکار ادامه می‌دهیم."; box.hidden = false; return; }
        inFlight = true;
        try {
            const ok = await Blazor.reconnect();
            if (ok) { up(); return; }
            expired();
        } catch {
            attempts++;
            message.textContent = "اتصال موقتاً قطع است؛ در حال تلاش مجدد…";
            if (attempts >= 2) box.hidden = false;
            if (attempts >= 3) { expired(); return; }
        } finally {
            inFlight = false;
            schedule(Math.min(1000 * Math.max(attempts, 1), 5000));
        }
    }
    function down() {
        connected = false; attempts = 0; stopped = false;
        reveal = setTimeout(() => { if (!connected && !document.hidden) box.hidden = false; }, 2500);
        schedule(0);
    }
    async function probe(version, retry = false) {
        if (version !== wakeVersion || document.hidden || !connected) return;
        try {
            await Promise.race([
                DotNet.invokeMethodAsync("TORSEPAN.Panel", "ConnectionProbe"),
                new Promise((_, reject) => setTimeout(() => reject(new Error("probe timeout")), 5000))
            ]);
            if (version === wakeVersion) up();
        } catch {
            if (version !== wakeVersion || document.hidden) return;
            if (!retry) { setTimeout(() => void probe(version, true), 700); return; }
            expired();
        }
    }
    async function wake() {
        if (document.hidden) return;
        const version = ++wakeVersion;
        if (!connected) { schedule(0); return; }
        if (hiddenAt && Date.now() - hiddenAt >= 2000) setTimeout(() => void probe(version), 250);
    }
    document.addEventListener("visibilitychange", () => {
        if (document.hidden) { hiddenAt = Date.now(); wakeVersion++; cancelTimers(); }
        else void wake();
    });
    window.addEventListener("pageshow", event => { if (event.persisted) reloadOnce(); else void wake(); });
    window.addEventListener("focus", () => void wake());
    window.addEventListener("online", () => void wake());
    document.getElementById("torsepan-connection-retry").addEventListener("click", () => location.reload());
    reloadButton.addEventListener("click", () => location.reload());
    window.torsepanConnection = { get connected() { return connected; } };
    Blazor.start({ circuit: {
        configureSignalR: builder => builder.withServerTimeout(120000).withKeepAliveInterval(15000),
        reconnectionHandler: { onConnectionDown: down, onConnectionUp: up }
    }}).then(clearRecovery).catch(() => { box.hidden = false; reloadButton.hidden = false; message.textContent = "راه‌اندازی ارتباط انجام نشد. دوباره تلاش کنید."; });
})();
