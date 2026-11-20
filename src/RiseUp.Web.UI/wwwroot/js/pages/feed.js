import '../core/profile-search.js';
// Shell + feed behaviours: Me menu, muted autoplay for feed videos, sound toggle, saving posts, spotlight teasers.
import { post } from '../core/http.js';
import { toast } from '../core/ui.js';

const accountButton = document.querySelector('[aria-controls="memenu"]');
function closeAccountMenu(restoreFocus) {
  if (accountButton?.getAttribute('aria-expanded') !== 'true') return;
  document.getElementById('memenu').classList.remove('open');
  accountButton.setAttribute('aria-expanded', 'false');
  if (restoreFocus) accountButton.focus();
}
document.addEventListener('click', e => {
  if (!e.target.closest('.tb-me')) closeAccountMenu(false);
});
document.addEventListener('keydown', e => {
  if (e.key === 'Escape') closeAccountMenu(true);
});

// A user starts playback. Pause offscreen media to save bandwidth.
const videos = document.querySelectorAll('video[data-feedvideo]');
if (videos.length && 'IntersectionObserver' in window) {
  const observer = new IntersectionObserver(entries => {
    for (const entry of entries) if (!entry.isIntersecting) entry.target.pause();
  });
  videos.forEach(video => observer.observe(video));
}

document.addEventListener('click', async e => {
  const save = e.target.closest('[data-save]');
  if (save) {
    const r = await post(`/posts/${save.dataset.save}/save`);
    if (!r.ok) return toast(r.title || 'Sign in to save posts.', false);
    const on = r.value === true;
    save.classList.toggle('on', on);
    save.setAttribute('aria-pressed', String(on));
    const label = save.querySelector('span'); if (label) label.textContent = on ? 'Saved' : 'Save';
    toast(r.message || (on ? 'Saved.' : 'Removed from saved.'));
  }
  // Pitch spotlight: swap the poster for the teaser video and play it inline.
  const play = e.target.closest('[data-play]');
  if (play) {
    const box = play.closest('.pk-media');
    if (!box || box.querySelector('video')) return;
    const v = document.createElement('video');
    v.src = play.dataset.play; v.controls = true; v.playsInline = true; v.autoplay = true;
    v.style.cssText = 'position:absolute;inset:0;width:100%;height:100%;object-fit:cover;background:#000';
    box.querySelectorAll('img,.pk-shade,.pk-play,.pk-dur').forEach(n => n.remove());
    box.appendChild(v);
    v.play().catch(() => {});
  }
});
