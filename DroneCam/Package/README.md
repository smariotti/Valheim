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

v0.1.4
- added requested features
v0.1.0 
- Initial Release