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

// Browsers unlock audio only after a real tap/key interaction. Keep it local to this device.
(() => {
  let audio;
  const sounded = new Map();
  async function unlock() {
    try {
      const Context = window.AudioContext || window.webkitAudioContext;
      if (!Context) return;
      audio ||= new Context();
      if (audio.state === 'suspended') await audio.resume();
    } catch { /* A muted device or blocked audio must never interrupt messaging. */ }
  }
  document.addEventListener('pointerdown', unlock, { passive: true });
  document.addEventListener('keydown', unlock, { passive: true });
  async function ding() {
    if (!audio) return false;
    if (audio.state === 'suspended') await audio.resume();
    if (audio.state !== 'running') return false;
    const now = audio.currentTime;
    for (const [frequency, offset] of [[880, 0], [1174.66, 0.12]]) {
      const tone = audio.createOscillator(), gain = audio.createGain();
      tone.type = 'sine'; tone.frequency.value = frequency;
      gain.gain.setValueAtTime(0, now + offset);
      gain.gain.linearRampToValueAtTime(0.12, now + offset + 0.012);
      gain.gain.exponentialRampToValueAtTime(0.001, now + offset + 0.25);
      tone.connect(gain); gain.connect(audio.destination);
      tone.onended = () => { tone.disconnect(); gain.disconnect(); };
      tone.start(now + offset); tone.stop(now + offset + 0.26);
    }
    return true;
  }
  window.workshopMessages.playIncoming = async (userId, total) => {
    if (!userId || !Number.isSafeInteger(total) || total < 1) return;
    const key = `workshop-sound:${userId}`;
    const play = async () => {
      let previous = sounded.get(key) || 0;
      try { previous = Math.max(previous, Number(localStorage.getItem(key)) || 0); } catch { }
      if (total <= previous) return;
      // Mark observed even if audio is unavailable; old messages must not chime on a later tap.
      sounded.set(key, total);
      try { localStorage.setItem(key, String(total)); } catch { }
      try { await ding(); } catch { }
    };
    try {
      if (navigator.locks) await navigator.locks.request(key, play);
      else await play();
    } catch { await play(); }
  };
})();
