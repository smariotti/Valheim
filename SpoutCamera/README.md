# ValheimSpoutCamera

A BepInEx mod for **Valheim** that creates a secondary camera and shares its
framebuffer with OBS (or any Spout-compatible receiver) using the
[Spout](https://spout.zeal.co/) inter-application video sharing protocol.

---

## How It Works

```
Valheim → Custom Camera → RenderTexture → Spout Sender → OBS Spout plugin
```

1. The mod spawns a dedicated Unity `Camera` that renders into a `RenderTexture`.
2. A Spout sender component streams that texture as a named Spout source.
3. OBS picks it up via the **obs-spout2** plugin as a video source.

---

## Requirements

| Dependency | Where to get it |
|---|---|
| **BepInEx 5.x** (not 6) | https://github.com/BepInEx/BepInEx/releases |
| **Spout for Windows** runtime | https://spout.zeal.co/ (install once system-wide) |
| **KlakSpout** Unity plugin DLL | https://github.com/keijiro/KlakSpout/releases |
| **obs-spout2** OBS plugin | https://github.com/Off-World-Live/obs-spout2-plugin/releases |

> **Only Windows is supported.** Spout is a DirectX/Windows-only technology.

---

## Installation

### 1 — Install BepInEx
Unpack BepInEx into your Valheim folder (same level as `valheim.exe`).
Run the game once to generate the `BepInEx/plugins` directory.

### 2 — Install the Spout runtime
Run the Spout installer from https://spout.zeal.co/.

### 3 — Install KlakSpout
Download the latest release from https://github.com/keijiro/KlakSpout/releases.
Copy `jp.keijiro.klak.spout.dll` (and any companion `.dll` / native `.dll`) into:
```
Valheim/BepInEx/plugins/KlakSpout/
```

### 4 — Install this mod
Copy the compiled `ValheimSpoutCamera.dll` into:
```
Valheim/BepInEx/plugins/ValheimSpoutCamera/
```

### 5 — Install obs-spout2
Download and install the OBS plugin from:
https://github.com/Off-World-Live/obs-spout2-plugin/releases

In OBS: **Sources → + → Spout2 Capture** → select `ValheimCamera` (or whatever
you set `SenderName` to in the config).

---

## Configuration

After first launch, edit:
```
Valheim/BepInEx/config/com.yourname.valheim.spoutcamera.cfg
```

| Key | Default | Description |
|---|---|---|
| `EnableMod` | `true` | Master on/off switch |
| `SenderName` | `ValheimCamera` | Name shown in OBS Spout source list |
| `Width` | `1920` | Output texture width |
| `Height` | `1080` | Output texture height |
| `ToggleKey` | `F8` | Hotkey to start/stop the sender |
| `FOV` | `60` | Camera field of view |
| `FollowPlayer` | `true` | Follow local player vs mirror main camera |
| `OffsetX/Y/Z` | `0 / 2 / -4` | Camera offset from player (local space) |

---

## Building from Source

```bash
# Clone
git clone https://github.com/yourname/ValheimSpoutCamera
cd ValheimSpoutCamera

# Edit ValheimSpoutCamera.csproj and set <ValheimDir> to your install path

# Build (requires .NET SDK 6+ or Visual Studio 2022)
dotnet build -c Release
# DLL is auto-copied to BepInEx/plugins by the post-build target
```

---

## Architecture Notes

### Reflection-based Spout binding
The mod locates the Spout sender component at runtime via
`AppDomain.CurrentDomain.GetAssemblies()` so that it compiles without a hard
reference. This means the project builds even if you haven't placed the Spout
DLLs yet, and you can swap between KlakSpout and Spout4Unity without recompiling.

Supported type names (tried in order):
- `Klak.Spout.SpoutSender` (KlakSpout)
- `Spout.SpoutSender` (Spout4Unity)

### Camera lifecycle
- The camera GameObject is created with `DontDestroyOnLoad` so it survives
  scene transitions.
- The camera is **disabled by default** and only renders when toggled on (F8).
- In mirror mode the camera copies the main camera's transform every frame.
- In follow mode a simple spring follows the local player with a configurable
  offset.

### Performance
- The `RenderTexture` is created once and reused.
- The Spout sender performs a GPU→GPU copy (no CPU readback), so the overhead
  is minimal — typically < 1 ms per frame at 1080p.

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| "Spout sender component not found" | Check that KlakSpout DLLs are in BepInEx/plugins |
| Black frame in OBS | Make sure `ToggleKey` was pressed (F8) |
| OBS shows no Spout sources | Install the Spout runtime and obs-spout2 plugin |
| Game crashes on startup | Confirm you are using BepInEx **5.x** (not 6.x) |

---

## License

MIT — see [LICENSE](LICENSE).
