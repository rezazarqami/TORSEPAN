(() => {
  const attach = () => {
    const host = document.querySelector('.topbar-left');
    const title = host?.querySelector('.topbar-title-block');
    if (!host || !title) return;
    const existing = host.querySelector('.app-mode-controls');
    if (existing) { if (existing.parentElement !== host) title.after(existing); return; }
    const controls = document.createElement('nav');
    controls.className = 'app-mode-controls';
    controls.setAttribute('aria-label', 'حالت کار برنامه');
    const online = document.createElement('span'); online.textContent = '● آنلاین';
    const offline = document.createElement('a'); offline.textContent = 'ثبت آفلاین / صف عملیات';
    offline.href = 'torsepan-app://offline';
    controls.append(online, offline); title.after(controls);
  };
  if (!document.getElementById('app-mode-style')) {
    const style = document.createElement('style'); style.id = 'app-mode-style';
    style.textContent = "\n/* Android mode controls belong to the left of the brand/title, within the existing header row. */\n.topbar-left:has(.app-mode-controls){min-width:0;gap:8px}\n.topbar-left .topbar-title-block:has(.app-mode-controls){display:contents}\n.topbar-left .page-title{flex-shrink:0}\n.topbar-left .app-mode-controls{display:flex;flex:0 0 auto;align-items:center;justify-content:center;gap:7px;margin:0 0 0 0;padding:0;border:0;font-size:10px;line-height:1.5;direction:rtl}\n.topbar-left .app-mode-controls span{color:#9be1c2;white-space:nowrap;font-size:10px}\n.topbar-left .app-mode-controls a{display:block;color:#e3eff8;text-decoration:none;border:1px solid #ffffff30;border-radius:9px;padding:5px 8px;background:#ffffff0d;line-height:1.45;text-align:center;font-size:10px}\n@media(max-width:768px){\n body .layout>.main>.topbar:has(.app-mode-controls) .topbar-left{grid-column:1/-1;grid-row:1;width:100%;min-width:0;gap:8px}\n body .layout>.main>.topbar:has(.app-mode-controls) .topbar-right{grid-column:1;grid-row:2}\n body .layout>.main>.topbar:has(.app-mode-controls) .mobile-menu-toggle{grid-column:2;grid-row:2}\n .topbar-left .app-mode-controls{flex-direction:column;gap:3px;width:105px;margin-inline-start:auto}\n .topbar-left .app-mode-controls span{font-size:9px;line-height:1.2}\n .topbar-left .app-mode-controls a{width:105px;max-width:100%;padding:4px 5px;font-size:9px;white-space:normal}\n .topbar-left .page-title-main{white-space:nowrap}\n}\n@media(max-width:350px){.topbar-left .app-mode-controls,.topbar-left .app-mode-controls a{width:94px}.topbar-left .app-mode-controls a{font-size:8.5px}}\n";
    document.head.append(style);
  }
  attach();
  if (!window.torsepanModeObserver) {
    window.torsepanModeObserver = new MutationObserver(attach);
    window.torsepanModeObserver.observe(document.body, {childList:true,subtree:true});
  }
})();
