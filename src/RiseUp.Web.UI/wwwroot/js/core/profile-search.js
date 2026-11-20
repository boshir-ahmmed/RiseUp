// Enhance ordinary search forms and the native recipient selector after the page loads.
function enhanceSearch(root, index) {
  const select = root.querySelector('select[name=investorId]');
  const input = select ? document.createElement('input') : root.querySelector('input');
  const form = root.closest('form');
  const selectedDetail = document.createElement('span');
  if (select) {
    input.type = 'text';
    input.id = select.id + '-query';
    input.className = 'input';
    input.maxLength = 100;
    input.required = true;
    input.placeholder = 'Search by investor name or firm';
    root.querySelector('label').htmlFor = input.id;
    select.hidden = true;
    select.required = false;
    select.after(input, selectedDetail);
    selectedDetail.className = 'recipient-detail small muted';
    selectedDetail.id = input.id + '-detail';
    input.setAttribute('aria-describedby', selectedDetail.id);
  }
  input.autocomplete = 'off';
  input.setAttribute('role', 'combobox');
  input.setAttribute('aria-autocomplete', 'list');
  input.setAttribute('aria-expanded', 'false');

  const popup = document.createElement('div');
  popup.className = 'profile-popup';
  popup.hidden = true;
  const list = document.createElement('div');
  list.id = 'profile-results-' + index;
  list.setAttribute('role', 'listbox');
  list.setAttribute('aria-label', 'Matching profiles');
  input.setAttribute('aria-controls', list.id);
  const feedback = document.createElement('div');
  feedback.className = 'profile-feedback';
  const live = document.createElement('span');
  live.className = 'sr';
  live.setAttribute('role', 'status');
  popup.append(feedback, list);
  root.append(popup, live);

  const matches = root.dataset.matches ? JSON.parse(root.dataset.matches) : null;
  let rows = [];
  let active = -1;
  let timer;
  let request;
  let composing = false;

  function avatar(row) {
    const badge = document.createElement('span');
    badge.className = 'suggestion-avatar';
    badge.setAttribute('aria-hidden', 'true');
    badge.textContent = row.name.trim().split(/\s+/u).slice(0, 2).map(word => Array.from(word)[0]).join('');
    if (row.photo) {
      const img = document.createElement('img');
      img.src = row.photo;
      img.alt = '';
      img.addEventListener('error', () => img.remove(), { once: true });
      badge.append(img);
    }
    return badge;
  }

  function setRecipient(option) {
    input.value = option.dataset.name;
    const accepting = option.dataset.accepting !== 'false';
    selectedDetail.replaceChildren(avatar({ name: input.value, photo: option.dataset.photo }),
      document.createTextNode([option.dataset.detail, accepting ? 'Accepting pitches' : 'Pitches paused'].filter(Boolean).join(' · ')));
    input.setCustomValidity(accepting ? '' : 'This investor has paused pitches. Choose another investor to send.');
  }
  if (select?.value) setRecipient(select.selectedOptions[0]);

  function close() {
    clearTimeout(timer);
    request?.abort();
    request = undefined;
    popup.hidden = true;
    rows = [];
    active = -1;
    input.setAttribute('aria-expanded', 'false');
    input.removeAttribute('aria-activedescendant');
    live.textContent = '';
  }

  function choose(row) {
    if (!select) { location.assign(row.href); return; }
    let option = Array.from(select.options).find(item => item.value === String(row.id));
    if (!option) {
      option = new Option(row.name + ' — ' + row.detail, String(row.id));
      select.add(option);
    }
    Object.assign(option.dataset, { name: row.name, detail: row.detail, photo: row.photo ?? '', accepting: String(row.accepting) });
    select.value = option.value;
    setRecipient(option);
    close();
    input.focus();
    live.textContent = row.name + ' selected.';
  }

  function show(message) {
    feedback.textContent = message;
    popup.hidden = false;
    input.setAttribute('aria-expanded', 'true');
    live.textContent = message;
  }

  function render(found) {
    rows = found;
    list.replaceChildren();
    show(rows.length ? '' : 'No matches. Try another name.');
    if (rows.length) live.textContent = rows.length + ' suggestions available.';
    rows.forEach((row, offset) => {
      const item = document.createElement('div');
      item.id = list.id + '-' + offset;
      item.setAttribute('role', 'option');
      item.setAttribute('aria-selected', 'false');
      const copy = document.createElement('span');
      const name = document.createElement('strong');
      name.textContent = row.name;
      const detail = document.createElement('small');
      const context = select ? (row.accepting ? 'Accepting pitches' : 'Pitches paused') : (row.kind === 'investors' ? 'Investor' : 'Startup');
      detail.textContent = [row.detail, context].filter(Boolean).join(' · ');
      copy.append(name, detail);
      item.append(avatar(row), copy);
      item.addEventListener('mousedown', event => event.preventDefault()); // Keep input focus until click.
      item.addEventListener('click', () => choose(row));
      list.append(item);
    });
  }

  function search() {
    close();
    const term = input.value.trim();
    if (!term || composing) return;
    if (matches) {
      const needle = term.toLocaleLowerCase();
      render(matches.filter(row => [row.name, row.detail, ...row.reasons]
        .some(text => text.toLocaleLowerCase().includes(needle))).slice(0, 8));
      return;
    }
    request = new AbortController();
    const signal = request.signal;
    timer = setTimeout(async () => {
      list.replaceChildren();
      show('Searching…');
      const params = new URLSearchParams({ q: term, scope: root.dataset.profiles });
      if (root.dataset.profiles === 'startups') {
        for (const key of ['Industry', 'Stage']) {
          const value = form.elements.namedItem(key)?.value;
          if (value) params.set(key, value);
        }
      }
      try {
        const response = await fetch('/search/suggestions?' + params, { signal });
        if (!response.ok) throw new Error('Search unavailable');
        const found = await response.json();
        if (!signal.aborted) render(found);
      } catch {
        if (signal.aborted) return;
        show(select ? 'Suggestions unavailable.' : 'Suggestions unavailable. Press Enter to search.');
        if (select) {
          const fallback = document.createElement('button');
          fallback.type = 'button';
          fallback.className = 'linkbtn';
          fallback.textContent = 'Use investor list';
          fallback.addEventListener('click', () => {
            close();
            root.querySelector('label').htmlFor = select.id;
            input.disabled = input.hidden = true;
            selectedDetail.hidden = true;
            select.hidden = false;
            select.required = true;
            select.focus();
          });
          feedback.append(' ', fallback);
        }
      }
    }, 160);
  }

  input.addEventListener('compositionstart', () => { composing = true; close(); });
  input.addEventListener('compositionend', () => { composing = false; search(); });
  input.addEventListener('input', () => {
    if (select) {
      select.value = '';
      selectedDetail.replaceChildren();
      input.setCustomValidity(input.value.trim() ? 'Choose an investor from the suggestions.' : '');
    }
    search();
  });
  input.addEventListener('keydown', event => {
    if (event.isComposing || composing) return;
    if (event.key === 'Escape' && (!popup.hidden || request)) { event.preventDefault(); close(); return; }
    if (event.key === 'Enter' && active >= 0) { event.preventDefault(); choose(rows[active]); return; }
    if (!['ArrowDown', 'ArrowUp'].includes(event.key)) return;
    event.preventDefault();
    if (popup.hidden) { search(); return; }
    if (!rows.length) return;
    const forward = event.key === 'ArrowDown';
    active = active < 0 ? (forward ? 0 : rows.length - 1) : (active + (forward ? 1 : -1) + rows.length) % rows.length;
    Array.from(list.children).forEach((item, i) => item.setAttribute('aria-selected', String(i === active)));
    const item = list.children[active];
    input.setAttribute('aria-activedescendant', item.id);
    item.scrollIntoView({ block: 'nearest' });
  });
  root.addEventListener('focusout', event => { if (!root.contains(event.relatedTarget)) close(); });
  document.addEventListener('pointerdown', event => { if (!root.contains(event.target)) close(); });
}

document.querySelectorAll('[data-profiles]').forEach(enhanceSearch);
