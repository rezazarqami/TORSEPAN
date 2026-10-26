(() => {
  const root = document.documentElement;
  const editable = el => el instanceof HTMLElement && el.matches('textarea,[contenteditable="true"],input:not([type="button"]):not([type="submit"]):not([type="checkbox"]):not([type="radio"]):not([type="file"]):not([type="range"])');
  let frame;
  const update = () => {
    cancelAnimationFrame(frame);
    frame = requestAnimationFrame(() => {
      const active = document.activeElement;
      const focused = editable(active) && matchMedia('(max-width: 900px)').matches;
      root.classList.toggle('mobile-input-active', focused);
      if (!focused) return;
      const rect = active.getBoundingClientRect();
      const viewport = window.visualViewport;
      const top = viewport?.offsetTop || 0;
      const bottom = top + (viewport?.height || innerHeight);
      if (rect.top < top + 20 || rect.bottom > bottom - 20)
        active.scrollIntoView({block:'center', inline:'nearest', behavior:'instant'});
    });
  };
  document.addEventListener('focusin', update);
  document.addEventListener('focusout', update);
  window.addEventListener('resize', update);
  window.visualViewport?.addEventListener('resize', update);
  window.visualViewport?.addEventListener('scroll', update);
})();
