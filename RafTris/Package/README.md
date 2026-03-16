# ⚓ RafTris

> *Slow raft trip? Tetris time.*

RafTris is a BepInEx mod for **Valheim** that embeds a fully-featured game of Tetris in a draggable overlay window. Each level is themed after a Valheim biome, using enemy **trophy icons** as block art. The four-cell I-piece always uses the **boss trophy** of the current biome, rewarding the coveted four-row "Tetris" clear with a boss face.

---

## Features

- **Full classic Tetris** — 7-bag randomiser, SRS wall kicks, ghost piece, hold piece, DAS/ARR input
- **8 Biome themes** — Meadows → Black Forest → Swamp → Mountains → Plains → Mistlands → Ashlands → Deep North (placeholder icons until content ships)
- **Trophy icon blocks** — pulls live sprites from Valheim's ObjectDB at runtime; falls back to a stylised placeholder automatically
- **Biome colour palettes** — each biome has its own background, grid, and piece colour ramp with a scanline overlay
- **Persistent saves** — scores and session state are written to `BepInEx/config/RafTris/raftris_save.json`
- **Standard Tetris scoring** with back-to-back Tetris bonus
- **Keyboard + mouse** — all buttons are clickable, all moves are keyboard-controllable
- **Configurable** via `BepInEx/config/com.raftris.valheim.cfg`

---

## Installation

1. Install **BepInExPack for Valheim** (5.4.22 or newer) — [Thunderstore link](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Drop `RafTris.dll` into `Valheim/BepInEx/plugins/`
3. Launch Valheim — press **F7** to open RafTris at any time

---

## Building from Source

**Requirements:** .NET Framework 4.7.2 SDK, Visual Studio 2022 or Rider

```bash
# Set your Valheim install path (Windows example)
set ValheimInstallDir=C:\Program Files (x86)\Steam\steamapps\common\Valheim

# Build
dotnet build RafTris.csproj -c Release

# The DLL will be in bin\Release\net472\RafTris.dll
```

Copy `RafTris.dll` to `<ValheimInstallDir>\BepInEx\plugins\`.

Uncomment the `<PostBuildEvent>` block in `RafTris.csproj` to auto-copy on every build.

---

## Controls

| Action         | Keys                              |
|---------------|-----------------------------------|
| Move left/right | ← → (with DAS/ARR)            |
| Soft drop       | ↓                               |
| Hard drop       | Space                            |
| Rotate CW       | ↑ or X                          |
| Rotate CCW      | Z or Left Ctrl                  |
| Hold piece      | C or Left Shift                 |
| Pause / Resume  | Esc or F1                       |
| Open / Close    | **F7** (configurable)           |

All action buttons in the UI window are also mouse-clickable.

---

## Scoring

| Lines cleared | Base points |
|---------------|------------|
| 1 (Single)    | 100 × Level |
| 2 (Double)    | 300 × Level |
| 3 (Triple)    | 500 × Level |
| 4 (Tetris)    | 800 × Level |

**Back-to-back Tetris bonus:** ×1.5 multiplier when two consecutive Tetris clears are achieved.  
**Soft drop:** +1 point per cell.  
**Hard drop:** +2 points per cell.

Level advances every **10 lines cleared**. Each level cycles to the next Valheim biome (loops after Deep North).

---

## Biome Themes & Trophy Mapping

| Level | Biome        | Boss Trophy (I-piece)  |
|-------|-------------|------------------------|
| 0     | Meadows      | Eikthyr                |
| 1     | Black Forest | The Elder              |
| 2     | Swamp        | Bonemass               |
| 3     | Mountains    | Moder                  |
| 4     | Plains       | Yagluth                |
| 5     | Mistlands    | The Queen              |
| 6     | Ashlands     | Fader                  |
| 7     | Deep North   | *(placeholder)*        |

The six O/S/Z/L/J/T pieces use enemy trophies from each biome (Neck, Boar, Greydwarf, etc.).

---

## Configuration (`com.raftris.valheim.cfg`)

| Key                    | Default | Description                                       |
|------------------------|---------|---------------------------------------------------|
| `ToggleKey`            | F7      | Key to open/close the window                     |
| `PauseGameWhilePlaying`| false   | Freeze Valheim time while the window is open     |
| `WindowScale`          | 1.0     | UI scale (0.5 – 2.0)                             |

---

## Save File

`BepInEx/config/RafTris/raftris_save.json`

```json
{
  "AllTimeBestScore": 12400,
  "AllTimeBestLevel": 5,
  "TotalLinesCleared": 88,
  "TotalGamesPlayed": 3,
  "CurrentScore": 3200,
  "CurrentLevel": 2,
  "CurrentLines": 17,
  "SessionInProgress": true,
  "BiomeBestScores": [4800, 3200, 2100, 0, 0, 0, 0, 0]
}
```

Delete this file to reset all progress.

---

## Deep North Placeholder Icons

Deep North content is not yet released in Valheim. RafTris uses coloured "?" tiles for that biome's pieces and logs a soft warning in BepInEx console. When Deep North ships, update `BiomeThemes.cs` with real trophy prefab names and recompile.

---

## Mod Compatibility

RafTris patches `ZInput.GetButton*` to suppress Valheim input while the window is open. This is compatible with most other mods. If you experience input conflicts, try setting `PauseGameWhilePlaying = true` in the config.

---

## Licence

MIT — see `LICENSE.txt`.
