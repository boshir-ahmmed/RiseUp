const videos = [...document.querySelectorAll('video[data-feedvideo]')];
if (videos.length && 'IntersectionObserver' in window) {
  const visible = new Map();
  const userPaused = new WeakSet();
  const motion = matchMedia('(prefers-reduced-motion: reduce)');
  let current;
  function choose() {
    const candidates = videos.filter(v => (visible.get(v) || 0) >= .5);
    candidates.sort((a, b) => visible.get(b) - visible.get(a));
    const next = document.hidden ? null : candidates[0];
    if (current === next) return; // A pause using the native controls lasts while this video is in view.
    current = next;
    videos.forEach(v => { if (v !== next && !v.paused) v.pause(); });
    if (next && !userPaused.has(next) && !motion.matches && !navigator.connection?.saveData) {
      next.muted = true;
      next.play().catch(() => {}); // Native controls also work when autoplay is blocked.
    }
  }
  const observer = new IntersectionObserver(entries => {
    for (const entry of entries) {
      visible.set(entry.target, entry.intersectionRatio);
      if (!entry.isIntersecting) userPaused.delete(entry.target);
    }
    choose();
  }, { threshold: Array.from({ length: 21 }, (_, i) => i / 20) });
  videos.forEach(video => {
    observer.observe(video);
    video.addEventListener('pause', () => {
      if (video === current && !document.hidden && !video.ended) userPaused.add(video);
    });
    video.addEventListener('play', () => {
      userPaused.delete(video);
      if (document.hidden) { video.pause(); return; }
      current = video;
      videos.forEach(other => { if (other !== video && !other.paused) other.pause(); });
    });
  });
  document.addEventListener('visibilitychange', choose);
  window.addEventListener('pagehide', () => { current = null; videos.forEach(video => video.pause()); });
  window.addEventListener('pageshow', choose);
}
