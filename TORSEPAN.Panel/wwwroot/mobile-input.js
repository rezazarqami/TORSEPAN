(() => {
  const root = document.documentElement;
  const editable = el => el instanceof HTMLElement && el.matches('textarea,[contenteditable="true"],input:not([type="button"]):not([type="submit"]):not([type="checkbox"]):not([type="radio"]):not([type="file"]):not([type="range"])');
  let frame;
  const update = () => {
    cancelAnimationFrame(frame);
    frame = requestAnimationFrame(() => {
      const active = document.activeElement;
      const mobile = matchMedia('(max-width: 900px)').matches;
      const viewport = window.visualViewport;
      // A native keyboard may shrink either the layout or just the visual viewport.
      // Move the fixed dock above the obscured region without ever hiding it.
      const inset = mobile && viewport && (viewport.scale ?? 1) === 1
        ? Math.max(0, innerHeight - viewport.offsetTop - viewport.height) : 0;
      root.style.setProperty('--tp-viewport-bottom', `${inset}px`);
      const focused = editable(active) && mobile;
      root.classList.toggle('mobile-input-active', focused);
      if (!focused) return;
      const rect = active.getBoundingClientRect();
      const top = viewport?.offsetTop || 0;
      const dockHeight = document.querySelector('.bottom-navigation')?.getBoundingClientRect().height || 0;
      const bottom = top + (viewport?.height || innerHeight) - dockHeight;
      if (rect.top < top + 20 || rect.bottom > bottom - 20)
        active.scrollIntoView({block:'center', inline:'nearest', behavior:'instant'});
    });
  };
  document.addEventListener('focusin', update);
  document.addEventListener('focusout', update);
  window.addEventListener('resize', update);
  window.addEventListener('pageshow', update);
  window.visualViewport?.addEventListener('resize', update);
  window.visualViewport?.addEventListener('scroll', update);
  update();
})();
