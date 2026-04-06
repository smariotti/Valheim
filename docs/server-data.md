# TrophyHuntMod — Data Sent to Server

All network calls go to `https://valheim.help`. Data is only sent when **all** of the following are true:
- Player is logged in with Discord
- A tournament is currently **Live** and has not ended
- The game mode is not CasualSaga, CulinarySaga, or TrophyFiesta
- The run has not been flagged as invalid for tournament play (e.g. cheats used)

### End-of-tournament final flush

When `DateTime.Now >= endAt`, a one-shot final transmission fires regardless of tournament status (the live/ended check is bypassed). It sends, in order:

1. **Player snapshot** — final health, stamina, food, skills, equipment, all kill/drop counts
2. **Remaining path points** — any points collected since the last 30-second batch
3. **Full event log** — complete session log via `POST /api/track/logs`

This ensures the server has a complete picture of the player's state at the exact moment the tournament closes, even if the player is still in-game. Fires at most once per session.

The snapshot and path flush both bypass the normal `CanPostToTracker()` guard (which rejects calls after `endAt`). This matters if the player dies near the end of the tournament — the death screen does not clear `Player.m_localPlayer`, so the snapshot is still captured and sent in the dead state (health will read 0).

---

## Endpoints

### GET `/api/track/standings`

Fetches the current tournament standings. Called every **30 seconds**.

**Query parameters:**

| Param  | Value                        |
|--------|------------------------------|
| `seed` | World seed string            |
| `mode` | Game mode name (e.g. `TrophyRush`) |

**Response fields used:**

| Field     | Type   | Description                          |
|-----------|--------|--------------------------------------|
| `name`    | string | Tournament event name                |
| `mode`    | string | Tournament game mode                 |
| `startAt` | string | Start time (UTC ISO 8601)            |
| `endAt`   | string | End time (UTC ISO 8601)              |
| `status`  | int    | `0` = NotRunning, `20` = Live, `30` = Over |
| `players` | array  | Array of `{ name, id, avatarUrl, score }` |

---

### POST `/api/track/log`

All game events and player snapshots go through this single endpoint as batched `code` strings. Called at most every **30 seconds**; scoring events flush immediately.

**JSON body (`TrackLogEntry`):**

| Field   | Type   | Description                             |
|---------|--------|-----------------------------------------|
| `id`    | string | Player's Discord ID                     |
| `seed`  | string | World seed                              |
| `score` | int    | Player's current score at time of batch |
| `code`  | string | One or more events (see format below)   |

#### Event format

Each event in the `code` string follows this pattern:

```
<tag>=<secs>@<x>,<y>,<z>[|<extra>];
```

- `tag` — single letter identifying the event type (see table below)
- `secs` — seconds elapsed since tournament `startAt` (integer)
- `x,y,z` — world position rounded to nearest integer
- `extra` — optional, pipe-delimited payload specific to the event type
- `;` — event separator; multiple events concatenated in one `code` string

#### Tag reference

| Tag | Event | `extra` | Flush |
|-----|-------|---------|-------|
| `F` | First real input after landing | *(none)* | No |
| `W` | Waypoint (movement sample) | *(none)* | No |
| `P` | Player snapshot | `h:cur/max\|s:cur/max[\|f:...]\|sk:...]\|eq:...]\|kd:...]` | No |
| `J` | Path jump (portal or respawn) | `Portal`, `Portal=<name>`, or `Respawn` | No |
| `T` | Trophy picked up | Mob name (Trophy prefix stripped) + optional bonuses, e.g. `Neck` or `Neck\|BonusMeadows` or `Fader\|BonusAshlands\|BonusAll\|BonusTime=840` | **Yes** |
| `D` | Penalty: death | *(none)* | **Yes** |
| `L` | Penalty: logout | *(none)* | **Yes** |
| `S` | Penalty: `/slashdie` | *(none)* | **Yes** |

**Flush = Yes** means the batch is sent immediately when this event occurs rather than waiting for the 30-second cadence.

#### `W` waypoint sampling

A coroutine fires every **5 seconds** and records a `W` event if the player has moved at least **10 metres** from the most recently recorded position (any tag, not just `W`). This means trophy pickups, deaths, portal uses, and snapshots all satisfy the distance check — `W` events only fill in the gaps between those.

The result is a continuous, dense position trace at roughly 10m resolution. All event tags share the same `__m_pendingEvents` list in chronological order, so the server sees a single interleaved stream.

#### Why `J` events matter for reconstructing the path

Without an explicit jump marker, a sudden position discontinuity in the `W` stream is ambiguous — it could be a portal, a death respawn, or simply a gap in recording. `J` events resolve this:

- **Initial spawn** — the first `F` event records the landing position. There is no preceding `W` history, so no jump is implied; the path simply starts here.
- **Portal use** — a `J|Portal` (or `J|Portal=<name>`) is inserted at the exit position *immediately* when the teleport fires, before the next `W` sample. The server can draw a line from the last pre-portal `W` to the portal entry, then restart the path at the `J` position.
- **Death respawn** — a `J|Respawn` is inserted at the spawn point when `OnSpawned` fires and `__m_firstInputDetected` is already true (meaning this is not the initial spawn). The `D` event at the moment of death provides the position where the player died; the `J|Respawn` provides the position where they wake up. The gap between them represents the respawn teleport.

