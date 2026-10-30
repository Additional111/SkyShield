/*
 * config.js — adresa backendului, într-un singur loc.
 *
 * Când echipa schimbă portul sau gazda serverului, se modifică aici
 * și atât. Niciun alt fișier JS nu mai conține adrese.
 */
window.SkyShield = window.SkyShield || {};

SkyShield.Config = (function () {
  // Gol = pagina e servită de același server (.NET), deci nu avem nevoie de adresă.
  // Dacă deschizi dashboardul din alt loc, pune aici adresa serverului,
  // de exemplu: 'http://localhost:5000'
  const apiBaseUrl = '';

  return {
    apiBaseUrl,

    // Datele inițiale, încărcate o singură dată la pornire.
    flightsUrl: apiBaseUrl + '/api/flights',

    // Conectarea live (SignalR).
    hubUrl: apiBaseUrl + '/hubs/skyshield',

    // Numele evenimentelor anunțate de server.
    events: {
      aircraftUpdated: 'AircraftUpdated',
      alertRaised: 'AlertRaised'
    },

    // Cât așteaptă între încercările de reconectare (milisecunde).
    reconnectDelaysMs: [0, 2000, 5000, 10000],

    // Din cât în cât se reia lista completă, ca siguranță dacă s-a pierdut un mesaj.
    refreshIntervalMs: 15000,

    // Dacă serverul nu răspunde, se folosesc date de test ca să se vadă dashboardul.
    mockWhenNoServer: true
  };
})();
