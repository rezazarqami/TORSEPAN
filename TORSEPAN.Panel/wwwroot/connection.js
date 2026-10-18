(() => {
    const box = document.getElementById("torsepan-connection");
    const message = document.getElementById("torsepan-connection-text");
    const reloadButton = document.getElementById("torsepan-connection-reload");
    let connected = true, stopped = false, inFlight = false, probing = false, started = false;
    let timer, reveal, wakeTimer, healthyTimer, hiddenAt = null, wakeVersion = 0, attempts = 0;
    function cancelTimers() { clearTimeout(timer); clearTimeout(reveal); clearTimeout(wakeTimer); clearTimeout(healthyTimer); }
    function hasPendingUpload() { return Boolean(window.handpanPhotos?.hasPending); }
    function clearRecovery() {
        try { sessionStorage.removeItem("torsepan-recovery"); } catch { }
    }
    function bounded(operation, milliseconds) {
        let deadline;
        return Promise.race([
            Promise.resolve().then(operation),
            new Promise((_, reject) => { deadline = setTimeout(() => reject(new Error("connection timeout")), milliseconds); })
        ]).finally(() => clearTimeout(deadline));
    }
    function reloadOnce() {
        if (document.hidden || !navigator.onLine || hasPendingUpload()) return;
        try {
            const key = "torsepan-recovery";
            const previous = JSON.parse(sessionStorage.getItem(key) || "null");
            const samePage = previous?.url === location.href;
            const recent = previous?.at && Date.now() - previous.at < 30000;
            const count = samePage && recent ? Number(previous.count || 0) : 0;
            if (count >= 2) return;
            sessionStorage.setItem(key, JSON.stringify({ url: location.href, at: Date.now(), count: count + 1 }));
            location.reload();
        } catch { /* The explicit recovery button remains usable without storage. */ }
    }
    function expired() {
        connected = false; stopped = true; cancelTimers();
        box.hidden = false; reloadButton.hidden = false;
        message.textContent = hasPendingUpload()
            ? "اتصال صفحه نیاز به بازیابی دارد؛ تا پایان دریافت عکس، صفحه را باز نگه دارید."
            : "در حال بازیابی صفحه برای ادامهٔ کار…";
        reloadOnce();
    }
    function up() {
        connected = true; stopped = false; attempts = 0; cancelTimers();
        box.hidden = true; reloadButton.hidden = true;
        // A newly started socket alone must not reset the reload-loop limit.
        healthyTimer = setTimeout(clearRecovery, 30000);
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
        const version = wakeVersion;
        try {
            let ok = await bounded(() => Blazor.reconnect(), 8000);
            if (!ok && typeof Blazor.resumeCircuit === "function") ok = await bounded(() => Blazor.resumeCircuit(), 5000);
            if (version !== wakeVersion || document.hidden) return;
            if (ok) { up(); return; }
            expired();
        } catch (error) {
            if (version !== wakeVersion || document.hidden) return;
            if (error.message === "connection timeout") { expired(); return; }
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
        connected = false; stopped = false;
        clearTimeout(healthyTimer); clearTimeout(reveal);
        reveal = setTimeout(() => { if (!connected && !document.hidden) box.hidden = false; }, 2500);
        schedule(0);
    }
    async function probe(version, retry = false) {
        if (version !== wakeVersion || document.hidden || !connected || probing || !started) return;
        if (!navigator.onLine) { down(); return; }
        probing = true;
        let failed = false;
        try {
            await bounded(() => DotNet.invokeMethodAsync("TORSEPAN.Panel", "ConnectionProbe"), 2500);
            if (version === wakeVersion && !document.hidden && connected) up();
        } catch { failed = true; }
        finally { probing = false; }
        if (version !== wakeVersion || document.hidden) { if (!document.hidden) wake(); return; }
        if (!connected) { schedule(0); return; }
        if (failed) {
            if (!retry) { wakeTimer = setTimeout(() => void probe(version, true), 350); return; }
            // A socket can still look connected after Android suspends it. A failed
            // round trip, rather than the socket flag, decides whether it is usable.
            expired();
        }
    }
    function wake() {
        if (document.hidden || !started) return;
        clearTimeout(wakeTimer);
        if (document.getElementById("torsepan-startup")) { wakeTimer = setTimeout(wake, 500); return; }
        if (!connected) { stopped = false; attempts = 0; schedule(0); return; }
        wakeTimer = setTimeout(() => void probe(wakeVersion), 200);
    }
    function background() {
        hiddenAt = Date.now(); wakeVersion++; cancelTimers();
    }
    document.addEventListener("visibilitychange", () => { if (document.hidden) background(); else wake(); });
    document.addEventListener("freeze", background);
    document.addEventListener("resume", wake);
    window.addEventListener("pagehide", background);
    window.addEventListener("pageshow", event => { if (event.persisted || hiddenAt !== null) wake(); });
    window.addEventListener("focus", wake);
    window.addEventListener("online", wake);
    document.getElementById("torsepan-connection-retry").addEventListener("click", () => { stopped = false; connected = false; attempts = 0; schedule(0); });
    reloadButton.addEventListener("click", () => location.reload());
    window.torsepanConnection = { get connected() { return connected; }, wake };
    Blazor.start({ circuit: {
        configureSignalR: builder => builder.withServerTimeout(30000).withKeepAliveInterval(15000),
        reconnectionHandler: { onConnectionDown: down, onConnectionUp: up }
    }}).then(() => { started = true; if (hiddenAt !== null) wake(); }).catch(() => {
        box.hidden = false; reloadButton.hidden = false; message.textContent = "راه‌اندازی ارتباط انجام نشد. دوباره تلاش کنید.";
    });
})();
