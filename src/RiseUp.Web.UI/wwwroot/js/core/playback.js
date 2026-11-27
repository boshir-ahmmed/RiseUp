const videos = [...document.querySelectorAll('video[data-feedvideo]')];
if (videos.length && 'IntersectionObserver' in window) {
  const visible = new Map();
  const userPaused = new WeakSet();
  const autoMuted = new WeakSet(); // Muted only because the browser refused to start it with sound.
  const expectedMuted = new WeakMap(); // The muted state this script set, to tell it apart from the viewer's own changes.
  const motion = matchMedia('(prefers-reduced-motion: reduce)');
  const SOUND_KEY = 'riseup.videoSound';
  let soundOn = true;
  try { soundOn = localStorage.getItem(SOUND_KEY) !== 'off'; } catch {}
  let current;
  function setMuted(video, muted) {
    expectedMuted.set(video, muted);
    video.muted = muted;
  }
  function start(video) {
    setMuted(video, !soundOn);
    autoMuted.delete(video);
    video.play().catch(() => {
      if (video !== current || video.muted) return;
      // Browsers block autoplay with sound until the viewer has interacted with the page: start muted instead.
      setMuted(video, true);
      autoMuted.add(video);
      video.play().catch(() => {}); // Native controls also work when autoplay is blocked.
    });
  }
  function choose() {
    const candidates = videos.filter(v => (visible.get(v) || 0) >= .5);
    candidates.sort((a, b) => visible.get(b) - visible.get(a));
    const next = document.hidden ? null : candidates[0];
    if (current === next) return; // A pause using the native controls lasts while this video is in view.
    current = next;
    videos.forEach(v => { if (v !== next && !v.paused) v.pause(); });
    if (next && !userPaused.has(next) && !motion.matches && !navigator.connection?.saveData) start(next);
  }
  // The first tap or key press anywhere on the page lets the browser play sound: unmute the video that had to start muted.
  function unlock(e) {
    if (e.target instanceof Element && e.target.closest('video')) return; // The video's own controls handle that tap.
    if (current && soundOn && autoMuted.has(current)) {
      autoMuted.delete(current);
      setMuted(current, false);
    }
  }
  document.addEventListener('pointerdown', unlock, true);
  document.addEventListener('keydown', unlock, true);
  const observer = new IntersectionObserver(entries => {
    for (const entry of entries) {
      visible.set(entry.target, entry.intersectionRatio);
      if (!entry.isIntersecting) userPaused.delete(entry.target);
    }
    choose();
  }, { threshold: Array.from({ length: 21 }, (_, i) => i / 20) });
  videos.forEach(video => {
    setMuted(video, !soundOn); // Videos the viewer starts themselves follow the same sound choice.
    observer.observe(video);
    video.addEventListener('volumechange', () => {
      if (video.muted === expectedMuted.get(video)) return;
      // The viewer used the mute button: remember the choice for every video, on this and later pages.
      expectedMuted.set(video, video.muted);
      autoMuted.delete(video);
      soundOn = !video.muted;
      try { localStorage.setItem(SOUND_KEY, soundOn ? 'on' : 'off'); } catch {}
    });
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
