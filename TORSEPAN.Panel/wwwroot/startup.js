(() => {
    const splash = document.getElementById("torsepan-startup");
    if (!splash) return;
    let finished = false;
    const observer = new MutationObserver(checkReady);
    function checkReady() {
        if (finished || !document.querySelector("[data-torsepan-startup-ready]")) return;
        finished = true;
        observer.disconnect();
        // The marker is emitted only after authentication and the initial page data have settled.
        // Leave the splash in place until that render is ready to paint; never use a fixed delay.
        requestAnimationFrame(() => splash.remove());
    }
    observer.observe(document.body, { childList: true, subtree: true });
    checkReady();
})();
