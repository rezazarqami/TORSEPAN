(() => {
    const box = document.getElementById("torsepan-connection");
    const message = document.getElementById("torsepan-connection-text");
    const reloadButton = document.getElementById("torsepan-connection-reload");
    let connected = true, stopped = false, inFlight = false, paused = false;
    let timer, reveal, pauseTask, hiddenAt = 0, wakeVersion = 0, attempts = 0;
    function cancelTimers() { clearTimeout(timer); clearTimeout(reveal); }
    function hasUnsavedWork() {
        const hasDraft = Array.from(document.querySelectorAll("input:not([type=hidden]),textarea"))
            .some(el => el.type === "checkbox" || el.type === "radio" ? el.checked : Boolean(el.value));
        return hasDraft || Boolean(window.handpanPhotos?.hasPending);
    }
    function reloadOnce() {
        if (hasUnsavedWork()) return;
        try {
            const last = Number(sessionStorage.getItem("torsepan-recovery-at") || 0);
            if (Date.now() - last > 60000) {
                sessionStorage.setItem("torsepan-recovery-at", String(Date.now()));
                location.reload();
            }
        } catch { /* Storage may be unavailable. Keep the explicit recovery button. */ }
    }
    function expired() {
        connected = false; stopped = true; paused = false; cancelTimers();
        box.hidden = false; reloadButton.hidden = false;
        message.textContent = "ارتباط صفحه منقضی شده است. برای ادامه صفحه را دوباره باز کنید.";
        reloadOnce();
    }
    function up() {
        connected = true; stopped = false; paused = false; attempts = 0; cancelTimers();
        box.hidden = true; reloadButton.hidden = true;
        window.dispatchEvent(new Event("torsepan-connected"));
    }
    function schedule(delay) {
        clearTimeout(timer);
        if (!connected && !stopped && !paused && !document.hidden) timer = setTimeout(attempt, delay);
    }
    async function attempt() {
        if (connected || stopped || paused || inFlight || document.hidden) return;
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
        } finally {
            inFlight = false;
            schedule(Math.min(1000 * Math.max(attempts, 1), 5000));
        }
    }
    function down(_options, _error, isGracefulPause) {
        connected = false; attempts = 0; stopped = false;
        if (isGracefulPause) { paused = true; cancelTimers(); return; }
        paused = false;
        reveal = setTimeout(() => { if (!connected && !document.hidden) box.hidden = false; }, 2500);
        schedule(0);
    }
    async function pauseForBackground() {
        hiddenAt = Date.now(); wakeVersion++;
        if (!connected || stopped || hasUnsavedWork() || typeof Blazor.pauseCircuit !== "function") return;
        try {
            pauseTask = Blazor.pauseCircuit();
            paused = Boolean(await pauseTask);
        } catch { paused = false; }
        finally { pauseTask = null; if (!document.hidden) void wake(); }
    }
    async function probe(version, retry = false) {
        if (version !== wakeVersion || document.hidden || paused || pauseTask || !connected) return;
        try {
            await Promise.race([
                DotNet.invokeMethodAsync("TORSEPAN.Panel", "ConnectionProbe"),
                new Promise((_, reject) => setTimeout(() => reject(new Error("probe timeout")), 5000))
            ]);
            if (version === wakeVersion) up();
        } catch {
            if (version !== wakeVersion || document.hidden) return;
            if (!retry) { setTimeout(() => void probe(version, true), 1200); return; }
            expired();
        }
    }
    async function wake() {
        if (document.hidden) return;
        const version = ++wakeVersion;
        if (pauseTask) { try { await pauseTask; } catch { /* handled by pause */ } }
        if (paused && typeof Blazor.resumeCircuit === "function") {
            if (inFlight) return;
            inFlight = true;
            try {
                const ok = await Blazor.resumeCircuit();
                if (ok) up(); else expired();
            } catch { expired(); }
            finally { inFlight = false; }
            return;
        }
        if (!connected) { schedule(0); return; }
        if (hiddenAt && Date.now() - hiddenAt >= 3000) setTimeout(() => void probe(version), 350);
    }
    document.addEventListener("visibilitychange", () => {
        if (document.hidden) void pauseForBackground(); else void wake();
    });
    window.addEventListener("pageshow", () => void wake());
    window.addEventListener("online", () => void wake());
    document.getElementById("torsepan-connection-retry").addEventListener("click", () => { stopped = false; schedule(0); });
    reloadButton.addEventListener("click", () => location.reload());
    window.torsepanConnection = { get connected() { return connected; } };
    Blazor.start({ circuit: {
        configureSignalR: builder => builder.withServerTimeout(120000).withKeepAliveInterval(15000),
        reconnectionHandler: { onConnectionDown: down, onConnectionUp: up }
    }}).catch(() => { box.hidden = false; reloadButton.hidden = false; message.textContent = "راه‌اندازی ارتباط انجام نشد. دوباره تلاش کنید."; });
})();
