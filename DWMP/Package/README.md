# Dude, Where's My Portal

This is a simple Valheim mod for BepInEx which automatically manages Minimap Pins for portals based on what you do with portals in the game world.

## Place a Portal

When you place a Wooden or Stone portal in Valheim, a Minimap Pin will automatically be added.

## Remove a Portal

When you destroy a portal or it gets destroyed by something else, it'll be removed from the Minimap

## Rename a Portal

When you rename a portal, it'll automatically be renamed in the Minimap

## Pass through a pre-existing Portal

Optionally, if you toggle `CreatePinOnTeleport` on, DWMP will create a pin for you as you pass through existing portals.

## That's it.

That's really all this thing does.

## Change Log
v0.1.3
- Updated to work with Valheim post Patch 0.220.3

v0.1.2
- Fixed a console bug with the new command. >.<

v0.1.1
- Added console command `CreatePinOnTeleport` which toggles on and off the auto-creation of named Minimap pins as you teleport through a portal. Aids with the generation of Minimap portal pins for portals that existed in the map before Dude Where's My Portal was installed.

v0.1.0
- Initial version