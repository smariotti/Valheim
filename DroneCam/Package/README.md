# Dronecam

Mod to implement a drone for a Valehim world.

## Controlling It

```
[DroneCam] Commands (/dronecam or /dc):
  /dc help
  /dc on                                  enter free-fly setup
  /dc off                                 return to normal camera
  /dc ff                                  enter free-fly mode
  /dc players                             list visible players
  /dc f <player> [dist] [height] [smooth] chase a player
  /dc o p <n> [r] [spd] [h]               orbit a player
  /dc o pos [r] [spd] [h]                 orbit current look-at position
  /dc o s <deg/sec>                       change orbit speed live
  /dc s p <n>                             security cam, track player
  /dc s pos                               security cam, track look-at position
Free-fly: WASD move  QE up/down  Shift fast  RMB/arrows rotate  F8 toggle
NOTE: You can also use freefly for ff, player for p, follow for f, orbit for o, and security for s.
NOTE: Player names with spaces must be quoted, e.g. /dc f \"Big Viking\" 8 3
```

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
