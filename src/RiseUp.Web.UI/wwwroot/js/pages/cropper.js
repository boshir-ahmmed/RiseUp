// Client-side crop for profile photos, covers and logos. The cropped JPEG is uploaded with the anti-forgery header;
// the server re-validates type/size/signature and malware-scans it (the browser is never trusted).
const token = () => document.querySelector('meta[name="csrf-token"]')?.content ?? '';
const SHAPES = {
  photo: { view: [300, 300], out: [400, 400], round: true, min: [200, 200], maxMB: 5, title: 'Crop profile photo' },
  cover: { view: [480, 120], out: [1200, 300], round: false, min: [800, 200], maxMB: 8, title: 'Crop cover photo' },
  logo: { view: [300, 300], out: [400, 400], round: false, min: [200, 200], maxMB: 2, title: 'Crop logo' },
};

function upload(url, blob, name) {
  return new Promise(resolve => {
    const fd = new FormData(); fd.append('file', blob, name);
    const xhr = new XMLHttpRequest();
    xhr.open('POST', url); xhr.setRequestHeader('X-CSRF-TOKEN', token()); xhr.setRequestHeader('Accept', 'application/json');
    xhr.onload = () => { try { resolve(JSON.parse(xhr.responseText)); } catch { resolve({ ok: xhr.status < 400, title: 'Upload failed.' }); } };
    xhr.onerror = () => resolve({ ok: false, title: 'Network error. Please try again.' });
    xhr.send(fd);
  });
}

function showError(input, msg) {
  const out = input.closest('[data-crop-host]')?.querySelector('[data-crop-error]');
  if (out) out.textContent = msg; else alert(msg);
}

document.addEventListener('change', e => {
  const input = e.target.closest('input[type=file][data-crop]');
  if (!input || !input.files[0]) return;
  const file = input.files[0]; input.value = '';
  const shape = SHAPES[input.dataset.crop];
  if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) return showError(input, 'Use a JPG, PNG or WebP image.');
  if (file.size > shape.maxMB * 1048576) return showError(input, `Images are limited to ${shape.maxMB} MB.`);
  const url = URL.createObjectURL(file);
  const img = new Image();
  img.onerror = () => showError(input, "That file couldn't be read as an image.");
  img.onload = () => {
    if (img.naturalWidth < shape.min[0] || img.naturalHeight < shape.min[1]) return showError(input, `Image is too small — at least ${shape.min[0]}×${shape.min[1]} px.`);
    const [W, H] = shape.view;
    const dlg = document.createElement('dialog');
    dlg.className = 'crop-dialog';
    dlg.innerHTML = `<form method="dialog" class="stack"><h3 style="margin:0"></h3><div class="crop-wrap"><canvas width="${W}" height="${H}" class="${shape.round ? 'round' : ''}" style="max-width:100%"></canvas></div>
      <label class="field"><span>Zoom</span><input type="range" min="1" max="3" step="0.01" value="1"></label>
      <p class="tiny muted" style="margin:0">Drag to reposition. Reported images are reviewed by a moderator.</p>
      <div class="progress-line"><i></i></div><div class="err" role="alert"></div>
      <div class="row" style="justify-content:flex-end"><button value="cancel" class="btn btn-outline">Cancel</button><button value="ok" class="btn btn-primary" data-apply>Apply</button></div></form>`;
    dlg.querySelector('h3').textContent = shape.title;
    document.body.appendChild(dlg);
    const cv = dlg.querySelector('canvas'), zoom = dlg.querySelector('input[type=range]');
    let z = 1, ox = 0, oy = 0, drag = null;
    const draw = target => {
      const t = target || cv, c = t.getContext('2d'), tw = t.width, th = t.height;
      const base = Math.max(tw / img.naturalWidth, th / img.naturalHeight) * z;
      const dw = img.naturalWidth * base, dh = img.naturalHeight * base;
      ox = Math.max(-1, Math.min(1, ox)); oy = Math.max(-1, Math.min(1, oy));
      c.fillStyle = '#fff'; c.fillRect(0, 0, tw, th);
      c.drawImage(img, (tw - dw) / 2 + ox * (dw - tw) / 2, (th - dh) / 2 + oy * (dh - th) / 2, dw, dh);
    };
    draw();
    zoom.addEventListener('input', () => { z = +zoom.value; draw(); });
    cv.addEventListener('pointerdown', ev => { drag = { x: ev.clientX, y: ev.clientY, ox, oy }; cv.setPointerCapture(ev.pointerId); });
    cv.addEventListener('pointermove', ev => { if (!drag) return; const r = cv.getBoundingClientRect(); ox = drag.ox - (ev.clientX - drag.x) / (r.width / 2); oy = drag.oy - (ev.clientY - drag.y) / (r.height / 2); draw(); });
    cv.addEventListener('pointerup', () => { drag = null; });
    dlg.querySelector('[data-apply]').addEventListener('click', async ev => {
      ev.preventDefault();
      const out = document.createElement('canvas'); out.width = shape.out[0]; out.height = shape.out[1]; draw(out);
      const bar = dlg.querySelector('.progress-line i'); bar.style.width = '60%';
      const blob = await new Promise(r => out.toBlob(r, 'image/jpeg', 0.86));
      const res = await upload(input.dataset.action, blob, 'image.jpg');
      bar.style.width = '100%';
      if (!res.ok) { dlg.querySelector('.err').textContent = res.title || 'Upload failed.'; return; }
      location.reload();
    });
    dlg.addEventListener('close', () => { URL.revokeObjectURL(url); dlg.remove(); });
    dlg.showModal();
  };
  img.src = url;
});
