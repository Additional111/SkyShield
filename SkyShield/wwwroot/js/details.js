/*
 * details.js — panoul „Aeronavă selectată".
 *
 * Citește aeronava selectată din SkyShield.State și afișează
 * valorile ei curente. Dacă nu e nimic selectat, arată textul de așteptare.
 * Click pe o alertă sau pe un rând de tabel selectează aeronava.
 */
window.SkyShield = window.SkyShield || {};

SkyShield.Details = (function () {
  var el;

  function esc(text) {
    return String(text == null ? '' : text)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;')
      .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  }

  function timeAgo(iso) {
    var d = new Date(iso);
    if (isNaN(d)) return '—';
    var s = Math.round((Date.now() - d.getTime()) / 1000);
    if (s < 5) return 'acum';
    if (s < 60) return s + ' s';
    if (s < 3600) return Math.round(s / 60) + ' min';
    return Math.round(s / 3600) + ' h';
  }

  // Cea mai mare alertă activă pentru aeronava respectivă.
  function worstAlert(alerts, icao) {
    var worst = null;
    alerts.forEach(function (a) {
      if (a.icao !== icao) return;
      if (!worst || a.riskScore > worst.riskScore) worst = a;
    });
    return worst;
  }

  function row(label, value) {
    return '<span class="muted">' + esc(label) + '</span><span>' + value + '</span>';
  }

  function render() {
    if (!el) return;
    var snap = SkyShield.State.getSnapshot();
    var a = snap.selected;

    if (!a) {
      el.classList.add('muted');
      el.textContent = 'Selectează o aeronavă pe hartă sau în tabel.';
      return;
    }

    el.classList.remove('muted');
    var worst = worstAlert(snap.alerts, a.icao);
    var level = worst ? SkyShield.State.riskLevel(worst.riskScore) : 'low';
    var levelText = level === 'critical' ? 'Critic' : level === 'medium' ? 'Mediu' : 'Scăzut';

    el.innerHTML =
      row('ICAO', '<strong>' + esc(a.icao) + '</strong>') +
      row('Altitudine', esc(Math.round(a.altitudeFeet).toLocaleString('ro-RO')) + ' ft') +
      row('Viteză', esc(Math.round(a.groundSpeedKnots)) + ' kt') +
      row('Poziție', esc(a.latitude.toFixed(4)) + ', ' + esc(a.longitude.toFixed(4))) +
      row('Ultima actualizare', esc(timeAgo(a.timestampUtc))) +
      row('Risc', '<span data-level="' + level + '">' + levelText +
           (worst ? ' (' + esc(worst.riskScore) + ' · ' + esc(worst.anomalyType) + ')' : '') +
           '</span>');
  }

  function init() {
    el = document.getElementById('aircraft-details');
    if (!el) return;
    SkyShield.State.onChange(render);
    render();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  return { render };
})();
