// Only recipient-scoped sequence numbers travel to the browser. Muting a chat suppresses its notification at creation.
const settings = document.querySelector('[data-sound-account]');
if (settings && window.AudioContext) startSounds(settings);

function startSounds(panel) {
  const prefix = 'riseup.sounds.' + panel.dataset.soundAccount + '.';
  const switches = [...panel.querySelectorAll('[data-tone]')];
  let audio, previous, checking = false, stopped = false;
  const read = key => { try { return localStorage.getItem(prefix + key); } catch { return null; } };
  const write = (key, value) => { try { localStorage.setItem(prefix + key, value); } catch {} };
  function sync() {
    for (const input of switches) input.checked = read(input.dataset.tone) !== 'off';
  }
  function unlock() {
    if (!switches.some(input => input.checked)) return;
    try {
      audio ??= new AudioContext();
      if (audio.state !== 'running') audio.resume().catch(() => {});
    } catch { /* The browser may disable audio. */ }
  }
  function chime(kind) {
    const tones = kind === 'message' ? [660, 880] : [523];
    tones.forEach((hz, i) => {
      const start = audio.currentTime + i * .09;
      const oscillator = audio.createOscillator(), gain = audio.createGain();
      oscillator.frequency.value = hz;
      gain.gain.setValueAtTime(.001, start);
      gain.gain.exponentialRampToValueAtTime(.09, start + .012);
      gain.gain.exponentialRampToValueAtTime(.001, start + .18);
      oscillator.connect(gain).connect(audio.destination);
      oscillator.start(start);
      oscillator.stop(start + .2);
      oscillator.onended = () => { oscillator.disconnect(); gain.disconnect(); };
    });
  }
  function announce(next, changed) {
    if (audio?.state !== 'running') return;
    const play = () => {
      for (const kind of changed) {
        if (read(kind) === 'off' || Number(read('last.' + kind)) >= next[kind]) continue;
        write('last.' + kind, next[kind]);
        chime(kind);
      }
    };
    if (navigator.locks) return navigator.locks.request(prefix + 'play', play);
    play();
  }
  async function check() {
    if (checking || stopped || (previous && audio?.state !== 'running') || !switches.some(input => input.checked)) return;
    checking = true;
    try {
      const res = await fetch('/notifications/latest', { cache: 'no-store', headers: { Accept: 'application/json' } });
      if (res.redirected || res.status === 401 || res.status === 403) { stopped = true; return; }
      if (!res.ok) return;
      const next = await res.json();
      if (next.owner !== panel.dataset.soundAccount) { stopped = true; return; }
      if (previous) await announce(next, ['message', 'alert'].filter(kind => next[kind] > previous[kind]));
      previous = next;
    } catch { /* A later poll retries network failures. */ }
    finally { checking = false; }
  }
  panel.hidden = false;
  sync();
  for (const input of switches) input.addEventListener('change', () => {
    write(input.dataset.tone, input.checked ? 'on' : 'off');
    previous = null;
    if (input.checked) { unlock(); check(); }
  });
  window.addEventListener('storage', event => { if (switches.some(input => event.key === prefix + input.dataset.tone)) { sync(); previous = null; check(); } });
  document.addEventListener('pointerup', unlock);
  document.addEventListener('keydown', unlock);
  document.addEventListener('riseup:check-alerts', check);
  check();
  setInterval(check, 6000);
}
