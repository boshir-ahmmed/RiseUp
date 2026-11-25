const stream = document.getElementById('reels-stream');
if (stream) {
  const slides = [...stream.querySelectorAll('.reel-slide')];
  const pager = document.querySelector('.reel-pager');
  const [previous, next] = pager.querySelectorAll('button');
  const position = pager.querySelector('output');
  const index = () => Math.min(slides.length - 1, Math.max(0, Math.round(stream.scrollTop / stream.clientHeight)));
  function update() {
    const current = index();
    position.textContent = (current + 1) + ' / ' + slides.length;
    previous.disabled = current === 0;
    next.disabled = current === slides.length - 1;
  }
  function go(i, smooth) {
    if (i < 0 || i >= slides.length) return;
    stream.scrollTo({ top: slides[i].offsetTop - slides[0].offsetTop,
      behavior: smooth && !matchMedia('(prefers-reduced-motion: reduce)').matches ? 'smooth' : 'instant' });
  }
  pager.hidden = false;
  pager.addEventListener('click', e => {
    const button = e.target.closest('[data-step]');
    if (button) go(index() + Number(button.dataset.step), true);
  });
  stream.addEventListener('keydown', e => {
    if (e.target !== stream || !['ArrowDown', 'ArrowUp'].includes(e.key)) return;
    e.preventDefault();
    go(index() + (e.key === 'ArrowDown' ? 1 : -1), true);
  });
  stream.addEventListener('scroll', update, { passive: true });
  const target = slides.findIndex(slide => '#' + slide.id === location.hash);
  if (target >= 0) go(target, false);
  update();
}
