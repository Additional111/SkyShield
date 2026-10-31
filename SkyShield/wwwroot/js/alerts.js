/*
  alerts.js — the alerts panel and the risk summary.
  Reads the alerts from SkyShield.State and:
 *   1. fills the #alerts-list (newest first);
 *   2. counts how many aircraft are on each risk level
 *      and writes the numbers into #risk-low / #risk-medium / #risk-critical.
 */
window.SkyShield = window.SkyShield || {};

SkyShield.Alerts = (function () {
  var listEl, countEl, riskEls;

  function esc(text) {
    return String(text == null ? '' : text)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;')
      .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  }

  // Accepts both camelCase (mock) and PascalCase (the .NET server).
  function field(obj, camel, pascal) {
    return obj[camel] != null ? obj[camel] : obj[pascal];
  }

  function timeOf(iso) {
    var d = new Date(iso);
    if (isNaN(d)) return '';
    return d.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
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
      + ' <span class="muted">(risk ' + score + ', ' + when + ')</span>'
      + (desc ? '<br><span class="muted">' + esc(desc) + '</span>' : '')
      + '</li>';
  }

  function render() {
    var snap = SkyShield.State.getSnapshot();
    var alerts = snap.alerts;

    // 1. The alerts list
    countEl.textContent = String(alerts.length);
    if (alerts.length === 0) {
      listEl.innerHTML = '<li class="muted empty">No alerts yet.</li>';
    } else {
      listEl.innerHTML = alerts.map(alertHtml).join('');
    }

    // 2. Risk summary: the highest level per aircraft
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

    // Clicking an alert selects the aircraft (highlights it on the map and in the table).
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
