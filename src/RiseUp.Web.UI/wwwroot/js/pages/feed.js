import '../core/profile-search.js';
import '../core/playback.js';
import '../core/alert-sounds.js';
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
});
