/* signalr.js — receives live updates from the team server.
 * Dependencies: the Microsoft SignalR client, config.js and state.js.
 * It does not start by itself: app.js calls SkyShield.Live.start().
 */
window.SkyShield = window.SkyShield || {};

SkyShield.Live = (function () {
  let connection = null;
  let pendingStart = null;

  function buildConnection() {
    if (!window.signalR) {
      throw new Error('The Microsoft SignalR client is not loaded.');
    }

    const config = SkyShield.Config;
    const hub = new signalR.HubConnectionBuilder()
      .withUrl(config.hubUrl)
      .withAutomaticReconnect(config.reconnectDelaysMs)
      .build();

    hub.on(config.events.aircraftUpdated, function (aircraft) {
      if (aircraft && typeof aircraft === 'object') {
        SkyShield.State.upsertAircraft(aircraft);
      }
    });

    hub.on(config.events.alertRaised, function (alert) {
      if (alert && typeof alert === 'object') {
        SkyShield.State.addAlert(alert);
      }
    });

    hub.on(config.events.systemStatus, function (status) {
      if (status && typeof status === 'object') {
        SkyShield.State.setMonitoring(status);
      }
    });

    // `skyshield-cli reset`: drop temporary aircraft and old alerts, keep normal traffic.
    hub.on(config.events.simulationReset, function (payload) {
      if (payload && (payload.clearAlerts ?? payload.ClearAlerts)) {
        SkyShield.State.clearAlerts();
      }
      if (SkyShield.App && SkyShield.App.reloadAircraft) SkyShield.App.reloadAircraft();
    });

    hub.onreconnecting(function () {
      SkyShield.State.setConnection('reconnecting');
      SkyShield.State.setSystem('unknown');
    });

    hub.onreconnected(function () {
      SkyShield.State.setConnection('connected');
      // The next SystemStatus event confirms monitoring; refresh what we missed.
      SkyShield.State.setSystem('unknown');
      if (SkyShield.App && SkyShield.App.reloadAircraft) SkyShield.App.reloadAircraft();
    });

    hub.onclose(function () {
      SkyShield.State.setConnection('disconnected');
      SkyShield.State.setSystem('unknown');
    });

    return hub;
  }

  async function start() {
    if (pendingStart) return pendingStart;
    if (connection && connection.state === signalR.HubConnectionState.Connected) return;

    SkyShield.State.setConnection('connecting');
    SkyShield.State.setSystem('unknown');

    pendingStart = (async function () {
      try {
        // A permanently closed connection cannot be restarted: rebuild it.
        if (!connection || connection.state === signalR.HubConnectionState.Disconnected) {
          connection = buildConnection();
        }
        await connection.start();
        SkyShield.State.setConnection('connected');
      } catch (error) {
        SkyShield.State.setConnection('disconnected');
        SkyShield.State.setSystem('unknown');
        throw error;
      }
    })();

    try {
      await pendingStart;
    } finally {
      pendingStart = null;
    }
  }

  async function stop() {
    if (pendingStart) {
      try { await pendingStart; } catch { /* The start already failed. */ }
    }
    if (connection) await connection.stop();
    SkyShield.State.setConnection('disconnected');
    SkyShield.State.setSystem('unknown');
  }

  return { start, stop };
})();
