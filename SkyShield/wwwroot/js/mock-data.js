/*
 * mock-data.js — date de test, folosite doar cât backend-ul nu e gata.
 *
 * Rolul lui: să arate dashboardul viu (hartă, alerte, risc) fără server.
 * Când app.js va fi gata, el hotărăște dacă se folosesc aceste date
 * sau cele reale de la GET /api/flights.
 */
window.SkyShield = window.SkyShield || {};

SkyShield.Mock = (function () {
  // Câteva aeronave de pornire, în jurul României.
  const BASE = [
    { icao: '4A2B1C', latitude: 44.4170, longitude: 26.0995, altitudeFeet: 35000, groundSpeedKnots: 460 },
    { icao: '4A3D5E', latitude: 45.9432, longitude: 24.9668, altitudeFeet: 38000, groundSpeedKnots: 480 },
    { icao: '4A5F7A', latitude: 47.1580, longitude: 27.5890, altitudeFeet: 31000, groundSpeedKnots: 430 },
    { icao: '4A6B8C', latitude: 45.7480, longitude: 21.2330, altitudeFeet: 12000, groundSpeedKnots: 250 },
    { icao: '4A7D9E', latitude: 43.9900, longitude: 28.6000, altitudeFeet: 4000,  groundSpeedKnots: 90 }
  ];

  // Alerte de pornire: una de risc mediu, una de risc critic.
  const ALERTS = [
    { icao: '4A6B8C', anomalyType: 'SpeedDrop', severity: 'Medium', riskScore: 45,
      description: 'Scădere bruscă de viteză față de media traseului.', latitude: 45.748, longitude: 21.233 },
    { icao: '4A7D9E', anomalyType: 'UnexpectedDescent', severity: 'Critical', riskScore: 82,
      description: 'Coborâre neașteptată deasupra zonei maritime.', latitude: 43.99, longitude: 28.60 }
  ];

  // Alerte care mai apar din când în când, ca panoul să nu fie static.
  const POOL = [
    { anomalyType: 'AltitudeDeviation', severity: 'Medium', riskScore: 38, description: 'Abatere de la altitudinea declarată.' },
    { anomalyType: 'RouteDeviation', severity: 'Low', riskScore: 22, description: 'Ieșire ușoară din corridorul planificat.' },
    { anomalyType: 'TransponderLoss', severity: 'Critical', riskScore: 88, description: 'Semnal de transponder pierdut.' }
  ];

  function install() {
    const now = Date.now();
    SkyShield.State.replaceAllAircraft(BASE.map((a, i) => ({
      ...a,
      timestampUtc: new Date(now - i * 4000).toISOString()
    })));
    ALERTS.forEach((a) => SkyShield.State.addAlert({
      ...a,
      detectedAtUtc: new Date(now - 60000).toISOString()
    }));
  }

  // Mișcăm puțin aeronavele la fiecare 2 secunde, ca harta să pară vie.
  function tick() {
    const s = SkyShield.State.getSnapshot();
    s.aircraft.forEach((a) => {
      SkyShield.State.upsertAircraft({
        icao: a.icao,
        latitude: a.latitude + (Math.random() - 0.5) * 0.04,
        longitude: a.longitude + (Math.random() - 0.5) * 0.06,
        altitudeFeet: a.altitudeFeet,
        groundSpeedKnots: a.groundSpeedKnots,
        timestampUtc: new Date().toISOString()
      });
    });
  }

  // Din 10 în 10 secunde adaugă o alertă nouă, luată din pool.
  let poolIndex = 0;
  function addRandomAlert() {
    const s = SkyShield.State.getSnapshot();
    if (!s.aircraft.length) return;
    const a = s.aircraft[Math.floor(Math.random() * s.aircraft.length)];
    const t = POOL[poolIndex % POOL.length];
    poolIndex += 1;
    SkyShield.State.addAlert({
      icao: a.icao,
      anomalyType: t.anomalyType,
      severity: t.severity,
      riskScore: t.riskScore,
      description: t.description,
      latitude: a.latitude,
      longitude: a.longitude,
      detectedAtUtc: new Date().toISOString()
    });
  }

  function start() {
    install();
    SkyShield.State.setConnection('connected');
    SkyShield.State.setSystem('ok');
    // Pornirea hărții se mută în app.js când îl scriem.
    if (SkyShield.Map && typeof SkyShield.Map.init === 'function') SkyShield.Map.init();
    setInterval(tick, 2000);
    setInterval(addRandomAlert, 10000);
  }

  return { install, start };
})();

// La încărcare, dacă serverul nu e de față, pornim direct cu datele de test.
if (SkyShield.Config.mockWhenNoServer) {
  SkyShield.Mock.start();
}
