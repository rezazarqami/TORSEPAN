(() => {
  const attach = () => {
    const title = document.querySelector('.topbar-title-block');
    if (!title || title.querySelector('.app-mode-controls')) return;
    const controls = document.createElement('nav');
    controls.className = 'app-mode-controls';
    controls.setAttribute('aria-label', 'حالت کار برنامه');
    const online = document.createElement('span'); online.textContent = '● آنلاین';
    const offline = document.createElement('a'); offline.textContent = 'ثبت آفلاین / صف عملیات';
    offline.href = 'torsepan-app://offline';
    controls.append(online, offline); title.append(controls);
  };
  if (!document.getElementById('app-mode-style')) {
    const style = document.createElement('style'); style.id = 'app-mode-style';
    style.textContent = '.app-mode-controls{display:flex;align-items:center;gap:7px;padding-top:6px;margin-top:6px;border-top:1px solid #ffffff26;font-size:10px;line-height:1.6;direction:rtl}.app-mode-controls span{color:#9be1c2;white-space:nowrap}.app-mode-controls a{color:#e3eff8;text-decoration:none;border:1px solid #ffffff30;border-radius:7px;padding:3px 7px;background:#ffffff0d}';
    document.head.append(style);
  }
  attach();
  if (!window.torsepanModeObserver) {
    window.torsepanModeObserver = new MutationObserver(attach);
    window.torsepanModeObserver.observe(document.body, {childList:true,subtree:true});
  }
})();
