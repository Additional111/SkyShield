/*
 * config.js — Backend address.
 * Modify here if we change the port.
 */
window.SkyShield = window.SkyShield || {};

SkyShield.Config = (function () {
  // We add here when we finish the app: 'http://localhost:5000'
  const apiBaseUrl = ''; // same origin as the backend (http://localhost:5000)

  return {
    apiBaseUrl,

    // Initial data.
    flightsUrl: apiBaseUrl + '/api/flights',

    // Live connection (SignalR).
    hubUrl: apiBaseUrl + '/hubs/skyshield',

    // Name of events on the server.
    events: {
      aircraftUpdated: 'AircraftUpdated',
      alertRaised: 'AlertRaised',
      systemStatus: 'SystemStatus',
      simulationReset: 'SimulationReset'
    },

    // Between retrying to connect (miliseconds).
    reconnectDelaysMs: [0, 2000, 5000, 10000],

    // Sometimes, it loads initial list.
    refreshIntervalMs: 15000,

    // Offline preview with fake aircraft (off by default: the real backend generates the traffic).
    mockWhenNoServer: false,

    // Normal monitoring is shown as degraded if no status arrives for this long.
    statusTimeoutMs: 10000
  };
})();
