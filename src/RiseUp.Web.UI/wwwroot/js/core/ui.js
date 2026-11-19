// Small, dependency-free UI behaviours. All user content is inserted with textContent (never innerHTML).
import { post } from './http.js';

export function toast(message, ok = true) {
  const host = document.getElementById('toasts');
  if (!host) return;
  const el = document.createElement('div');
  el.className = 'toast' + (ok ? '' : ' bad');
  el.setAttribute('role', 'status');
  el.textContent = message;
  host.appendChild(el);
  setTimeout(() => el.remove(), ok ? 3800 : 6500);
}

// Read more / Show less: the server renders full text; we clamp it progressively.
function initClamp(root = document) {
  root.querySelectorAll('.clamp[data-limit]:not([data-ready])').forEach(el => {
    el.dataset.ready = '1';
    const full = el.textContent.trim();
    const limit = parseInt(el.dataset.limit, 10);
    if (full.length <= limit) return;
    const short = full.slice(0, limit).replace(/\s+\S*$/, '') + '…';
    const text = document.createElement('span');
    const btn = document.createElement('button');
    btn.type = 'button'; btn.className = 'linkbtn';
    let open = false;
    const render = () => { text.textContent = open ? full : short; btn.textContent = open ? ' Show less' : ' Read more'; btn.setAttribute('aria-expanded', String(open)); };
    btn.addEventListener('click', () => { open = !open; render(); });
    el.textContent = ''; el.append(text, ' ', btn); render();
  });
}

// Character counters for inputs with data-count="max".
function initCounters() {
  document.querySelectorAll('[data-count]').forEach(input => {
    const out = document.createElement('div');
    out.className = 'help';
    const span = document.createElement('span'); span.className = 'tiny';
    out.append(document.createElement('span'), span);
    input.after(out);
    const max = parseInt(input.dataset.count, 10);
    const update = () => { span.textContent = `${input.value.length.toLocaleString()} / ${max.toLocaleString()}`; span.className = input.value.length > max ? 'over' : 'tiny'; };
    input.addEventListener('input', update); update();
  });
}

async function like(id, button) {
  const r = await post(`/posts/${id}/like`);
  if (!r.ok) return toast(r.title || 'Only signed-in verified founders and investors can like posts.', false);
  const liked = r.value === true;
  const count = document.querySelector(`[data-likes="${id}"]`);
  if (count) count.textContent = String(Math.max(0, parseInt(count.textContent, 10) + (liked ? 1 : -1)));
  if (button) {
    button.classList.toggle('on', liked);
    button.setAttribute('aria-pressed', String(liked));
    const svg = button.querySelector('svg');
    svg?.setAttribute('fill', liked ? '#E0245E' : 'none');
    svg?.setAttribute('stroke', liked ? '#E0245E' : 'currentColor');
  }
  return liked;
}

document.addEventListener('click', async e => {
  const toggle = e.target.closest('[data-toggle]');
  if (toggle) document.querySelector(toggle.dataset.toggle)?.classList.toggle('open');

  const likeBtn = e.target.closest('[data-like]');
  if (likeBtn) {
    if (likeBtn.dataset.can !== '1') return toast('Only signed-in verified founders and investors can like posts.', false);
    await like(likeBtn.dataset.like, likeBtn);
  }
  const shareBtn = e.target.closest('[data-share]');
  if (shareBtn) {
    if (shareBtn.dataset.can !== '1') return toast('Only verified founders and investors can share posts.', false);
    const r = await post(`/posts/${shareBtn.dataset.share}/share`);
    if (!r.ok) return toast(r.title, false);
    const url = location.origin + r.value;
    try { await navigator.clipboard.writeText(url); toast('Link copied: ' + url); } catch { toast('Share link: ' + url); }
  }
  const confirmBtn = e.target.closest('[data-confirm]');
  if (confirmBtn && !window.confirm(confirmBtn.dataset.confirm)) e.preventDefault();
});

// Double-tap to like (feed and post detail).
document.addEventListener('dblclick', async e => {
  const media = e.target.closest('[data-dbl]');
  if (!media) return;
  const id = media.dataset.dbl;
  const btn = document.querySelector(`[data-like="${id}"]`);
  if (btn?.dataset.can !== '1') return toast('Only signed-in verified founders and investors can like posts.', false);
  const burst = media.querySelector('[data-burst]');
  burst?.classList.remove('go'); void burst?.offsetWidth; burst?.classList.add('go');
  if (!btn.classList.contains('on')) await like(id, btn);
});

initClamp();
initCounters();
