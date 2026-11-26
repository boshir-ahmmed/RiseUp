const root = document.querySelector('[data-conversation]');
if (root && document.getElementById('msgs')) startChat(root);

function startChat(root) {
  const id = Number(root.dataset.conversation);
  const list = root.querySelector('#msgs');
  const form = root.querySelector('.chat-in');
  const input = form?.elements.body;
  const file = form?.elements.file;
  const status = root.querySelector('[data-live-status]');
  const jump = root.querySelector('[data-show-latest]');
  const typing = root.querySelector('[data-typing]');
  const historical = new URLSearchParams(location.search).has('before');
  const connection = window.signalR && new signalR.HubConnectionBuilder().withUrl('/hubs/chat').withAutomaticReconnect().build();
  const directoryToggle = root.querySelector('[data-chat-directory]');
  directoryToggle.hidden = false;
  directoryToggle.addEventListener('click', () => {
    const open = directoryToggle.getAttribute('aria-expanded') === 'true';
    directoryToggle.setAttribute('aria-expanded', String(!open));
  });
  const typists = new Map();
  let reading = false, pending = false, sending = false, live = false, lastPulse = 0, idleTimer;
  function nearBottom() { return list.scrollHeight - list.scrollTop - list.clientHeight < 72; }
  function bottom() { list.scrollTop = list.scrollHeight; jump.hidden = true; }

  let renderedThrough = 0, acknowledged = 0, acknowledging = false;
  async function acknowledgeVisible() {
    if (acknowledging || historical || document.hidden || !nearBottom() || renderedThrough <= acknowledged) return;
    const throughId = renderedThrough;
    acknowledging = true;
    try {
      const res = await fetch('/conversations/' + id + '/read', {
        method: 'POST', body: new URLSearchParams({ throughId }),
        headers: { 'X-CSRF-TOKEN': document.querySelector('meta[name="csrf-token"]').content }
      });
      if (!res.ok || res.redirected) return;
      acknowledged = throughId;
      document.dispatchEvent(new Event('riseup:check-alerts'));
    } catch { /* Retry when the conversation refreshes. */ }
    finally { acknowledging = false; }
    if (renderedThrough > throughId) acknowledgeVisible();
  }

  // Leave a message being edited in place as new messages arrive.
  function render(doc) {
    const existing = new Map([...list.querySelectorAll('[data-msg]')].map(row => [row.dataset.msg, row]));
    const incoming = [...doc.body.querySelectorAll('[data-msg]')];
    const oldLastId = list.lastElementChild?.dataset.msg;
    const hasNew = incoming.at(-1)?.dataset.msg !== oldLastId;
    function unchanged(row, incomingRow) {
      return row && row.textContent === incomingRow.textContent &&
        row.querySelector('img')?.getAttribute('src') === incomingRow.querySelector('img')?.getAttribute('src');
    }
    const same = incoming.length === existing.size && incoming.every(row => unchanged(existing.get(row.dataset.msg), row));
    if (same && list.querySelector('.history-link')?.getAttribute('href') === doc.querySelector('.history-link')?.getAttribute('href')) return;
    const stick = nearBottom();
    const anchor = [...existing.values()].find(row => row.getBoundingClientRect().bottom > list.getBoundingClientRect().top);
    const offset = anchor?.getBoundingClientRect().top;
    const focused = document.activeElement;
    const fragment = document.createDocumentFragment();
    for (const node of doc.body.children) {
      const row = existing.get(node.dataset.msg);
      const editing = row?.querySelector('details[open]') && node.querySelector('details');
      fragment.appendChild(editing || unchanged(row, node) ? row : node.cloneNode(true));
    }
    list.replaceChildren(fragment);
    if (focused && list.contains(focused)) focused.focus({ preventScroll: true });
    if (stick) bottom();
    else {
      const restored = anchor && list.querySelector('[data-msg="' + anchor.dataset.msg + '"]');
      if (restored) list.scrollTop += restored.getBoundingClientRect().top - offset;
      if (hasNew) jump.hidden = false;
    }
    const latest = list.querySelector('.msg:not(.sys):last-of-type');
    const preview = root.querySelector('.chat-item.on [data-preview]');
    if (latest && preview) preview.textContent = latest.classList.contains('del') ? 'Message deleted' :
      [latest.querySelector('.meta strong')?.textContent, (latest.querySelector('.msg-body') || latest.querySelector('.chat-attachment'))?.textContent.trim()].filter(Boolean).join(': ');
  }
  async function refresh() {
    if (historical) { jump.hidden = false; return; }
    if (reading) { pending = true; return; }
    reading = true;
    try {
      do {
        pending = false;
        const res = await fetch('/conversations/' + id + '/messages', { cache: 'no-store' });
        if (res.redirected || res.status === 403 || res.status === 401) { location.replace('/conversations'); return; }
        if (!res.ok) throw new Error();
        render(new DOMParser().parseFromString(await res.text(), 'text/html'));
        renderedThrough = Math.max(renderedThrough, Number(res.headers.get('X-Chat-Notice')) || 0);
        acknowledgeVisible();
        status.textContent = live ? 'Live' : 'Checking periodically';
      } while (pending);
    } catch { status.textContent = 'Waiting for updates'; }
    finally { reading = false; }
  }
  jump.addEventListener('click', e => { if (!historical) { e.preventDefault(); bottom(); refresh(); } });
  list.addEventListener('scroll', () => { if (!historical && nearBottom()) { jump.hidden = true; acknowledgeVisible(); } });

  function showTypists() {
    const names = [...typists.values()].map(x => x.name);
    typing.hidden = names.length === 0;
    typing.querySelector('[data-typing-text]').textContent = names.length === 1 ? names[0] + ' is typing…' :
      names.length > 1 ? names.join(', ') + ' are typing…' : '';
  }
  function pulse(active) {
    if (!live || (!active && !lastPulse)) return;
    lastPulse = active ? Date.now() : 0;
    connection.invoke('Typing', id, active).catch(() => {});
  }
  input?.addEventListener('input', () => {
    clearTimeout(idleTimer);
    if (!input.value.trim()) return pulse(false);
    if (Date.now() - lastPulse > 1400) pulse(true);
    idleTimer = setTimeout(() => pulse(false), 1700);
  });
  input?.addEventListener('blur', () => pulse(false));
  input?.addEventListener('keydown', e => {
    if (e.key === 'Enter' && !e.shiftKey && !e.isComposing && e.keyCode !== 229) {
      e.preventDefault();
      if (!sending) form.requestSubmit();
    }
  });
  if (connection) {
    connection.on('typing', (chat, key, name, active) => {
      if (chat !== id || historical) return;
      clearTimeout(typists.get(key)?.timer);
      typists.delete(key);
      if (active) typists.set(key, { name, timer: setTimeout(() => { typists.delete(key); showTypists(); }, 3600) });
      showTypists();
    });
    connection.on('changed', chat => {
      document.dispatchEvent(new Event('riseup:check-alerts'));
      if (chat === id) refresh();
      else {
        const preview = root.querySelector('[data-chat-id="' + Number(chat) + '"] [data-preview]');
        if (preview) preview.textContent = 'New activity';
      }
    });
    async function join() {
      await connection.invoke('Join', id);
      live = true;
      status.textContent = 'Live';
      if (!historical) refresh();
      for (const item of root.querySelectorAll('[data-chat-id]')) {
        const other = Number(item.dataset.chatId);
        if (other !== id) await connection.invoke('Join', other).catch(() => {});
      }
    }
    function offline() { live = false; status.textContent = 'Reconnecting…'; lastPulse = 0; typists.clear(); showTypists(); }
    async function connect() {
      try { await connection.start(); await join(); }
      catch { offline(); if (connection.state === 'Connected') await connection.stop(); }
    }
    connection.onreconnecting(offline);
    connection.onreconnected(() => join().catch(() => connection.stop()));
    connection.onclose(offline);
    connect();
    setInterval(() => { if (!document.hidden && connection.state === 'Disconnected') connect(); }, 10000);
  }
  if (!historical) bottom();
  setInterval(() => { if (!document.hidden && !historical && (!live || status.textContent === 'Waiting for updates')) refresh(); }, 8000);
  setInterval(() => { if (!document.hidden && !historical && live) refresh(); }, 60000);
  document.addEventListener('visibilitychange', () => { if (!document.hidden && !historical) refresh(); });

  if (!form) return;
  const note = form.querySelector('[data-send-status]');
  const clear = form.querySelector('[data-clear-file]');
  const filename = form.querySelector('[data-file-name]');
  form.querySelector('[data-attach-label]').hidden = false;
  function showFile() {
    const name = file.files[0]?.name || '';
    filename.textContent = name;
    filename.hidden = clear.hidden = !name;
  }
  file.addEventListener('change', () => { showFile(); note.textContent = ''; });
  clear.addEventListener('click', () => { file.value = ''; showFile(); note.textContent = ''; file.focus(); });
  form.addEventListener('submit', async e => {
    e.preventDefault();
    if (sending) return;
    if (!input.value.trim() && !file.files.length) { note.textContent = 'Write a message or choose a file.'; input.focus(); return; }
    if (file.files[0]?.size > 10 * 1024 * 1024) { note.textContent = 'Choose a file up to 10 MB.'; return; }
    const data = new FormData(form);
    const controls = [input, file, clear, form.querySelector('[type=submit]')];
    sending = true;
    controls.forEach(c => { c.disabled = true; });
    pulse(false);
    note.textContent = file.files.length ? 'Uploading…' : 'Sending…';
    try {
      const res = await fetch(form.action, { method: 'POST', body: data, headers: { Accept: 'application/json' } });
      if (res.redirected) throw new Error('Please sign in again to send.');
      const result = await res.json().catch(() => ({}));
      if (!res.ok || !result.ok) throw new Error(result.title || 'Could not send the message.');
      form.reset(); showFile(); note.textContent = '';
      if (historical) { location.assign('/conversations/' + id); return; }
      bottom();
      await refresh();
    } catch (error) { note.textContent = error.message || 'Connection interrupted. Check the conversation before retrying.'; }
    finally { controls.forEach(c => { c.disabled = false; }); sending = false; input.focus({ preventScroll: true }); }
  });
}
