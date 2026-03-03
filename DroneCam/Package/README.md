# Dronecam

Mod to implement a drone for a Valehim world.

## Controlling It

```
[DroneCam] Commands (/dronecam or /dc):
  /dc help
  /dc on | ff | freefly - enter free-fly setup
  /dc off - return to normal camera
  /dc players - list visible players
  /dc f p <player> [dist] [h] [sm]   follow a player
  /dc f e <enemy>  [dist] [h] [sm]   follow nearest named enemy
  /dc o p <n> [r] [spd] [h] - orbit a player
  /dc o e <n> [r] [spd] [h] - orbit nearest named enemy
  /dc o pos [r] [spd] [h] - orbit current look-at position
  /dc o s <deg/sec> - change orbit speed live
  /dc s p <n> - security cam, track player
  /dc s pos - security cam, track look-at position
  /dc te - target nearest enemy for look-at
  /dc te <name> - target named enemy for look-at
  /dc te c - clear enemy look-at target
  /dc hud - toggle HUD visibility
Player names with spaces must be quoted: /dc f p "Big Viking"
Wheel: dist/radius  Alt+wheel: height  Ctrl+wheel: orbit speed
. / , keys cycle next/prev player target  F8 toggle
```

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
