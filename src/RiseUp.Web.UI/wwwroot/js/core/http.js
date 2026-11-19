// Shared fetch wrapper: sends the anti-forgery token header and normalises error handling.
const token = () => document.querySelector('meta[name="csrf-token"]')?.content ?? '';

export async function post(url, body = null) {
  const res = await fetch(url, {
    method: 'POST',
    credentials: 'same-origin',
    headers: { 'X-CSRF-TOKEN': token(), 'Accept': 'application/json', ...(body ? { 'Content-Type': 'application/json' } : {}) },
    body: body ? JSON.stringify(body) : null,
  });
  let data = null;
  try { data = await res.json(); } catch { /* non-JSON (e.g. redirect to login) */ }
  if (res.status === 401 || res.redirected) return { ok: false, code: 'unauthenticated', title: 'Please sign in first.' };
  return data ?? { ok: res.ok, code: res.ok ? '' : 'error', title: res.ok ? '' : 'Request failed. Please try again.' };
}
