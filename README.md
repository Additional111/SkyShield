# SkyShield – Aircraft Communication Anomaly Detection

SkyShield este un MVP de monitorizare a telemetriei aeronavelor și detectare a anomaliilor.
Totul este **sintetic și local**: nu interacționează cu aeronave sau sisteme reale.

## Componente

| Componentă | Rol |
|---|---|
| `SkyShield` (backend + dashboard) | Primește telemetrie, o validează, rulează regulile de detecție, calculează scorul de risc și trimite rezultatul prin SignalR. Servește și dashboard-ul (`wwwroot`). |
| `SkyShield.Attacker.Cli` | Aplicație de consolă separată. Lansează **manual** scenarii sintetice către interfața de simulare a backend-ului. Nu creează alerte singură. |

Dashboard-ul este doar de monitorizare: nu are butoane de simulare. Niciun scenariu nu pornește automat.

## Pornire (demo)

Necesită .NET 10 SDK.

```powershell
# Terminal 1 – backend + dashboard
dotnet run --project .\SkyShield
# apoi deschide http://localhost:5000
```

Apar ~50 de aeronave sintetice care se mișcă pe hartă; nu există alerte.

```powershell
# Terminal 2 – scenarii manuale
dotnet run --project .\SkyShield.Attacker.Cli -- list
dotnet run --project .\SkyShield.Attacker.Cli -- simulate ghost-aircraft
dotnet run --project .\SkyShield.Attacker.Cli -- simulate unusual-movement
dotnet run --project .\SkyShield.Attacker.Cli -- simulate altitude-anomaly
dotnet run --project .\SkyShield.Attacker.Cli -- simulate duplicate-identity
dotnet run --project .\SkyShield.Attacker.Cli -- status
dotnet run --project .\SkyShield.Attacker.Cli -- reset
```

Opțiuni: `--icao <ICAO>` (alege aeronava), `--url <adresa>` (implicit `http://localhost:5000`, doar local).

| Scenariu | Regulă declanșată | Scor / severitate |
|---|---|---|
| `ghost-aircraft` | GhostAircraftRule | 55 / Medium |
| `unusual-movement` | ImpossibleMovementRule | 85 / Critical |
| `altitude-anomaly` | AltitudeJumpRule | 65 / High |
| `duplicate-identity` | IdentityDuplicationRule (după o mișcare imposibilă) | 95 / Critical |

Scenariile pot fi repetate oricând. `reset` șterge starea temporară de simulare, markerii temporari și alertele din dashboard; monitorizarea normală continuă.

## Fluxul unui scenariu

CLI → `POST /api/simulation/events` → validare → reguli de detecție → scor de risc → răspuns către CLI **și** eveniment `AlertRaised` prin SignalR către dashboard.

Evenimentele de simulare rulează pe un **depozit de stare separat** de traficul normal, deci nu pot corupe pozițiile aeronavelor reale. Interfața de simulare acceptă doar cereri de pe localhost și poate fi dezactivată cu `Simulation:Enabled=false`.

## API

| Endpoint | Descriere |
|---|---|
| `GET /api/flights`, `GET /api/flights/{icao}` | Aeronave urmărite |
| `POST /api/flights` | Ingestie telemetrie (`icao` 6 caractere hex, `latitude`, `longitude`, `altitudeFeet`, `groundSpeedKnots`, `timestampUtc`); 400 cu mesaj clar dacă e invalidă |
| `GET /api/health` | Stare backend și monitorizare |
| `GET /api/simulation/status`, `POST /api/simulation/events`, `POST /api/simulation/reset` | Interfața locală de simulare |
| `/hubs/skyshield` (SignalR) | Evenimente: `AircraftUpdated`, `AlertRaised`, `SystemStatus`, `SimulationReset` |

## Detecție și risc

Regulile sunt independente (`Services/Detection`, înregistrate în `Program.cs`); praguri în `appsettings.json` (`Detection:*`). Scor total pe aeronavă = suma scorurilor, maxim 100.
Niveluri: Low 0–29, Medium 30–59, High 60–79, Critical 80–100.

## Note

- Aeronavele demo se mișcă de `Demo:TimeScale` (implicit 8) ori mai repede decât timpul real, ca mișcarea să fie vizibilă; traficul normal nu trece prin reguli și nu generează alerte.
- Leaflet și clientul SignalR sunt incluse local în `wwwroot/lib`. Doar plăcile hărții (OpenStreetMap) necesită internet; fără ele, aeronavele se văd pe fundal închis.
