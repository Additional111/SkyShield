/*
 * status.js — cele două pastile din header.
 *
 * „Conectat / Deconectat" arată starea legăturii SignalR,
 * „Sistem: OK / degradat" arată starea generală a monitorizării.
 * Ambele citesc din SkyShield.State, deci se actualizează singure.
 */
window.SkyShield = window.SkyShield || {};

SkyShield.Status = (function () {
  var connEl, sysEl;

  var CONN = {
    connected: 'Conectat',
    connecting: 'Se conectează…',
    disconnected: 'Deconectat'
  };

  var SYS = {
    ok: 'Sistem: OK',
    degraded: 'Sistem: degradat',
    unknown: 'Sistem: necunoscut'
  };

  function label(el, text) {
    var span = el.querySelector('.label');
    if (span) span.textContent = text;
    else el.textContent = text;
  }

  function render() {
    var snap = SkyShield.State.getSnapshot();

    if (connEl) {
      connEl.setAttribute('data-state', snap.connection);
      label(connEl, CONN[snap.connection] || snap.connection);
    }

    if (sysEl) {
      // 'degraded' folosește culoarea de avertizare, 'unknown' rămâne gri.
      sysEl.setAttribute('data-state', snap.system === 'degraded' ? 'warning' : snap.system);
      label(sysEl, SYS[snap.system] || snap.system);
    }
  }

  function init() {
    connEl = document.getElementById('connection-status');
    sysEl = document.getElementById('system-status');
    if (!connEl && !sysEl) return;
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
