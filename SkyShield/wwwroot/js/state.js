/*
 * state.js — singura sursă de adevăr a dashboardului.
 *
 * Toate datele (aeronave, alerte, selecția, starea conexiunii) stau aici.
 * Celelalte fișiere (map, table, alerts, details) doar citesc de aici
 * și se redesenează când ceva se schimbă.
 */
window.SkyShield = window.SkyShield || {};

SkyShield.State = (function () {
  // Câte alerte păstrăm în listă (cele mai noi primele).
  const MAX_ALERTS = 50;

  // Datele interne.
  let aircraft = new Map();   // icao -> obiect aeronavă
  let alerts = [];            // listă de alerte, cea mai nouă prima
  let selectedIcao = null;    // icao-ul aeronavei selectate (sau null)
  let connection = 'offline'; // 'offline' | 'connecting' | 'connected'
  let system = 'ok';          // 'ok' | 'degraded'

  // Cine vrea să fie anunțat când se schimbă ceva (hartă, tabel etc.).
  const listeners = new Set();

  function notify() {
    listeners.forEach((fn) => {
      try { fn(); } catch (e) { console.error('Eroare într-un listener:', e); }
    });
  }

  // Un modul se abonează la schimbări; primește înapoi o funcție de dezabonare.
  function onChange(fn) {
    listeners.add(fn);
    return () => listeners.delete(fn);
  }

  // Acceptăm și câmpuri camelCase (JS), și PascalCase (cum vin din C#).
  function normalizeAircraft(raw) {
    return {
      icao: raw.icao ?? raw.Icao ?? '',
      latitude: raw.latitude ?? raw.Latitude ?? 0,
      longitude: raw.longitude ?? raw.Longitude ?? 0,
      altitudeFeet: raw.altitudeFeet ?? raw.AltitudeFeet ?? 0,
      groundSpeedKnots: raw.groundSpeedKnots ?? raw.GroundSpeedKnots ?? 0,
      timestampUtc: raw.timestampUtc ?? raw.TimestampUtc ?? null
    };
  }

  function normalizeAlert(raw) {
    return {
      id: raw.id ?? raw.Id ?? String(Date.now()) + Math.random(),
      icao: raw.icao ?? raw.Icao ?? '',
      anomalyType: raw.anomalyType ?? raw.AnomalyType ?? 'Unknown',
      severity: raw.severity ?? raw.Severity ?? 'Low',
      riskScore: raw.riskScore ?? raw.RiskScore ?? 0,
      description: raw.description ?? raw.Description ?? '',
      detectedAtUtc: raw.detectedAtUtc ?? raw.DetectedAtUtc ?? null,
      latitude: raw.latitude ?? raw.Latitude ?? null,
      longitude: raw.longitude ?? raw.Longitude ?? null
    };
  }

  // Adaugă sau actualizează o aeronavă.
  function upsertAircraft(raw) {
    const a = normalizeAircraft(raw);
    if (!a.icao) return;
    aircraft.set(a.icao, a);
    notify();
  }

  // Înlocuiește toată lista (folosit la încărcarea inițială).
  function replaceAllAircraft(list) {
    aircraft = new Map();
    (list || []).forEach((raw) => {
      const a = normalizeAircraft(raw);
      if (a.icao) aircraft.set(a.icao, a);
    });
    notify();
  }

  // Adaugă o alertă nouă la începutul listei.
  function addAlert(raw) {
    const alert = normalizeAlert(raw);
    alerts.unshift(alert);
    if (alerts.length > MAX_ALERTS) alerts.length = MAX_ALERTS;
    notify();
  }

  // Selectează (sau deselectează cu null) o aeronavă.
  function select(icao) {
    selectedIcao = icao;
    notify();
  }

  function getSelected() {
    return selectedIcao ? aircraft.get(selectedIcao) || null : null;
  }

  function setConnection(value) {
    connection = value;
    notify();
  }

  function setSystem(value) {
    system = value;
    notify();
  }

  // O copie a datelor, ca nimeni să nu le modifice din greșeală.
  function getSnapshot() {
    return {
      aircraft: Array.from(aircraft.values()),
      alerts: alerts.slice(),
      selectedIcao,
      connection,
      system
    };
  }

  // Nivelul de risc după scor: 0-29 scăzut, 30-69 mediu, 70-100 critic.
  function riskLevel(score) {
    if (score >= 70) return 'critical';
    if (score >= 30) return 'medium';
    return 'low';
  }

  // Golește tot (folosit la teste).
  function clear() {
    aircraft = new Map();
    alerts = [];
    selectedIcao = null;
    connection = 'offline';
    system = 'ok';
    notify();
  }

  return {
    onChange,
    upsertAircraft,
    replaceAllAircraft,
    addAlert,
    select,
    getSelected,
    setConnection,
    setSystem,
    getSnapshot,
    riskLevel,
    clear
  };
})();
