# I Tawt I Taw...
This is a simple Valheim mod that sticks pins on the Minimap for the last location you saw a specified enemy.

By default, it shows Serpent and BonemawSerpent locations.

For something to be marked on the Minimap:
- it needs to be inside your minimap explore range, and 
- you need to be pointing the camera at it. 

If those are both true, a pin will be dropped with the name of the puddy tat that you saw.

Pins will remain until you clear them. While looking at the enemy, the pin position will update to the last seen position.

## Configuration

I Tawt I Taw stores a config file in the `BepInEx/config` directory called `com.oathorse.ITIT.cfg` which you can edit with a text editor.

The following config variables are supported:

`ListOfPuddyTats` - this is a string listing all the enemy names you want to track. It defaults to "Serpent,BonemawSerpent"
`ShowEverything` - if `true` it'll enable minimap pins for ALL enemys spawning in the area as they move around
`CheckFrequency` - this is how often, in seconds, the mod should check to see if you taw any puddy tats

