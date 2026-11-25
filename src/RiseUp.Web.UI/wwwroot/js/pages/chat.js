const root = document.querySelector('[data-conversation]');
const list = document.getElementById('msgs');
if (root && list && window.signalR) {
  const id = Number(root.dataset.conversation);
  const refresh = root.querySelector('[data-show-latest]');
  const connection = new signalR.HubConnectionBuilder().withUrl('/hubs/chat').withAutomaticReconnect().build();
  const historical = new URLSearchParams(location.search).has('before');
  async function join() { await connection.invoke('Join', id); }
  function newMessages() {
    refresh.textContent = 'Show latest messages';
    document.dispatchEvent(new Event('riseup:check-alerts'));
  }
  connection.on('changed', changed => { if (changed === id) newMessages(); });
  connection.onreconnected(async () => { await join(); newMessages(); });
  connection.start().then(join).catch(() => { /* Normal form posts remain available. */ });
  refresh.addEventListener('click', async event => {
    if (historical) return;
    event.preventDefault();
    refresh.textContent = 'Refresh messages';
    try {
      const response = await fetch('/conversations/' + id + '/messages', { redirect: 'error' });
      if (!response.ok) throw new Error('Conversation unavailable');
      const doc = new DOMParser().parseFromString(await response.text(), 'text/html');
      list.replaceChildren(...doc.body.childNodes);
      list.scrollTop = list.scrollHeight;
    } catch { refresh.textContent = 'Retry loading messages'; }
  });
  if (!historical) list.scrollTop = list.scrollHeight;
}
