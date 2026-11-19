// Real-time chat via SignalR. The server re-checks membership on join and on every send.
const root = document.querySelector('[data-conversation]');
if (root && window.signalR) {
  const id = parseInt(root.dataset.conversation, 10);
  const list = document.getElementById('msgs');
  const conn = new signalR.HubConnectionBuilder().withUrl('/hubs/chat').withAutomaticReconnect().build();
  conn.on('message', m => {
    if (m.conversationId !== id || document.querySelector(`[data-msg="${m.id}"]`)) return;
    const el = document.createElement('div');
    el.className = 'msg'; el.dataset.msg = m.id;
    const meta = document.createElement('div'); meta.className = 'meta';
    const who = document.createElement('strong'); who.textContent = m.author;
    meta.append(who);
    const body = document.createElement('div'); body.textContent = m.body;
    el.append(meta, body);
    list.append(el); list.scrollTop = list.scrollHeight;
  });
  conn.start().then(() => conn.invoke('Join', id)).catch(() => { /* falls back to normal form posts */ });
}
document.getElementById('msgs')?.scrollTo({ top: 1e9 });
