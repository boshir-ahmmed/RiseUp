const form = document.querySelector('[data-composer]');
const preview = document.querySelector('.preview-panel');
const fields = form.elements;
const video = preview.querySelector('[data-preview-video]');
const image = preview.querySelector('[data-preview-image]');
const art = preview.querySelector('[data-preview-art]');
const reel = form.dataset.reel === 'true';
let selectedUrl;
let previewingDraft = false;

function setSource(element, name, url) {
  if (url) element.setAttribute(name, url);
  else element.removeAttribute(name);
}

function updateMedia() {
  if (selectedUrl) URL.revokeObjectURL(selectedUrl);
  const file = fields.attachment?.files[0];
  selectedUrl = file ? URL.createObjectURL(file) : null;
  const isImage = file && /\.(jpe?g|webp|png)$/i.test(file.name);
  const videoUrl = file ? (isImage ? null : selectedUrl) : form.dataset.video;
  const imageUrl = file ? (isImage ? selectedUrl : null) : form.dataset.poster;
  video.pause();
  setSource(video, 'poster', file ? null : form.dataset.poster);
  setSource(video, 'src', videoUrl);
  setSource(image, 'src', videoUrl ? null : imageUrl);
  video.hidden = !videoUrl;
  image.hidden = !imageUrl || !!videoUrl;
  art.hidden = !!videoUrl || !!imageUrl;
  if (fields.clientDuration) fields.clientDuration.value = '';
  video.load();
}

video.addEventListener('loadedmetadata', () => {
  if (selectedUrl && video.currentSrc === selectedUrl && Number.isFinite(video.duration)) {
    fields.clientDuration.value = String(video.duration);
  }
});

function update(event) {
  if (!previewingDraft || event.target === fields.attachment) updateMedia();
  previewingDraft = true;
  preview.querySelector('[data-preview-state]').textContent = 'Draft preview';
  preview.querySelector('[data-preview-category]').textContent = fields.category.value;
  preview.querySelector('[data-preview-title]').textContent = fields.title.value.trim() || 'Your title';
  preview.querySelector('[data-art-title]').textContent = fields.title.value.trim() || (reel ? 'Your reel' : 'Your next update');
  preview.querySelector('[data-preview-body]').textContent = fields.body.value.trim() || (reel ? 'Your caption will appear here.' : 'Your update will appear here.');
}

form.addEventListener('input', update);
window.addEventListener('pagehide', () => { if (selectedUrl) URL.revokeObjectURL(selectedUrl); });
