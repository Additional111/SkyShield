/*
 * table.js — the bottom table with the aircraft.
 *
 * It doesn't keep its own data either: it reads the snapshot from
 * state.js and draws one row per aircraft. Clicking a row selects
 * the aircraft (the same selection used by the map).
 */
window.SkyShield = window.SkyShield || {};

SkyShield.Table = (function () {
  const COLS = 6;

  // Text comes from the server: we write it as text, not HTML, so it can't break the page.
  function esc(value) {
    return String(value ?? '').replace(/[&<>"']/g, (ch) => ({
      '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    })[ch]);
  }

  // Time like "now", "12 s", "3 min" since the last update.
  function timeAgo(iso) {
    const then = new Date(iso).getTime();
    if (!Number.isFinite(then)) return '—';
    const sec = Math.max(0, Math.round((Date.now() - then) / 1000));
    if (sec < 5) return 'now';
    if (sec < 60) return sec + ' s';
    const min = Math.round(sec / 60);
    if (min < 60) return min + ' min';
    return Math.round(min / 60) + ' h';
  }

  function cell(text, extraClass) {
    return '<td' + (extraClass ? ' class="' + extraClass + '"' : '') + '>' + esc(text) + '</td>';
  }

  function rowHtml(a, selectedIcao) {
    return '<tr data-icao="' + esc(a.icao) + '"' + (a.icao === selectedIcao ? ' class="selected"' : '') + '>' +
      cell(a.icao) +
      cell(a.latitude.toFixed(4)) +
      cell(a.longitude.toFixed(4)) +
      cell(Math.round(a.altitudeFeet).toLocaleString('en-US')) +
      cell(Math.round(a.groundSpeedKnots)) +
      cell(timeAgo(a.timestampUtc), 'muted') +
      '</tr>';
  }

  function render() {
    const tbody = document.getElementById('aircraft-table');
    if (!tbody) return;

    const s = SkyShield.State.getSnapshot();

    const count = document.getElementById('aircraft-count');
    if (count) {
      count.textContent = s.aircraft.length + ' aircraft';
    }

    if (!s.aircraft.length) {
      tbody.innerHTML = '<tr><td colspan="' + COLS + '" class="muted empty">Waiting for data…</td></tr>';
      return;
    }

    const rows = s.aircraft.slice().sort((a, b) => a.icao.localeCompare(b.icao));
    tbody.innerHTML = rows.map((a) => rowHtml(a, s.selectedIcao)).join('');
  }

  // A single listener for the whole table: find the clicked row and select the aircraft.
  function init() {
    const tbody = document.getElementById('aircraft-table');
    if (!tbody) return;
    tbody.addEventListener('click', (ev) => {
      const row = ev.target.closest('tr[data-icao]');
      if (row) SkyShield.State.select(row.getAttribute('data-icao'));
    });
    SkyShield.State.onChange(render);
    render();
  }

  return { init, render };
})();

// It hooks itself up to the state as soon as the page is ready.
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', SkyShield.Table.init);
} else {
  SkyShield.Table.init();
}
