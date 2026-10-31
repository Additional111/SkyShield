/* app.js — starts the map, the initial list and the live connection.
 */
window.SkyShield = window.SkyShield || {};

SkyShield.App = (function () {
  let started = false;
  let demo = false;
  let busy = false;
  let liveConnected = false;

  async function loadFlights() {
    const response = await fetch(SkyShield.Config.flightsUrl, {
      headers: { Accept: 'application/json' },
      signal: AbortSignal.timeout(8000)
    });
    if (!response.ok) throw new Error('Aircraft list: HTTP ' + response.status);
    const flights = await response.json();
    if (!Array.isArray(flights)) throw new Error('GET /api/flights must return a list.');
    return flights;
  }

  function showMode() {
    const label = document.querySelector('.brand-sub');
    if (label) label.textContent = demo
      ? 'Aircraft telemetry monitoring · Simulated data'
      : 'Aircraft telemetry monitoring';
  }

  async function connect() {
    if (busy) return;
    busy = true;
    try {
      // We don't overwrite live messages with an older list.
      if (!liveConnected) {
        const flights = await loadFlights();
        SkyShield.Mock.stop();
        if (demo) SkyShield.State.clear();
        demo = false;
        showMode();
        SkyShield.State.replaceAllAircraft(flights);
        await SkyShield.Live.start();
      }
    } catch (error) {
      console.warn('SkyShield: server unavailable.', error);
      if (SkyShield.Config.mockWhenNoServer && !demo &&
          SkyShield.State.getSnapshot().aircraft.length === 0) {
        demo = true;
        SkyShield.Mock.start();
        showMode();
      }
      // Simulation does not mean a real connection or a healthy system.
      SkyShield.State.setConnection('disconnected');
      SkyShield.State.setSystem('unknown');
    } finally {
      busy = false;
    }
  }

  function start() {
    if (started) return;
    started = true;
    SkyShield.Map.init();
    SkyShield.State.onChange(function () {
      liveConnected = SkyShield.State.getSnapshot().connection === 'connected';
    });
    connect();
    // Retry starting if the server appears later.
    setInterval(function () {
      const status = SkyShield.State.getSnapshot().connection;
      if (!liveConnected && status !== 'connecting') connect();
    }, SkyShield.Config.refreshIntervalMs);
  }

  return { start };
})();

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', SkyShield.App.start);
} else {
  SkyShield.App.start();
}
