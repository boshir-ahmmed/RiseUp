// Unread badges work even when sound is off or the browser blocks audio.
const settings = document.querySelector('[data-sound-account]');
if (settings) startInbox(settings);

function startInbox(panel) {
  const prefix = 'riseup.sounds.' + panel.dataset.soundAccount + '.';
  const switches = [...panel.querySelectorAll('[data-tone]')];
  let audio, previous, checking = false, pending = false, stopped = false;
  const read = key => { try { return localStorage.getItem(prefix + key); } catch { return null; } };
  const write = (key, value) => { try { localStorage.setItem(prefix + key, value); } catch {} };
  function sync() {
    for (const input of switches) input.checked = read(input.dataset.tone) !== 'off';
  }
  function unlock() {
    if (!window.AudioContext || !switches.some(input => input.checked)) return;
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
  function updateNav(path, unread) {
    for (const link of document.querySelectorAll('.tb-nav a[href="' + path + '"], .side-nav a[href="' + path + '"]')) {
      const top = !!link.closest('.tb-nav');
      const label = link.querySelector(top ? '.lbl' : 'span').textContent;
      link.setAttribute('aria-label', unread ? label + ', ' + unread + ' unread' : label);
      let badge = link.querySelector(top ? '.dot' : '.cnt');
      if (!unread) { badge?.remove(); continue; }
      if (!badge) {
        badge = document.createElement('span');
        badge.className = top ? 'dot' : 'cnt';
        link.append(badge);
      }
      badge.textContent = top && unread > 9 ? '9+' : String(unread);
    }
  }
  function updateBadges(next) {
    updateNav('/conversations', next.unreadMessages);
    updateNav('/notifications', next.unreadAlerts);
    for (const room of document.querySelectorAll('[data-chat-id]')) {
      const count = next.conversations[room.dataset.chatId] || 0;
      const badge = room.querySelector('[data-chat-unread]');
      badge.hidden = !count;
      badge.textContent = String(count);
      badge.setAttribute('aria-label', count + ' unread');
    }
  }
  async function check() {
    if (stopped || (document.hidden && audio?.state !== 'running')) return;
    if (checking) { pending = true; return; }
    checking = true;
    try {
      const res = await fetch('/notifications/latest', { cache: 'no-store', headers: { Accept: 'application/json' } });
      if (res.redirected || res.status === 401 || res.status === 403) { stopped = true; return; }
      if (!res.ok) return;
      const next = await res.json();
      if (next.owner !== panel.dataset.soundAccount) { stopped = true; return; }
      updateBadges(next);
      if (previous) await announce(next, ['message', 'alert'].filter(kind => next[kind] > previous[kind]));
      previous = next;
    } catch { /* A later poll retries network failures. */ }
    finally { checking = false; if (pending) { pending = false; check(); } }
  }
  panel.hidden = !window.AudioContext;
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
  document.addEventListener('visibilitychange', () => { if (!document.hidden) check(); });
  check();
  setInterval(check, 6000);
}
