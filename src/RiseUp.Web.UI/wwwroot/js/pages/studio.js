// Pitch Studio uploads: reads the video's length and grabs a poster frame in the browser, then uploads with progress.
// The server re-reads the MP4/MOV duration itself and enforces every limit; the browser values are only a convenience.
const token = () => document.querySelector('meta[name="csrf-token"]')?.content ?? '';

function probe(file) {
  return new Promise(resolve => {
    const url = URL.createObjectURL(file);
    const v = document.createElement('video');
    v.preload = 'metadata'; v.muted = true; v.playsInline = true; v.src = url;
    let done = false;
    const finish = (dur, blob) => { if (done) return; done = true; URL.revokeObjectURL(url); resolve({ duration: dur, poster: blob }); };
    v.onerror = () => finish(null, null);
    v.onloadedmetadata = () => {
      const dur = isFinite(v.duration) ? v.duration : null;
      v.currentTime = Math.min(1, (dur || 2) / 3);
      v.onseeked = () => {
        const c = document.createElement('canvas'); c.width = 640; c.height = 360;
        try { c.getContext('2d').drawImage(v, 0, 0, 640, 360); c.toBlob(b => finish(dur, b), 'image/jpeg', 0.82); } catch { finish(dur, null); }
      };
    };
    setTimeout(() => finish(null, null), 15000);
  });
}

document.addEventListener('change', async e => {
  const input = e.target.closest('input[type=file][data-video-upload]');
  if (!input || !input.files[0]) return;
  const form = input.closest('form');
  const file = input.files[0];
  const status = form.querySelector('[data-status]'), bar = form.querySelector('.progress-line i');
  const max = parseInt(input.dataset.maxSeconds || '0', 10);
  status.textContent = 'Reading video…';
  const { duration, poster } = await probe(file);
  if (duration && max && duration > max + 0.5) { status.textContent = `Too long: ${Math.round(duration)}s (max ${max}s). Trim it and try again.`; input.value = ''; return; }
  const fd = new FormData();
  fd.append('video', file);
  if (poster) fd.append('poster', poster, 'poster.jpg');
  if (duration) fd.append('clientDuration', String(duration));
  const xhr = new XMLHttpRequest();
  xhr.open('POST', form.action); xhr.setRequestHeader('X-CSRF-TOKEN', token()); xhr.setRequestHeader('Accept', 'application/json');
  xhr.upload.onprogress = ev => { if (ev.lengthComputable) { const p = Math.round(ev.loaded / ev.total * 100); bar.style.width = p + '%'; status.textContent = `Uploading… ${p}%`; } };
  xhr.onload = () => {
    let r = {}; try { r = JSON.parse(xhr.responseText); } catch { /* ignore */ }
    if (xhr.status < 400 && r.ok !== false) location.reload();
    else { status.textContent = r.title || 'Upload failed.'; bar.style.width = '0'; input.value = ''; }
  };
  xhr.onerror = () => { status.textContent = 'Network error. Please try again.'; };
  xhr.send(fd);
});

// Auto-submit simple toggles and file pickers marked data-autosubmit.
document.addEventListener('change', e => {
  const el = e.target.closest('[data-autosubmit]');
  if (el && el.form) el.form.requestSubmit();
});
