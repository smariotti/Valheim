# Yakkity Fast

Moving fast now has the soundtrack that it deserves... so long as you keep moving fast enough. Think of the movie Speed, except in Valheim... and stupid.

## Isn't this kinda dumb?

Yeah, it is, but it's also kinda fun.

## Controlling It

`/yakkity unleashed`
- No limits to the amount of yakkity

`/yakkity default` 
- Start fading the music out at 60 seconds, stop it completely at 120

`/yakkity off` 
- Turn it off completely

`/yakkity on`
- You changed your mind, you love it

## Editing the Config File manually
The BepInEx config file lives somewhere like here:
  `...\BepInEx\config\com.oathorse.Yakkity.cfg`

And you can freely edit it to dial things in for how you want it to behave.

```
## Settings file was created by plugin Yakkity Fast v0.1.0
## Plugin GUID: com.oathorse.Yakkity

[General]

## The minimum velocity where the audio will start playing
# Setting type: Single
# Default value: 10.5
Minimum Speed = 10.5

## The maximum number of seconds of audio that will ever play, 0 for no limit
# Setting type: Single
# Default value: 60
Maximum Seconds = 60

## The number of seconds before audio fades
# Setting type: Single
# Default value: 30
Fade Delay = 30

## The rate at which audio will fade once playing
# Setting type: Single
# Default value: 0.1
Fade Rate = 0.5

## Number of seconds velocity must be below Minimum Speed for music to Start
# Setting type: Single
# Default value: 1
Start Grace Period = 1

## Number of seconds velocity must be below Minimum Speed for music to Stop
# Setting type: Single
# Default value: 3
Stop Grace Period = 3
```

## Change Log
v0.1.0 
- Initial Release, defaults to "unleashed" mode