// Restore an explicit dark preference before the stylesheets load.
(() => {
  const key = 'riseup.appearance';
  const root = document.documentElement;
  const scheme = document.querySelector('meta[name="color-scheme"]');
  let toggle;

  function apply(value) {
    const dark = value === 'dark';
    root.dataset.theme = dark ? 'dark' : 'light';
    if (scheme) scheme.content = root.dataset.theme;
    if (toggle) toggle.setAttribute('aria-checked', String(dark));
  }

  try { apply(localStorage.getItem(key)); }
  catch { apply('light'); }

  window.addEventListener('storage', event => {
    if (event.key === key || event.key === null) apply(event.newValue);
  });

  document.addEventListener('DOMContentLoaded', () => {
    const host = document.getElementById('memenu') || document.getElementById('pubnav');
    if (!host) return;

    toggle = document.createElement('button');
    toggle.type = 'button';
    toggle.className = 'theme-picker';
    toggle.title = 'Dark mode';
    toggle.setAttribute('role', 'switch');
    toggle.setAttribute('aria-label', 'Dark mode');
    toggle.setAttribute('aria-checked', String(root.dataset.theme === 'dark'));
    toggle.addEventListener('click', () => {
      apply(root.dataset.theme === 'dark' ? 'light' : 'dark');
      try { localStorage.setItem(key, root.dataset.theme); }
      catch { /* The switch still works for this page. */ }
    });
    host.insertBefore(toggle, host.querySelector('form[action="/logout"]'));
  });
})();
