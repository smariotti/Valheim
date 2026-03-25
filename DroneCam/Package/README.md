# XDC - Xpert's Drone Cam

Mod to implement a drone for a Valehim world.

## Controlling It

```
[XDC] Xpert's Drone Cam 
console: 'dc (sub)' 
chat: '/dc (sub)'
sub-commands:
  help - show this help
  on / ff / freefly - enter free-fly
  off - exit drone cam
  hud - toggle HUD
  snap - snap drone to player target
  players - list players
  follow p (name) [dist] [h] [smooth] - follow player
  follow e (name) [dist] [h] [smooth] - follow enemy
  orbit p (name) [r] [spd] [h] - orbit player
  orbit e (name) [r] [spd] [h] - orbit enemy
  orbit pos [r] [spd] [h] - orbit look-at position
  orbit s (deg/sec) - set orbit speed
  security p (name) - security cam on player
  security pos - security cam on look-at position
  te - target nearest enemy for look-at
  te (name) - target named enemy for look-at
  te c - clear enemy look-at target
  stream on [w] [h] - start Spout stream (default 1920x1080)
  stream off - stop Spout stream
  stream res (w) (h) - change stream resolution
Wheel - dist/radius / Alt+wheel - height / Ctrl+wheel - focal offset / Alt+Ctrl+wheel - orbit speed
. / , - cycle players / F8 - toggle
Names with spaces: use quotes e.g. follow p "Big Viking"
```

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
