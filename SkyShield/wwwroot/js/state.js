/*
 * state.js — the single source of truth for the dashboard.
 *
 * The map, table, alerts and risk panels do not keep their own lists.
 * They all read from here. That way, a single data update
 * refreshes the whole interface at once.
 */
window.SkyShield = window.SkyShield || {};

SkyShield.State = (function () {
  const MAX_ALERTS = 50;

  // ICAO -> { icao, latitude, longitude, altitudeFeet, groundSpeedKnots, timestampUtc }
  const aircraft = new Map();
  // Alerts, newest first.
  const alerts = [];

  let selectedIcao = null;
  let connection = 'disconnected'; // 'disconnected' | 'connecting' | 'reconnecting' | 'connected'
  let system = 'unknown';          // 'unknown' | 'ok' | 'degraded'
  let lastStatusAt = 0;            // when the last SystemStatus event arrived
  let aircraftTotal = 0;
  let lastMessageAt = null;

  const listeners = new Set();

  // Many updates can arrive in the same moment (one per aircraft): coalesce them
  // so the map, table and panels re-render once instead of dozens of times.
  let notifyTimer = null;
  function notify() {
    if (notifyTimer !== null) return;
    notifyTimer = setTimeout(() => {
      notifyTimer = null;
      listeners.forEach((fn) => {
        try {
          fn();
        } catch (err) {
          console.error('SkyShield: a listener failed', err);
        }
      });
    }, 50);
  }

  // Subscribe to changes; returns an unsubscribe function.
  function onChange(fn) {
    listeners.add(fn);
    return () => listeners.delete(fn);
  }

  function newId() {
    if (window.crypto && typeof window.crypto.randomUUID === 'function') {
      return window.crypto.randomUUID();
    }
    return 'a-' + Date.now() + '-' + Math.random().toString(16).slice(2);
  }

  // We accept both camelCase JSON (the team contract) and PascalCase.
  function normalizeAircraft(raw) {
    return {
      icao: raw.icao ?? raw.Icao ?? '',
      latitude: Number(raw.latitude ?? raw.Latitude ?? NaN),
      longitude: Number(raw.longitude ?? raw.Longitude ?? NaN),
      altitudeFeet: Number(raw.altitudeFeet ?? raw.AltitudeFeet ?? 0),
      groundSpeedKnots: Number(raw.groundSpeedKnots ?? raw.GroundSpeedKnots ?? 0),
      timestampUtc: raw.timestampUtc ?? raw.TimestampUtc ?? new Date().toISOString()
    };
  }

  function isUsable(a) {
    return !!a.icao && Number.isFinite(a.latitude) && Number.isFinite(a.longitude);
  }

  function upsertAircraft(raw) {
    const a = normalizeAircraft(raw);
    if (!isUsable(a)) return null;
    aircraft.set(a.icao, a);
    lastMessageAt = new Date().toISOString();
    notify();
    return a;
  }

  // Replaces everything with the list from GET /api/flights.
  function replaceAllAircraft(list) {
    aircraft.clear();
    (list || []).forEach((raw) => {
      const a = normalizeAircraft(raw);
      if (isUsable(a)) aircraft.set(a.icao, a);
    });
    // The selection does not survive an aircraft that left the list.
    if (selectedIcao && !aircraft.has(selectedIcao)) selectedIcao = null;
    lastMessageAt = new Date().toISOString();
    notify();
  }

  function addAlert(raw) {
    const alert = {
      id: raw.id ?? raw.Id ?? newId(),
      icao: raw.icao ?? raw.Icao ?? '',
      anomalyType: raw.anomalyType ?? raw.AnomalyType ?? '',
      severity: raw.severity ?? raw.Severity ?? 'Info',
      riskScore: Math.max(0, Math.min(100, Number(raw.riskScore ?? raw.RiskScore ?? 0))),
      description: raw.description ?? raw.Description ?? '',
      detectedAtUtc: raw.detectedAtUtc ?? raw.DetectedAtUtc ?? new Date().toISOString(),
      latitude: Number(raw.latitude ?? raw.Latitude ?? NaN),
      longitude: Number(raw.longitude ?? raw.Longitude ?? NaN)
    };
    alerts.unshift(alert);
    if (alerts.length > MAX_ALERTS) alerts.length = MAX_ALERTS;
    lastMessageAt = new Date().toISOString();
    notify();
    return alert;
  }

  // Same thresholds as the backend: Low 0–29, Medium 30–59, High 60–79, Critical 80–100.
  function riskLevel(score) {
    if (score >= 80) return 'critical';
    if (score >= 60) return 'high';
    if (score >= 30) return 'medium';
    return 'low';
  }

  // Number of aircraft per level, using each aircraft's highest alert.
  function riskCounts() {
    const worst = new Map();
    alerts.forEach((a) => {
      worst.set(a.icao, Math.max(worst.get(a.icao) || 0, a.riskScore));
    });
    const counts = { low: 0, medium: 0, high: 0, critical: 0 };
    worst.forEach((score) => {
      counts[riskLevel(score)] += 1;
    });
    return counts;
  }

  function clearAlerts() {
    alerts.length = 0;
    notify();
  }

  // The server reports whether normal monitoring is running.
  function setMonitoring(status) {
    lastStatusAt = Date.now();
    aircraftTotal = Number(status.aircraftCount ?? status.AircraftCount ?? 0);
    const active = status.monitoringActive ?? status.MonitoringActive;
    system = active ? 'ok' : 'degraded';
    notify();
  }

  // If statuses stop arriving while connected, monitoring cannot be trusted.
  function checkStatusFreshness(timeoutMs) {
    if (connection === 'connected' && lastStatusAt && system === 'ok' &&
        Date.now() - lastStatusAt > timeoutMs) {
      system = 'degraded';
      notify();
    }
  }

  // We only select aircraft that are present; a missing one does not clear
  // the current selection (e.g. clicking an alert whose aircraft left the radar).
  function select(icao) {
    if (!aircraft.has(icao)) return;
    selectedIcao = icao;
    notify();
  }

  function getSelected() {
    return selectedIcao ? aircraft.get(selectedIcao) || null : null;
  }

  function setConnection(next) {
    connection = next;
    notify();
  }

  function setSystem(next) {
    system = next;
    notify();
  }

  // A working copy for display: nobody modifies the internal list directly.
  function getSnapshot() {
    return {
      aircraft: Array.from(aircraft.values()),
      alerts: alerts.slice(),
      selectedIcao,
      selected: getSelected(),
      connection,
      system,
      aircraftTotal,
      lastMessageAt,
      risk: riskCounts()
    };
  }

  function clear() {
    aircraft.clear();
    alerts.length = 0;
    selectedIcao = null;
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
    setMonitoring,
    checkStatusFreshness,
    clearAlerts,
    getSnapshot,
    riskLevel,
    clear
  };
})();