Without `J`, the path would show a straight line jumping across the map between two `W` samples, making it impossible to distinguish legitimate fast travel from suspicious position changes.

#### Deduplication (applied before recording)

- `T`: first occurrence per unique trophy name only
- `D`, `L`, `S`: per unique position (within 5 units) — prevents double-counting rapid re-fires
- `J`: no deduplication — every portal use and respawn is always recorded
- `F`: once per session
- `W`: implicit deduplication via the 10m distance threshold

#### `P` snapshot extra fields

| Section | Format | Description |
|---------|--------|-------------|
| `h`     | `cur/max` | Health (rounded) |
| `s`     | `cur/max` | Stamina (rounded) |
| `f`     | `item,item,...` | Active food items (omitted if none) |
| `sk`    | `Skill:level,...` | Skills with level ≥ 1 |
| `eq`    | `slot=item,...` | Equipped items by slot (omitted if empty) |
| `kd`    | `enemy:kills/trophies,...` | Kill/drop counts; "Trophy" prefix stripped |

Equipment slot keys: `R` = right hand, `L` = left hand, `H` = helmet, `C` = chest, `G` = legs, `S` = shoulder, `U` = utility

P snapshots are sent every **5 minutes** and at end-of-tournament.

#### Examples

Trophy pickup (immediate flush):
```json
{ "id": "185432167890123456", "seed": "Niffelheim", "score": 340, "code": "T=125@203,31,14|Boar;" }
```

Trophy completing a biome (immediate flush):
```json
{ "id": "185432167890123456", "seed": "Niffelheim", "score": 680, "code": "T=240@203,31,14|Neck|BonusMeadows;" }
```

Final trophy with all bonuses (immediate flush):
```json
{ "id": "185432167890123456", "seed": "Niffelheim", "score": 5040, "code": "T=840@-1402,18,887|Bonemaw|BonusAshlands|BonusAll|BonusTime=840;" }
```

Death penalty (immediate flush):
```json
{ "id": "185432167890123456", "seed": "Niffelheim", "score": 290, "code": "D=180@88,22,-540;" }
```

Portal use followed by respawn in a 30-second batch:
```json
{ "id": "185432167890123456", "seed": "Niffelheim", "score": 680, "code": "J=300@142,32,-87|Portal=base;J=420@-312,45,201|Respawn;" }
```

Periodic batch with FirstInput, waypoints, and a snapshot:
```json
{ "id": "185432167890123456", "seed": "Niffelheim", "score": 0, "code": "F=5@0,32,0;W=10@14,32,18;W=15@28,32,35;W=20@44,31,52;P=305@450,25,800|h:87/90|s:150/150|f:SerpentStew|sk:Swords:42,Run:38|eq:R=SwordBronze,L=ShieldBronze|kd:Boar:12/3,Neck:8/1;" }
```

Death followed by respawn jump:
```json
{ "id": "185432167890123456", "seed": "Niffelheim", "score": 240, "code": "W=310@88,22,-540;D=312@88,22,-540;J=340@0,32,0|Respawn;" }
```

---

### POST `/api/track/map`

Uploads the three world terrain texture cache files as a single multipart request. Only sent when the standings response includes `mapLoaded: false`, meaning no client has uploaded for this seed yet. First client to successfully complete the pre-upload standings re-check wins; subsequent clients see `mapLoaded: true` and skip.

**Trigger flow:**
1. Standings poll returns `mapLoaded: false`
2. Mod reads the 3 cache files from disk
3. Immediately re-fetches standings — if now `mapLoaded: true`, aborts (another client won the race)
4. If still `false`, POSTs all 3 files atomically

**Why one call:** Sending all files in a single multipart request lets the server set `mapLoaded=true` in one transaction. Three separate calls would leave a window between uploads where another client sees `mapLoaded=false` and starts a duplicate upload.

**Multipart form fields:**

| Field        | Type   | Description                          |
|--------------|--------|--------------------------------------|
| `id`         | string | Player's Discord ID                  |
| `seed`       | string | World seed                           |
| `mode`       | string | Game mode name                       |
| `heightTex`  | file   | `<worldId>_heightTexCache` (binary)  |
| `mapTex`     | file   | `<worldId>_mapTexCache` (binary)     |
| `forestTex`  | file   | `<worldId>_forestMaskTexCache` (binary) |

Files are located at `%APPDATA%\..\LocalLow\IronGate\Valheim\worlds_local\`.

---

### GET `/api/track/standings` — updated response

Added field:

| Field       | Type | Description                                              |
|-------------|------|----------------------------------------------------------|
| `mapLoaded` | bool | `false` = no client has uploaded tex caches for this seed yet |

---

