# `/showalltrophystats` Command

## What It Does

Toggles between two kill/trophy tracking modes:

| Mode | Tracks |
|------|--------|
| **Default (off)** | Only kills made by the local player, and only trophies the local player physically picked up |
| **Show All (on)** | Every kill of a trophy-capable enemy on the server regardless of who killed it, and every trophy that dropped regardless of who picked it up |

## Tournament Validity

Enabling show-all stats **immediately flags the run as invalid for tournament play** (`__m_invalidForTournamentPlay = true`). This cannot be undone for the session. The score text turns **green** as a visual indicator that the run is disqualified.

## Data Tracking

Two separate dictionaries are maintained at all times for every trophy-capable enemy:

- **`__m_playerTrophyDropInfo`** — kills credited to the local player + trophies the local player picked up
- **`__m_allTrophyDropInfo`** — all kills server-wide + all trophy drops server-wide

Both are always being recorded. The toggle only controls what is *displayed*.

### How each dictionary is populated

| Event | `__m_playerTrophyDropInfo` | `__m_allTrophyDropInfo` |
|-------|---------------------------|------------------------|
| Enemy killed by local player | `m_numKilled++` | — |
| Enemy killed by anyone else | — | `m_numKilled++` |
| Trophy dropped (any killer) | — | `m_trophies++` |
| Trophy picked up by local player | `m_trophies++` | — |

> Note: "all" kills and "all" drops are tracked independently. A kill by another player increments `__m_allTrophyDropInfo.m_numKilled`; a drop (regardless of who caused it) increments `__m_allTrophyDropInfo.m_trophies`.

## Effect on Trophy Tooltip

When hovering a trophy icon, the tooltip expands with additional rows when show-all is active:

**Default tooltip:**
- Point Value
- Player Kills
- Trophies Picked Up
- Kill/Pickup Rate
- Wiki Trophy Drop Rate
- Player Luck Rating

**Show-all tooltip (additional rows):**
- Actual Kills *(all kills server-wide)*
- Actual Trophies *(all drops server-wide)*
- Actual Drop Rate *(vs wiki rate)*
- Actual Luck Rating

The tooltip background window grows from `240×125` to `240×195` to accommodate the extra rows.

## Effect on Score Color

| Condition | Score text color |
|-----------|-----------------|
| Normal, only mod running | Yellow |
| Unauthorized mod detected | Cyan |
| Show-all enabled **or** any other invalidating condition | Green |

## Server Reporting

The `kd:` section of every player snapshot **always uses `__m_allTrophyDropInfo`** — server-wide kill and drop counts — regardless of whether `/showalltrophystats` is enabled. The toggle has no effect on what is sent.

`/showalltrophystats` only controls what is shown in the **tooltip UI**. The server was already receiving all-kill data the whole time.

If the command is enabled, `__m_invalidForTournamentPlay = true` causes `CanPostToTracker()` to block all server communication anyway — so the run goes dark entirely.

## Persistence

`__m_showAllTrophyStats` is reset to `false` on every new game/world load (`InitializeTrackedDataForNewPlayer`). It is not saved between sessions.

## Console Output on Toggle

**Enabling:**
```
Displaying ALL enemy deaths for kills and trophies!
WARNING: Not legal for Tournament Play!
```

**Disabling:**
```
Displaying ONLY Player enemy kills and picked up trophies!
```

Disabling does **not** clear the invalid tournament flag — once set, the run stays disqualified for the session.
