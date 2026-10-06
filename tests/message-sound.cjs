const vm = require('node:vm');
const fs = require('node:fs');
const assert = require('node:assert/strict');
const source = fs.readFileSync('TORSEPAN.Panel/wwwroot/workshop-messages.js', 'utf8');
function device(storage = new Map(), denied = false) {
  const events = {}, tones = [], contexts = [];
  class Context {
    constructor() { contexts.push(this); }
    state = 'suspended'; currentTime = 0; destination = {};
    async resume() { if (!denied) this.state = 'running'; }
    createOscillator() { const tone = { frequency: {}, connect(){}, disconnect(){}, start(t){ tones.push(t); }, stop(){} }; return tone; }
    createGain() { return { gain: { setValueAtTime(){}, linearRampToValueAtTime(){}, exponentialRampToValueAtTime(){} }, connect(){}, disconnect(){} }; }
  }
  const sandbox = { window: { AudioContext: Context }, document: { addEventListener(k,f){ events[k] = f; } }, navigator: { locks: { request: async (_,f) => f() } }, localStorage: { getItem(k){ return storage.get(k); }, setItem(k,v){ storage.set(k,v); } } };
  vm.runInNewContext(source, sandbox);
  return { events, tones, contexts, document: sandbox.document, play: sandbox.window.workshopMessages.playIncoming };
}
(async () => {
  const store = new Map(), first = device(store);
  await first.play('member-a', 1);
  assert.equal(first.tones.length, 0, 'sound is not forced before a real gesture');
  await first.events.pointerdown();
  await first.play('member-a', 1);
  assert.equal(first.tones.length, 0, 'old observed arrivals do not replay after unlock');
  await first.play('member-a', 2);
  assert.equal(first.tones.length, 2, 'one short two-note ding per new batch');
  await first.play('member-a', 2); await first.play('member-a', 1);
  assert.equal(first.tones.length, 2, 'repeated/stale polling does not replay');
  const second = device(store); await second.events.keydown(); await second.play('member-a', 2);
  assert.equal(second.tones.length, 0, 'another tab does not repeat the same batch');
  await second.play('member-b', 2);
  assert.equal(second.tones.length, 2, 'account boundaries are independent');
  await second.play('', 4); await second.play('member-b', NaN);
  assert.equal(second.tones.length, 2, 'malformed notifications are ignored');
  const muted = device(new Map(), true); await muted.events.pointerdown(); await muted.play('member', 1);
  assert.equal(muted.tones.length, 0, 'browser audio refusal is harmless');
  first.contexts[0].state = 'interrupted'; await first.play('member-a', 3);
  assert.equal(first.tones.length, 4, 'an interrupted Safari audio context is resumed for a new arrival');
  first.document.visibilityState = 'hidden'; await first.play('member-a', 4);
  assert.equal(first.tones.length, 4, 'a hidden tab must not claim the next sound');
  first.document.visibilityState = 'visible'; await first.play('member-a', 4);
  assert.equal(first.tones.length, 6, 'an active tab can still play an arrival first observed in the background');
  const locked = device(store); await locked.play('member-a', 5);
  await first.play('member-a', 5);
  assert.equal(first.tones.length, 8, 'a tab without audio permission cannot consume another tab\'s chime');
  const storageDenied = device({get(){throw new Error('storage denied')},set(){throw new Error('storage denied')}});
  await storageDenied.events.pointerdown(); await storageDenied.play('member', 1);
  assert.equal(storageDenied.tones.length, 2, 'storage restrictions do not silently disable sound');
  console.log('PASS message sound: gesture, new batch, deduplication, account boundaries, muted device');
})();
