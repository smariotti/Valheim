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

Sends a single event. Used for game events as they happen, path updates, and player snapshots.

**JSON body (`TrackLogEntry`):**

| Field   | Type   | Description                             |
|---------|--------|-----------------------------------------|
| `id`    | string | Player's Discord ID                     |
| `seed`  | string | World seed                              |
| `score` | int    | Player's current score at time of event |
| `code`  | string | Event payload (see event types below)   |

#### `code` values

**Game events** — sent immediately when the event occurs. World position is embedded in `code` as a `@x,y,z` suffix (coordinates rounded to integer).

| Event type | `code` value | Deduplication |
|------------|--------------|---------------|
| First real input after landing | `FirstInput` | Once per session |
| Trophy picked up | Trophy prefab name (e.g. `TrophyBoar`) | First occurrence per unique trophy name |
| Bonus event(s) | Pipe-delimited composite (see below) | One event per trophy pickup that triggers bonuses |
| Penalty: logout | `PenaltyLogout` | Per unique position (within 5 units) |
| Penalty: death | `PenaltyDeath` | Per unique position (within 5 units) |
| Penalty: `/slashdie` | `PenaltySlashDie` | Per unique position (within 5 units) |
| Portal used | `Portal` (unnamed) or `Portal:<tag>` | Per unique position (within 5 units) |

`FirstInput` fires once after the fly-in cinematic ends and the player presses any key, moves, or clicks. It marks the moment the player actually took control and began their run. Position is the player's location at the standing stones.

**Bonus event format** — all bonuses triggered by a single trophy pickup are combined into one `code` string, pipe-delimited:

| Segment | When present | Example |
|---------|-------------|---------|
| `Bonus<Biome>` | The pickup completed a biome set | `BonusMeadows` |
| `BonusAll` | The pickup completed every trophy | `BonusAll` |
| `BonusTime:<score>` | Mode is TrophyBlitz or TrophyTrailblazer and BonusAll fired | `BonusTime:420` |

Examples:
- Biome complete only: `BonusMeadows`
- Biome + all trophies (non-time-bonus mode): `BonusMeadows|BonusAll`
- Biome + all trophies + time bonus: `BonusMeadows|BonusAll|BonusTime:420`
- All trophies in Saga mode (no biome bonus tracked): `BonusAll`

Whitelisted **items**: `RoundLog`, `Finewood`, `ElderBark`, `SpearFlint`/2/3/4, `ArmorTrollLeatherChest`/2/3, `ArmorRootChest`/2

Whitelisted **builds**: `$piece_workbench`, `$piece_sapling_turnip`, `$piece_sapling_onion`, `$piece_bonfire`

#### Examples

Plain trophy pickup (no bonus triggered):
```json
{ "id": "185432167890123456", "seed": "Niffelheim", "score": 340, "code": "TrophyBoar@142,32,-87" }
```

Trophy that completes a biome (bonus combined into same event):
```json
{ "id": "185432167890123456", "seed": "Niffelheim", "score": 680, "code": "TrophyNeck|BonusMeadows@203,31,14" }
```

Final Trophy that completes everything early
```json
{ "id": "185432167890123456", "seed": "Niffelheim", "score": 5040, "code": "TrophyBonemaw|BonusAshlands|BonusAll|BonusTime:840@-1402,18,887" }
```

Death penalty:
```json
{ "id": "185432167890123456", "seed": "Niffelheim", "score": 290, "code": "PenaltyDeath@-312,45,201" }
```

Second death at a different location (both are logged):
```json
{ "id": "185432167890123456", "seed": "Niffelheim", "score": 240, "code": "PenaltyDeath@88,22,-540" }
```

Logout penalty:
```json
{ "id": "185432167890123456", "seed": "Niffelheim", "score": 240, "code": "PenaltyLogout@88,22,-540" }
```

Path update (points embedded in `code`, no position suffix):
```json
{ "id": "185432167890123456", "seed": "Niffelheim", "score": 340, "code": "Path=0:142,32,-87;8:155,32,-91;16:178,31,-104" }
```

Player snapshot (no position suffix):
```json
{ "id": "185432167890123456", "seed": "Niffelheim", "score": 680, "code": "Snap=h:87/90|s:150/150|f:SerpentStew,MisthareSupreme,MushroomOmelette|sk:Swords:42,Run:38,Jump:22,Sneak:11|eq:R=SwordBronze,L=ShieldBronze,H=HelmetTrollLeather,C=ArmorTrollLeatherChest,G=ArmorTrollLeatherLegs|kd:Boar:12/3,Neck:8/1,Greydwarf:5/2" }
```

---

**Path update** — sent every **30 seconds** (same cadence as standings), only when new points exist:

```
Path=<t>:<x>,<y>,<z>;<t>:<x>,<y>,<z>;...
```

- `t` — seconds elapsed since hunt start (integer)
- `x`, `y`, `z` — player world position rounded to nearest integer
- Points are sampled every **8 seconds**
- Only unsent points are included (batched incrementally)

---

**Player snapshot** — sent every **10 minutes**:

```
Snap=h:<cur>/<max>|s:<cur>/<max>[|f:<food1>,<food2>,...][|sk:<Skill>:<level>,...][|eq:<slot>=<item>,...][|kd:<enemy>:<kills>/<trophies>,...]
```

| Section | Format | Description |
|---------|--------|-------------|
| `h`     | `cur/max` | Current and max health (rounded) |
| `s`     | `cur/max` | Current and max stamina (rounded) |
| `f`     | `item,item,...` | Active food items by prefab name (omitted if none) |
| `sk`    | `Skill:level,...` | All skills with level ≥ 1 |
| `eq`    | `slot=item,...` | Equipped items by slot (omitted if empty) |
| `kd`    | `enemy:kills/trophies,...` | Kill and trophy drop counts for all enemies with ≥ 1 kill; "Trophy" prefix stripped from enemy names |

Equipment slot keys: `R` = right hand, `L` = left hand, `H` = helmet, `C` = chest, `G` = legs, `S` = shoulder, `U` = utility

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

