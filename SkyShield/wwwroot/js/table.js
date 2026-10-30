/*
 * table.js — tabelul de jos cu aeronavele.
 *
 * Nici el nu își ține date: citește snapshot-ul din state.js și
 * desenează câte un rând pentru fiecare aeronavă. Un click pe un rând
 * selectează aeronava (aceeași selecție folosită și de hartă).
 */
window.SkyShield = window.SkyShield || {};

SkyShield.Table = (function () {
  const COLS = 6;

  // Textul vine din server: îl scriem ca text, nu ca HTML, ca să nu strice pagina.
  function esc(value) {
    return String(value ?? '').replace(/[&<>"']/g, (ch) => ({
      '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    })[ch]);
  }

  // Timp de genul „acum", „12 s", „3 min" față de ultima actualizare.
  function timeAgo(iso) {
    const then = new Date(iso).getTime();
    if (!Number.isFinite(then)) return '—';
    const sec = Math.max(0, Math.round((Date.now() - then) / 1000));
    if (sec < 5) return 'acum';
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
      cell(Math.round(a.altitudeFeet).toLocaleString('ro-RO')) +
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
      count.textContent = s.aircraft.length + (s.aircraft.length === 1 ? ' aeronavă' : ' aeronave');
    }

    if (!s.aircraft.length) {
      tbody.innerHTML = '<tr><td colspan="' + COLS + '" class="muted empty">Se așteaptă date…</td></tr>';
      return;
    }

    const rows = s.aircraft.slice().sort((a, b) => a.icao.localeCompare(b.icao));
    tbody.innerHTML = rows.map((a) => rowHtml(a, s.selectedIcao)).join('');
  }

  // Un singur ascultător pentru tot tabelul: căutăm rândul apăsat și selectăm aeronava.
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

// Se leagă singur de stare imediat ce pagina e gata.
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', SkyShield.Table.init);
} else {
  SkyShield.Table.init();
}
