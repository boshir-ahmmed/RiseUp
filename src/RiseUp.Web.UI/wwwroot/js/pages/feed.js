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

// Feed/Reels videos autoplay muted when at least 60% visible, pause otherwise.
const vids = [...document.querySelectorAll('video[data-feedvideo]')];
if (vids.length && 'IntersectionObserver' in window) {
  const io = new IntersectionObserver(entries => entries.forEach(en => {
    const v = en.target;
    if (en.intersectionRatio >= 0.6) v.play().catch(() => {}); else v.pause();
  }), { threshold: [0, 0.6] });
  vids.forEach(v => io.observe(v));
}

document.addEventListener('click', async e => {
  const snd = e.target.closest('[data-sound]');
  if (snd) {
    const v = snd.closest('.ig-media, .pk-media')?.querySelector('video');
    if (v) { v.muted = !v.muted; snd.textContent = v.muted ? '🔇' : '🔊'; if (!v.muted) v.play().catch(() => {}); }
  }
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
