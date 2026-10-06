window.workshopMessages = {
  isVisible() { return document.visibilityState === 'visible'; },
  atBottom(el) { return !!el && el.scrollHeight - el.scrollTop - el.clientHeight < 72; },
  scrollBottom(el) { if (el) el.scrollTop = el.scrollHeight; },
  saveScroll(el) { if (el) el._workshopScroll = { height: el.scrollHeight, top: el.scrollTop }; },
  restoreScroll(el) { if (el?._workshopScroll) { el.scrollTop = el._workshopScroll.top + el.scrollHeight - el._workshopScroll.height; delete el._workshopScroll; } },
  visibleIncoming(el) {
    if (!el || document.visibilityState !== 'visible') return [];
    const box = el.getBoundingClientRect(), top = Math.max(0, box.top), bottom = Math.min(innerHeight, box.bottom);
    return [...el.querySelectorAll('[data-incoming-id]')].filter(node => {
      const rect = node.getBoundingClientRect();
      return rect.top < bottom && rect.bottom > top;
    }).map(node => node.dataset.incomingId);
  }
};
