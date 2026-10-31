/*
 * map.js — the dashboard's Leaflet map.
 *
 * It doesn't keep its own data: it reads the snapshot from state.js and
 * draws one dot per aircraft. When a dot is clicked, the aircraft
 * becomes "selected".
 */
window.SkyShield = window.SkyShield || {};

SkyShield.Map = (function () {
  const CENTER = [45.9432, 24.9668]; // Romania, roughly in the middle
  const ZOOM = 6;

  let map = null;
  let fitted = false;          // we frame the aircraft once, then leave the user alone
  const markers = new Map();   // icao -> Leaflet marker

  // Colors come from dashboard.css, so there is a single source for them.
  function cssColor(name, fallback) {
    const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    return value || fallback;
  }

  // The highest recorded risk for each aircraft, from the current alerts.
  function highestRisk(alerts) {
    const byIcao = new Map();
    (alerts || []).forEach((a) => {
      if (!a.icao) return;
      byIcao.set(a.icao, Math.max(byIcao.get(a.icao) || 0, a.riskScore || 0));
    });
    return byIcao;
  }

  function styleFor(icao, riskByIcao, selectedIcao) {
    const hasAlert = riskByIcao.has(icao);
    const level = hasAlert ? SkyShield.State.riskLevel(riskByIcao.get(icao)) : null;
    const color = !hasAlert ? cssColor('--no-risk', '')
                : level === 'critical' ? cssColor('--critical', '')
                : level === 'medium'  ? cssColor('--medium', '')
                : cssColor('--low', '');
    const border = hasAlert ? color : cssColor('--no-risk-border', '');
    const isSelected = icao === selectedIcao;
    return {
      radius: isSelected ? 9 : 6,
      color: isSelected ? cssColor('--accent', '') : border,
      weight: isSelected ? 3 : 2,
      fillColor: color,
      fillOpacity: 0.85
    };
  }

  function popupText(a) {
    return '<strong>' + a.icao + '</strong><br />' +
      'Altitude: ' + Math.round(a.altitudeFeet).toLocaleString('en-US') + ' ft<br />' +
      'Ground speed: ' + Math.round(a.groundSpeedKnots) + ' kt';
  }

  function render() {
    if (!map) return;
    const s = SkyShield.State.getSnapshot();

    const mapCountEl = document.querySelector('.panel-map .panel-head span') 
                    || document.querySelector('.map-panel .panel-head span')
                    || document.querySelector('.map-panel span')
                    || document.getElementById('map-aircraft-count');
    if (mapCountEl) {
      const count = s.aircraft.length;
      const label = count === 1 ? 'aircraft' : 'aircrafts';
      mapCountEl.textContent = `${count} ${label}`;
    }

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

    // Aircraft that disappeared: remove the leftover dots.
    markers.forEach((marker, icao) => {
      if (!seen.has(icao)) {
        map.removeLayer(marker);
        markers.delete(icao);
      }
    });

    // On the first data set we frame the aircraft ourselves.
    if (!fitted && s.aircraft.length) {
      const bounds = L.latLngBounds(s.aircraft.map((a) => [a.latitude, a.longitude]));
      map.fitBounds(bounds.pad(0.25));
      fitted = true;
    }
  }

  function init() {
    if (map) return;

    const container = document.getElementById('map');

    if (!window.L || !container) {
      console.warn('SkyShield: the map cannot start (Leaflet is missing or the #map div does not exist).');
      return;
    }

    if (container._leaflet_id) {
      console.warn('SkyShield: map container already initialized.');
      return;
    }

    map = L.map(container, { center: CENTER, zoom: ZOOM });

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
      maxZoom: 19
    }).addTo(map);

    SkyShield.State.onChange(render);
    render();
  }

  return { init, render };
})();
