/*
 * mock-data.js — test data, used only while the backend is not ready.
 *
 * Its role: make the dashboard look alive (map, alerts, risk) without a server.
 * app.js decides whether this data or the real data from GET /api/flights is used.
 */
window.SkyShield = window.SkyShield || {};

SkyShield.Mock = (function () {
  // A few starting aircraft, around Romania.
  const BASE = [
    { icao: '4A2B1C', latitude: 44.4170, longitude: 26.0995, altitudeFeet: 35000, groundSpeedKnots: 460 },
    { icao: '4A3D5E', latitude: 45.9432, longitude: 24.9668, altitudeFeet: 38000, groundSpeedKnots: 480 },
    { icao: '4A5F7A', latitude: 47.1580, longitude: 27.5890, altitudeFeet: 31000, groundSpeedKnots: 430 },
    { icao: '4A6B8C', latitude: 45.7480, longitude: 21.2330, altitudeFeet: 12000, groundSpeedKnots: 250 },
    { icao: '4A7D9E', latitude: 43.9900, longitude: 28.6000, altitudeFeet: 4000,  groundSpeedKnots: 90 }
  ];

  // Starting alerts: one medium risk, one critical risk.
  const ALERTS = [
    { icao: '4A6B8C', anomalyType: 'SpeedDrop', severity: 'Medium', riskScore: 45,
      description: 'Sudden speed drop compared with the route average.', latitude: 45.748, longitude: 21.233 },
    { icao: '4A7D9E', anomalyType: 'UnexpectedDescent', severity: 'Critical', riskScore: 82,
      description: 'Unexpected descent over the maritime area.', latitude: 43.99, longitude: 28.60 }
  ];

  // Alerts that appear from time to time, so the panel is not static.
  const POOL = [
    { anomalyType: 'AltitudeDeviation', severity: 'Medium', riskScore: 38, description: 'Deviation from the reported altitude.' },
    { anomalyType: 'RouteDeviation', severity: 'Low', riskScore: 22, description: 'Minor deviation from the planned corridor.' },
    { anomalyType: 'TransponderLoss', severity: 'Critical', riskScore: 88, description: 'Transponder signal lost.' }
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

  // We move the aircraft a little every 2 seconds, so the map looks alive.
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

  // Every 10 seconds it adds a new alert, taken from the pool.
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

  let movementTimer = null;
  let alertTimer = null;

  function start() {
    if (movementTimer !== null) return;
    install();
    movementTimer = setInterval(tick, 2000);
    alertTimer = setInterval(addRandomAlert, 10000);
  }

  function stop() {
    clearInterval(movementTimer);
    clearInterval(alertTimer);
    movementTimer = null;
    alertTimer = null;
  }

  return { install, start, stop };
})();
