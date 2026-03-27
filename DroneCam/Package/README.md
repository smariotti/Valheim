# Xpert's Drone Cam

A spectator camera mod for Valheim that lets you detach from your character and fly a free camera around the world, follow and orbit other players, set up security-style tracking shots, and target enemies for cinematic look-at shots. Designed for use during multiplayer sessions to capture footage or spectate other players.

---

## Requirements

- BepInEx 5.x
- Valheim (Unity 6 build)

---

## Installation

Place `XpertsDroneCam.dll` in your `BepInEx/plugins/` folder. If you are using the Spout or NDI streaming features, see the [Streaming](#streaming) section below.

---

## Overview

When the drone cam is active your character becomes invisible to other players, is protected from damage, and does not count toward enemy difficulty scaling. You can fly freely around the world, follow a specific player from behind, orbit a player or point of interest, or set up a fixed security camera that tracks a moving target.

If the player you are targeting goes through a portal to a distant part of the map, the drone will automatically teleport to follow them.

Commands can be entered in two ways:

- **In-game console** — type `dc` followed by a subcommand, e.g. `dc follow player Xmal`
- **Chat window** — type `/dc` followed by the same subcommand, e.g. `/dc follow player Xmal`

Player names that contain spaces must be wrapped in quotes in both the console and chat, e.g. `dc follow player "Big Viking"`.

---

## Key Bindings

| Key | Action |
|---|---|
| F8 | Toggle drone cam on and off |
| W | Fly forward |
| S | Fly backward |
| A | Strafe left |
| D | Strafe right |
| E | Fly up |
| Q | Fly down |
| Left Shift | Hold to fly faster |
| Right Mouse Button | Hold to look around with the mouse |
| Left Arrow | Rotate left |
| Right Arrow | Rotate right |
| Up Arrow | Pitch up |
| Down Arrow | Pitch down |
| Mouse Wheel | Adjust distance or orbit radius |
| Alt + Mouse Wheel | Adjust camera height above target |
| Ctrl + Mouse Wheel | Slide the focal point up or down |
| Alt + Ctrl + Mouse Wheel | Adjust orbit speed |
| . (period) | Cycle to next player target |
| , (comma) | Cycle to previous player target |

---

## Full Command Reference

All commands are entered as `dc <command> <subcommand> <parameters>` in the console, or `/dc <command> <subcommand> <parameters>` in chat.

### Commands Table

| Command | Subcommand | Parameters | Description |
|---|---|---|---|
| `help` | | | Show command help in chat |
| `on` | | | Enter free-fly mode |
| `freefly` | | | Enter free-fly mode |
| `off` | | | Exit drone cam and return to normal camera |
| `hud` | | | Toggle the HUD on and off |
| `snap` | | | Snap drone instantly to current player target |
| `players` | | | List all players currently on the server |
| `follow` | `player` | (name) [distance] [height] [smoothing] | Follow a player from behind |
| `follow` | `enemy` | (name) [distance] [height] [smoothing] | Follow the nearest enemy with the given name |
| `orbit` | `player` | (name) [radius] [speed] [height] | Orbit around a player |
| `orbit` | `enemy` | (name) [radius] [speed] [height] | Orbit around the nearest enemy with the given name |
| `orbit` | `pos` | [radius] [speed] [height] | Orbit around the point you are currently looking at |
| `orbit` | `speed` | (deg/sec) | Change the orbit speed while orbiting |
| `security` | `player` | (name) | Fixed camera that tracks a player |
| `security` | `pos` | | Fixed camera that tracks the point you are looking at |
| `targetenemy` | | | Lock look-at onto the nearest enemy |
| `targetenemy` | (name) | | Lock look-at onto the nearest enemy with that name |
| `targetenemy` | `clear` | | Release enemy look-at lock |
| `stream` | `on` | [spout\|ndi] [width] [height] | Start video streaming via Spout or NDI |
| `stream` | `off` | | Stop video streaming |
| `stream` | `res` | (width) (height) | Change stream resolution while streaming |

**Parameter units:**

| Parameter | Unit | Default |
|---|---|---|
| distance | Valheim world units | 5 |
| height | Valheim world units | 2 |
| smoothing | Seconds (lower = snappier) | 0.1 |
| radius | Valheim world units | 10 |
| speed | Degrees per second | 30 |
| width | Pixels | 1920 |
| height (stream) | Pixels | 1080 |

---

### Abbreviated Commands Table

Every command and subcommand has a short alias for faster typing.

| Short form | Expands to |
|---|---|
| `ff` | `freefly` |
| `f` | `follow` |
| `o` | `orbit` |
| `s` | `security` |
| `te` | `targetenemy` |
| `st` | `stream` |
| `p` | `player` (as a subcommand) |
| `e` | `enemy` (as a subcommand) |
| `c` | `clear` (as a subcommand of targetenemy) |
| `n` | `nearest` (as a subcommand of targetenemy) |

**Examples using short forms:**

```
dc f p Xmal
dc o e Troll 15 45 6
dc te
dc st on ndi 1920 1080
```

---

## Command Details

### on / freefly / ff

Activates the drone cam in free-fly mode. Your character is hidden from other players and the camera detaches from your character and floats in place where it was. Use WASD/QE to move, hold Shift to move faster, and hold Right Mouse Button to look around.

```
dc on
dc freefly
dc ff
```

---

### off

Deactivates the drone cam and returns you to the normal third-person camera. Your character reappears at the last safe ground position near where the drone was.

```
dc off
```

---

### hud

Toggles the game HUD (health bar, stamina, hotbar etc.) on and off. Useful for clean recording footage. The crosshair is always hidden while the drone is active regardless of this setting.

```
dc hud
```

---

### snap

When following or orbiting a player, instantly moves the drone to the correct relative position near that player. Useful if the drone has drifted or you want to quickly reframe after issuing a new follow or orbit command. If the player is far away, the drone will teleport to them.

```
dc snap
```

---

### players

Lists all players currently connected to the server in the chat window. Players shown with *(distant)* next to their name are in an area of the map that is not currently loaded on your client — the drone will automatically teleport to them if you target them.

```
dc players
```

---

### follow player

Follows a named player from behind at a set distance. The camera stays behind the player and looks toward them as they move. Use the mouse wheel to adjust the follow distance and Alt + mouse wheel to adjust the height offset above the player.

```
dc follow player Xmal
dc follow player Xmal 8
dc follow player Xmal 8 3 0.2
dc f p "Big Viking" 5 2
```

| Parameter | Description | Default |
|---|---|---|
| name | Player name (quote names with spaces) | required |
| distance | How far behind the player to position the camera | 5 |
| height | How far above the player to position the camera | 2 |
| smoothing | How quickly the camera catches up to the player. Lower values are snappier, higher values are smoother | 0.1 |

If the player is in a part of the map that is not yet loaded, the drone will automatically teleport there.

---

### follow enemy

Follows the nearest enemy with the given display name (the name shown when you hover your crosshair over an enemy in game). The camera stays behind the enemy and looks toward it.

```
dc follow enemy Troll
dc follow enemy Troll 10 4
dc f e "Fuling Berserker" 8 3 0.15
```

Parameters are the same as follow player.

---

### orbit player

Orbits continuously around a named player at a fixed radius and height. The camera always looks toward the player while orbiting. Use the mouse wheel to adjust the orbit radius, Alt + mouse wheel to adjust height, and Alt + Ctrl + mouse wheel to adjust speed.

```
dc orbit player Xmal
dc orbit player Xmal 15 20 6
dc o p "Big Viking"
```

| Parameter | Description | Default |
|---|---|---|
| name | Player name | required |
| radius | Distance from the player in world units | 10 |
| speed | How fast the camera orbits in degrees per second | 30 |
| height | How far above the player the camera orbits | 4 |

---

### orbit enemy

Orbits the nearest enemy with the given name. Parameters are the same as orbit player.

```
dc orbit enemy Troll
dc orbit enemy Troll 12 25 5
dc o e Bonemass 20 15 8
```

---

### orbit pos

Orbits around the point in the world that the drone camera is currently looking at. Position the drone first in free-fly mode, aim at your point of interest, then issue this command.

```
dc orbit pos
dc orbit pos 10 30 4
dc o pos 20 20 6
```

---

### orbit speed

Changes the orbit speed while already orbiting without stopping or restarting the orbit.

```
dc orbit speed 15
dc o s 60
```

---

### security player

Locks the drone in place at its current position and continuously rotates to track a named player as they move around. The camera does not follow the player — it stays fixed and only rotates, like a security camera mounted on a wall.

```
dc security player Xmal
dc s p "Big Viking"
```

---

### security pos

Locks the drone in place and tracks the point in the world you are currently looking at. The camera rotates to keep that point in frame as the drone's own position stays fixed.

```
dc security pos
dc s pos
```

---

### targetenemy

Sets an enemy as the look-at target. When a look-at target is set, the camera always points toward that enemy regardless of what the anchor target is. For example you can orbit a player while keeping the camera pointed at a nearby enemy instead of at the player.

```
dc targetenemy
dc targetenemy Troll
dc targetenemy clear
dc te
dc te Troll
dc te c
```

If used with no name, targets the nearest enemy. Use `clear` or `c` to release the look-at lock and return to pointing at the anchor target.

---

### stream on

Starts broadcasting the drone camera view as a video stream that can be captured in OBS or other software. Requires KlakSpout (for Spout) or KlakNDI (for NDI) to be installed alongside the mod. See the [Streaming](#streaming) section for setup instructions.

```
dc stream on
dc stream on spout
dc stream on ndi
dc stream on spout 1920 1080
dc stream on ndi 2560 1440
dc st on ndi 3840 2160
```

| Parameter | Description | Default |
|---|---|---|
| protocol | `spout` or `ndi` — which streaming protocol to use | spout |
| width | Stream width in pixels | 1920 |
| height | Stream height in pixels | 1080 |

---

### stream off

Stops the active video stream.

```
dc stream off
dc st off
```

---

### stream res

Changes the resolution of an active stream without stopping and restarting it.

```
dc stream res 2560 1440
dc st res 3840 2160
```

---

## Streaming

The drone cam can broadcast its view as a live video feed that OBS or other capture software can pick up. Two protocols are supported:

**Spout** is the recommended option for most users. It shares the video directly between programs on the same PC with no encoding overhead and no network required. To use it:

1. Install the [OBS Spout2 plugin](https://github.com/Off-World-Live/obs-spout2-plugin)
2. Place `Klak.Spout.Runtime.dll` and `KlakSpout.dll` in your `BepInEx/plugins/DroneCam/` folder
3. Place the `dronecam_spout` asset bundle file in the same folder
4. In OBS add a Spout2 Capture source and set the source name to `DroneCam`
5. Start streaming with `dc stream on spout`

**NDI** works over a local network, so OBS can be running on a different PC from Valheim. To use it:

1. Install [NDI Tools](https://ndi.video/tools/) on both the Valheim PC and the OBS PC
2. Install the [OBS NDI plugin](https://github.com/obs-ndi/obs-ndi)
3. Place `Klak.Ndi.Runtime.dll` and `KlakNDI.dll` in your `BepInEx/plugins/DroneCam/` folder
4. In OBS add an NDI Source and set the source name to `DroneCam`
5. Start streaming with `dc stream on ndi`

---

## Configuration

A configuration file is created at `BepInEx/config/com.oathorse.xdc.cfg` after the first run. The following settings can be adjusted:

| Setting | Default | Description |
|---|---|---|
| FlySpeed | 10 | Normal free-fly movement speed (world units per second) |
| FlySpeedFast | 40 | Fast free-fly movement speed when holding Shift |
| RotationSpeed | 90 | Keyboard rotation speed in degrees per second |
| SmoothTime | 0.25 | Movement smoothing for orbit and security modes |
| TeleportDetectionDistance | 50 | Distance in world units that triggers portal follow detection |
| ScrollSensitivity | 0.5 | How much each mouse wheel tick adjusts distance or radius |

---

## Notes

- Your character is hidden from other players while the drone is active. Other players will not see your character model anywhere on the map.
- Your character cannot take damage while the drone is active.
- Nearby enemies do not scale their difficulty based on your presence while the drone is active.
- If the server has a sleep vote and all other players are sleeping, the drone will automatically suspend itself so your character can participate in the sleep vote, then resume when the night ends.
- Portals in the world cannot teleport the drone. Only the drone's own targeting system can initiate teleports.
- The drone camera can target players anywhere on the map, even in areas that are not currently loaded. It will automatically teleport to reach them.
- Player names are not case-sensitive in commands.

# Version History

v0.1.28
 - Fixed bug where drone player was visible to target player shortly after teleporting to be with him

v0.1.27
 - Fix teleport/distant player target acquire/reacquire problem
 - Use PlayerInfo instead of ZNetPeer and modify player teleport/distant player check to use it

v0.1.26
 - Fixed lock on problems using ZNetPeer, but still doesn't follow the player through portals

v0.1.25
 - Fix bug where teleport would happen even if the player's ZDO had a valid Player object
 - Fixed bug where drone would fail to reacquire player target 

v0.1.24
 - Fix player name quoted string parsing bug
 - Allow drone to re-acquire target after teleporting, either through portal or to reach distant player

v0.1.23
 - Use standard Player.TeleportTo() functionality instead of trying to move the drone to the player's new location when they teleport. Applies to cycling to distant players on servers as well.

v0.1.22
 - When a net peer is found, but has no associated ZDO due to distance/unloaded state, use peer's location for moving camera and loading zone

v0.1.21
 - Fix Console argument parsing bug

v0.1.20
 - Added vertical offset for focal point, adjustable with scroll wheel while holding control
 - Allow console commands to be entered via the dev console as well as the chat console

v0.1.19
 - Use ZNetPeer to get list of players in game (on server)

v0.1.18
 - ZDO based player name lookup everywhere

v0.1.17
 - added `/snap` command to snap drone to player position and rotation, useful for teleporting to player position without having to wait for the drone to catch up
 - refactored code to use the same function for snapping to player position on teleport and for the new snap command

v0.1.16
 - use ZDO to look up players, and get player position

v0.1.15
 - added grace period for reacquiring player target after teleporting to prevent rapid reacquisition if player is still in the process of teleporting/loading in

v0.1.14
 - use the body position to determine player teleport and not the position on the player's transform?

v0.1.13
 - remove SetSleeping patch and simply poll for sleep state
 - Don't use drone TeleportTo() and simply snap the drone to the player's new position, moving the drone's rigid body so zone loading occurs

v0.1.12
 - wait for player to fully reappear after teleporting before trying to reacquire target
 - push sleep state for all players to server after setting it for drone so server has current data before checking for all sleepers

v0.1.11
 - when switching from player or enemy targeting to position targeting, use the last position it was looking at. Should make setting up orbits easier.
 - hide wet effect on drone for both client and server when drone passes though water
 - use a fake bed when trying to get drone to sleep
 - scan for player every tick rather than caching its target point to make it possible to reacquire after going through portal

v0.1.10
 - another attempt at sleeping when player sleeps
 - don't get the drone wet when passing through water
 - use built in teleport function when player teleports rather than trying to relocate drone
 - sleeping now uses "in bed" setting when drone tries to sleep
  
v0.1.9 
 - fixed a bug with command line interpret

v0.1.8
 - Unified targeting for player/enemy/pos
 - added `/dronecam follow enemy` and `/dronecam orbit enemy` commands specifying enemy name for nearest enemy
 - added `/dronecam hud` command to toggle HUD visibility

v0.1.7
 - fixed a chat console entry bug

v0.1.6
 - added `/dc targetenemy                         target nearest enemy for look-at`
 - added `/dc targetenemy <name>                  target named enemy for look-at`
 - added `/dc targetenemy clear                   clear enemy target`

v0.1.5
 - fixed bug where `/dc` was ignored
 - prevent drone player from falling to death when turning `/dronecam off`
 - dronecam now no longer prevents players from sleeping
 - fix to re-aquiring player target after player goes through portal
 - made sure drone can't be hit or die (should be in god/ghost mode)
 - make Valheim server ignore drone with regards to difficulty scaling
 - added modifiers (alt and ctrl) for scroll wheel to adjust drone distance and height on the fly
 - added keys '.' and ',' to cycle the drone's target through players on the server

v0.1.4
- added requested features
v0.1.0 
- Initial Release
