/*
 * status.js — the two pills in the header.
 *
 * "Connected / Disconnected" shows the SignalR link state,
 * "System: OK / degraded" shows the overall monitoring state.
 * Both read from SkyShield.State, so they update by themselves.
 */
window.SkyShield = window.SkyShield || {};

SkyShield.Status = (function () {
  var connEl, sysEl;

  var CONN = {
    connected: 'Connected',
    connecting: 'Connecting…',
    disconnected: 'Disconnected'
  };

  var SYS = {
    ok: 'System: OK',
    degraded: 'System: degraded',
    unknown: 'System: unknown'
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
      // 'degraded' uses the warning color, 'unknown' stays gray.
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
