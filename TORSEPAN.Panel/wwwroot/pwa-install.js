let deferredInstallPrompt = null;
window.addEventListener("beforeinstallprompt", event => { event.preventDefault(); deferredInstallPrompt = event; });
window.addEventListener("appinstalled", () => { deferredInstallPrompt = null; });

window.torsepanPwa = {
  install: async function () {
    if (window.matchMedia("(display-mode: standalone)").matches || window.navigator.standalone === true) {
      this.showMessage("TORSEPAN نصب شده است", "این سامانه همین حالا به‌صورت اپلیکیشن اجرا شده است.");
      return;
    }
    if (deferredInstallPrompt) {
      deferredInstallPrompt.prompt();
      await deferredInstallPrompt.userChoice;
      deferredInstallPrompt = null;
      return;
    }
    const ios = /iphone|ipad|ipod/i.test(navigator.userAgent);
    this.showMessage(ios ? "افزودن به صفحه اصلی در iPhone" : "افزودن به صفحه اصلی",
      ios ? "در Safari دکمه Share را بزنید، سپس Add to Home Screen و بعد Add را انتخاب کنید."
          : "منوی مرورگر را باز کنید و گزینه Install app یا Add to Home screen را بزنید.");
  },
  showMessage: function (title, message) {
    document.getElementById("torsepan-pwa-help")?.remove();
    const overlay = document.createElement("div");
    overlay.id = "torsepan-pwa-help";
    overlay.className = "pwa-help-overlay";
    overlay.innerHTML = `<section class="pwa-help-dialog" dir="rtl"><img src="/icons/torsepan-192.png" alt="TORSEPAN"><h3>${title}</h3><p>${message}</p><button type="button">متوجه شدم</button></section>`;
    overlay.addEventListener("click", event => { if (event.target === overlay || event.target.tagName === "BUTTON") overlay.remove(); });
    document.body.appendChild(overlay);
  }
};

if ("serviceWorker" in navigator) window.addEventListener("load", () => navigator.serviceWorker.register("/service-worker.js"));
