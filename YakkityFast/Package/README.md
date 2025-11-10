# Yakkity Fast

Moving fast now has the soundtrack that it deserves... so long as you keep moving fast enough. Think of the movie Speed, except in Valheim... and stupid.

## Isn't this kinda dumb?

Yeah, it is, but it's also kinda fun.

## Streamers Be Warned!

Read below for details if you're live streaming with this mod enabled. You may be subject to Copyright claims.

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

## Streamer Warning
"Yakety Sax" is copyrighted by Sony/ATV Tree Publishing, and the song's writers are Randy Randolph and James Rich. To use the song, permission is required from the copyright holder, which can be obtained by contacting the publisher, Sony/ATV Tree Publishing. 

I'm warning streamers, as you may get a copyright claim. Youtube can flag your video as containing copyrighted material.

I'm furnishing the mod for fun, and for your own personal enjoyment and take full responsibility for redistributing that song.

FWIW, apparently the copyright holder (Sony) allows this song to be used on YouTube. Their copyright claim does not restrict most uses of this song on YouTube. Though there may be restrictions to monetization for YouTube Partners.

## Change Log
v0.1.1
- Fade music as you start timing out when speed is below the minimum
- Include Copyright Claim information in the README for Streamers

v0.1.0 
- Initial Release, defaults to "unleashed" mode