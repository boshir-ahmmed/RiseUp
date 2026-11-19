// Pitch room: chapter seeking, in-app deck viewer, and engagement tracking for the receiving investor only
// (the server ignores tracking from anyone else).
import { post } from '../core/http.js';

const room = document.querySelector('[data-room]');
const id = room?.dataset.room;
const track = room?.dataset.track === '1';
const send = body => { if (track && id) post(`/pitch-room/${id}/track`, body); };

document.querySelectorAll('video[data-slot]').forEach(v => {
  let last = 0;
  const report = force => {
    if (!v.duration) return;
    const pct = Math.round(Math.min(1, v.currentTime / v.duration) * 100);
    if (force || pct - last >= 10) { last = pct; send({ kind: 'video', slot: v.dataset.slot, percent: pct }); }
  };
  v.addEventListener('timeupdate', () => report(false));
  v.addEventListener('ended', () => { last = 100; send({ kind: 'video', slot: v.dataset.slot, percent: 100 }); });
  v.addEventListener('pause', () => report(true));
});

document.addEventListener('click', e => {
  const ch = e.target.closest('[data-seek]');
  if (ch) { const v = document.getElementById(ch.dataset.target); if (v) { v.currentTime = parseFloat(ch.dataset.seek); v.play().catch(() => {}); } }
  const tab = e.target.closest('[data-vtab]');
  if (tab) {
    document.querySelectorAll('[data-vtab]').forEach(t => t.classList.toggle('on', t === tab));
    document.querySelectorAll('[data-vpane]').forEach(p => { const show = p.dataset.vpane === tab.dataset.vtab; p.hidden = !show; if (!show) p.querySelector('video')?.pause(); });
  }
  const nav = e.target.closest('[data-slide]');
  if (nav) {
    const deck = nav.closest('[data-deck]');
    const slides = [...deck.querySelectorAll('.slide')];
    let i = slides.findIndex(s => !s.hidden);
    i = nav.dataset.slide === 'next' ? i + 1 : nav.dataset.slide === 'prev' ? i - 1 : parseInt(nav.dataset.slide, 10);
    i = Math.max(0, Math.min(slides.length - 1, i));
    slides.forEach((s, n) => { s.hidden = n !== i; });
    deck.querySelectorAll('.th').forEach((t, n) => t.classList.toggle('on', n === i));
    deck.querySelector('[data-slide="prev"]').disabled = i === 0;
    deck.querySelector('[data-slide="next"]').disabled = i === slides.length - 1;
    send({ kind: 'slide', slide: i });
  }
  const doc = e.target.closest('[data-doc]');
  if (doc) send({ kind: 'doc', documentId: parseInt(doc.dataset.doc, 10) });
});
if (document.querySelector('[data-deck] .slide')) send({ kind: 'slide', slide: 0 });
