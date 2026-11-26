// Apply a saved choice before the stylesheets load. Device mode follows the browser.
(() => {
  const key = 'riseup.appearance';
  const root = document.documentElement;
  let picker;
  function apply(value) {
    if (value === 'dark' || value === 'light') root.dataset.theme = value;
    else delete root.dataset.theme;
    if (picker) picker.value = root.dataset.theme || 'system';
  }
  try { apply(localStorage.getItem(key)); } catch { /* Device colours still work. */ }

  window.addEventListener('storage', event => {
    if (event.key === key || event.key === null) apply(event.newValue);
  });
  document.addEventListener('DOMContentLoaded', () => {
    const host = document.getElementById('memenu') || document.getElementById('pubnav');
    if (!host) return;
    const label = document.createElement('label');
    label.className = 'theme-picker';
    label.append('Appearance');
    picker = document.createElement('select');
    for (const [value, name] of [['system', 'Device'], ['light', 'Light'], ['dark', 'Dark']]) {
      picker.add(new Option(name, value));
    }
    picker.value = root.dataset.theme || 'system';
    picker.addEventListener('change', () => {
      apply(picker.value);
      try {
        if (picker.value === 'system') localStorage.removeItem(key);
        else localStorage.setItem(key, picker.value);
      } catch { /* Keep the selection for this page if storage is unavailable. */ }
    });
    label.append(picker);
    host.insertBefore(label, host.querySelector('form[action="/logout"]'));
  });
})();
