(() => {
    const splash = document.getElementById("torsepan-startup");
    if (!splash) return;
    let finished = false;
    const observer = new MutationObserver(checkReady);
    function checkReady() {
        if (finished || !document.querySelector("[data-torsepan-startup-ready]")) return;
        finished = true;
        observer.disconnect();
        // The marker means authenticated navigation and the page's usable shell
        // are ready. Dashboard statistics load in their own regions afterwards.
        requestAnimationFrame(() => splash.remove());
    }
    observer.observe(document.body, { childList: true, subtree: true });
    checkReady();
})();
