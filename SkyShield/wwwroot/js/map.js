/*
 * map.js — harta Leaflet a dashboardului.
 *
 * Nu își ține propriile date: citește snapshot-ul din state.js și
 * desenează câte un punct pentru fiecare aeronavă. Când un punct este
 * apăsat, aeronava devine „selectată".
 */
window.SkyShield = window.SkyShield || {};

SkyShield.Map = (function () {
  const CENTER = [45.9432, 24.9668]; // România, aproximativ în mijloc
  const ZOOM = 6;

  let map = null;
  let fitted = false;          // încadrăm prima dată, apoi lăsăm utilizatorul în pace
  const markers = new Map();   // icao -> marker Leaflet

  // Culorile vin din dashboard.css, ca să existe o singură sursă pentru ele.
  function cssColor(name, fallback) {
    const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    return value || fallback;
  }

  // Cel mai mare risc înregistrat pentru fiecare aeronavă, din alertele curente.
  function highestRisk(alerts) {
    const byIcao = new Map();
    (alerts || []).forEach((a) => {
      if (!a.icao) return;
      byIcao.set(a.icao, Math.max(byIcao.get(a.icao) || 0, a.riskScore || 0));
    });
    return byIcao;
  }

  function styleFor(icao, riskByIcao, selectedIcao) {
    const level = SkyShield.State.riskLevel(riskByIcao.get(icao) || 0);
    const color = level === 'critical' ? cssColor('--critical', '#ef4444')
                : level === 'medium'  ? cssColor('--medium', '#f59e0b')
                : cssColor('--low', '#22c55e');
    const isSelected = icao === selectedIcao;
    return {
      radius: isSelected ? 9 : 6,
      color: isSelected ? cssColor('--accent', '#38bdf8') : color,
      weight: isSelected ? 3 : 2,
      fillColor: color,
      fillOpacity: 0.85
    };
  }

  function popupText(a) {
    return '<strong>' + a.icao + '</strong><br />' +
      'Altitudine: ' + Math.round(a.altitudeFeet).toLocaleString('ro-RO') + ' ft<br />' +
      'Viteză: ' + Math.round(a.groundSpeedKnots) + ' kt';
  }

  function render() {
    if (!map) return;
    const s = SkyShield.State.getSnapshot();
    const riskByIcao = highestRisk(s.alerts);
    const seen = new Set();

    s.aircraft.forEach((a) => {
      seen.add(a.icao);
      const style = styleFor(a.icao, riskByIcao, s.selectedIcao);
      let marker = markers.get(a.icao);

      if (!marker) {
        marker = L.circleMarker([a.latitude, a.longitude], style);
        marker.bindPopup(popupText(a));
        marker.on('click', () => SkyShield.State.select(a.icao));
        marker.addTo(map);
        markers.set(a.icao, marker);
      } else {
        marker.setLatLng([a.latitude, a.longitude]);
        marker.setStyle(style);
        marker.setRadius(style.radius);
        marker.setPopupContent(popupText(a));
      }
    });

    // Aeronave dispărute: scoatem punctele rămase în urmă.
    markers.forEach((marker, icao) => {
      if (!seen.has(icao)) {
        map.removeLayer(marker);
        markers.delete(icao);
      }
    });

    // La primul set de date ne punem singuri în dreptul aeronavelor.
    if (!fitted && s.aircraft.length) {
      const bounds = L.latLngBounds(s.aircraft.map((a) => [a.latitude, a.longitude]));
      map.fitBounds(bounds.pad(0.25));
      fitted = true;
    }
  }

  function init() {
    if (!window.L || !document.getElementById('map')) {
      console.warn('SkyShield: harta nu poate fi pornită (Leaflet lipsește sau div-ul #map nu există).');
      return;
    }
    map = L.map('map', { center: CENTER, zoom: ZOOM });
    // OpenStreetMap: gratuit, fără cheie API — varianta sigură pentru hackathon.
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
      maxZoom: 19
    }).addTo(map);


    SkyShield.State.onChange(render);
    render();
  }

  return { init, render };
})();
