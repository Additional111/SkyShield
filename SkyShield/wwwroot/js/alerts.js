/*
 * alerts.js — panoul de alerte și rezumatul de risc.
 *
 * Citește alertele din SkyShield.State și:
 *   1. umple lista #alerts-list (cele mai noi primele);
 *   2. numără câte aeronave sunt pe fiecare nivel de risc
 *      și scrie cifrele în #risk-low / #risk-medium / #risk-critical.
 */
window.SkyShield = window.SkyShield || {};

SkyShield.Alerts = (function () {
  var listEl, countEl, riskEls;

  function esc(text) {
    return String(text == null ? '' : text)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;')
      .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  }

  // Acceptă și camelCase (mock), și PascalCase (serverul .NET).
  function field(obj, camel, pascal) {
    return obj[camel] != null ? obj[camel] : obj[pascal];
  }

  function timeOf(iso) {
    var d = new Date(iso);
    if (isNaN(d)) return '';
    return d.toLocaleTimeString('ro-RO', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  }

  function alertHtml(a) {
    var score = Number(field(a, 'riskScore', 'RiskScore')) || 0;
    var level = SkyShield.State.riskLevel(score);
    var icao = field(a, 'icao', 'Icao');
    var type = field(a, 'anomalyType', 'AnomalyType');
    var desc = field(a, 'description', 'Description');
    var when = timeOf(field(a, 'detectedAtUtc', 'DetectedAtUtc'));
    return '<li data-level="' + level + '" data-icao="' + esc(icao) + '">'
      + '<strong>' + esc(icao) + '</strong> · ' + esc(type)
      + ' <span class="muted">(risc ' + score + ', ' + when + ')</span>'
      + (desc ? '<br><span class="muted">' + esc(desc) + '</span>' : '')
      + '</li>';
  }

  function render() {
    var snap = SkyShield.State.getSnapshot();
    var alerts = snap.alerts;

    // 1. Lista de alerte
    countEl.textContent = String(alerts.length);
    if (alerts.length === 0) {
      listEl.innerHTML = '<li class="muted empty">Nicio alertă încă.</li>';
    } else {
      listEl.innerHTML = alerts.map(alertHtml).join('');
    }

    // 2. Rezumatul de risc: nivelul maxim pe aeronavă
    var counts = { low: 0, medium: 0, critical: 0 };
    var worstByIcao = {};
    alerts.forEach(function (a) {
      var icao = field(a, 'icao', 'Icao');
      var score = Number(field(a, 'riskScore', 'RiskScore')) || 0;
      if (worstByIcao[icao] == null || score > worstByIcao[icao]) {
        worstByIcao[icao] = score;
      }
    });
    Object.keys(worstByIcao).forEach(function (icao) {
      counts[SkyShield.State.riskLevel(worstByIcao[icao])]++;
    });
    riskEls.low.textContent = String(counts.low);
    riskEls.medium.textContent = String(counts.medium);
    riskEls.critical.textContent = String(counts.critical);
  }

  function init() {
    listEl = document.getElementById('alerts-list');
    countEl = document.getElementById('alert-count');
    riskEls = {
      low: document.getElementById('risk-low'),
      medium: document.getElementById('risk-medium'),
      critical: document.getElementById('risk-critical')
    };
    if (!listEl || !countEl) return;

    // Click pe o alertă selectează aeronava (o evidențiază pe hartă și în tabel).
    listEl.addEventListener('click', function (ev) {
      var li = ev.target.closest('li[data-icao]');
      if (li) SkyShield.State.select(li.getAttribute('data-icao'));
    });

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
