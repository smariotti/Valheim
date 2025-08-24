# TrophyHuntMod

This is a BepInEx mod for Valheim for the Valheim Trophy Hunt that displays discovered/undiscovered trophies at the bottom edge of the screen along with a computed score for the Trophy Hunt based on current scoring rules. 

It offers three modes, details are provided below.
- **Trophy Hunt**
  - This is the normal Trophy Hunt. Valheim is not modified in any way. You play on default settings. No aspects of Valheim are modified in any way.
- **Trophy Rush**
  - This is an advanced version of the Trophy Hunt. Combat Difficulty is set to Very Hard, Resources drop at 2x rate and all Trophy-bearing enemies drop them 100% of the time.
- **Trophy Saga**
  - This is a highly modified Trophy Hunt where many game mechanics are altered to speed up progression such as production building speeds, enemy drops, and crop growth times
- **Culinary Saga**
  - Trophy Saga game modifiers, but you score by creating one of each cooked food.
- **Casual Saga**
  - Trophy Saga game modifiers, but no time limit, no scoring, and no penalties. Free play!

Available here:

https://thunderstore.io/c/valheim/p/oathorse/TrophyHuntMod/

Requires a BepInEx install.

https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/

## Installation (manual)

Two Options:
- Use r2modman to automatically install it from Thunderstore and launch Valheim. R2modman is the mod manager available for download at https://thunderstore.io
- Manual: Download and install BepinEx_Valheim, then simply copy the contents of the archive into the BepinEx/Plugins directory. This is usually found somewhere like 'C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins' if you've installed BepInEx according to the instructions. (https://www.nitroserv.com/en/guides/installing-mods-on-valheim-with-bepinex)

# Trophy Hunt Game Modes

- You have 4 hours to score as many points as you can.
- Points are scored by picking up trophies, with point values assigned by biome. You only get points for the first Trophy of that type, and it must enter your inventory to count. You can drop it immediately if you like.
- Points are subtracted for dying or re-logging to the main menu (to clear aggro, etc)
- Some game modes offer additional score bonuses

## Point Values

Scores are awareded from the following points table:
```
**Enemies**
Meadows Trophies - 10 points each
Black Forest & Swamp Trophies - 20 points each
Mountain/Plains Trophies - 30 points each
Mistland Trophies -  40 points each
Ashland Trophies - 50 points each
Serpent Trophy - 25 points

**Mini-Bosses**
Brenna Trophy - 25 points
Geirrhafa Trophy - 45 points
Zil & Thungr Trophies - 65 points

**Bosses**
Eikthyr Trophy - 40 points
Elder Trophy - 60 points
Bonemass Trophy - 80 points
Moder Trophy - 100 points
Yagluth Trophy - 120 points
Queen/Fader Trophy - 1000 points (400 in Saga modes)

(Queen/Fader points will get nerfed when/if someone completes)
```

## Trophy Hunt

This is the standard Trophy Hunt game mode, played at completely stock/vanilla Valheim settings for "Normal" game mode.

- Trophies drop at standard Valheim rates (documented on the wiki and displayed in the Trophy tooltips in-game, pause the game to see the tooltips on the HUD Trophies)
- Death penalty is -20 points
- Relog penalty is -10 points

## Trophy Rush

This Trophy Hunt variant has the following changes from Trophy Hunt

- Trophies drop at 100% drop rate for any enemy that can drop one
- Combat is set to Very Hard (Normal in Trophy Hunt)
- Resource Rate is set to 2x (you get double all resources picked up, dropped by enemies, or found in chests)
- Death penalty is -10 points (half as much as Trophy Hunt)
- Relog penalty is -5 points (half as much as Trophy Hunt)
- /die penalty is an additional -10 points (total -20 points for using /die Console command)

In addition, this mode offers "Biome Bonuses" for completing the full set of non-boss Trophies for a given Biome:
```
**Biome Bonuses**
+20 Meadows
+40 Black Forest
+40  Swamp
+60  Mountains
+60  Plains
+80  Mistlands
+100 Ashlands
```

## Casual Saga

Trophy Saga game modifiers, but no trophies/scoring or penalties. There is no time limit. Nothing is reported to the leaderboards. You just play and have fun.

## Trophy Saga

This Trophy Hunt variant implements a good number of modifications to standard Valheim play to speed progression. Bosses no longer gate upgrade progression since boss dropped items can now drop from powerful minions in that boss' biome.

- Trophies drop 100% of the time until you have one, then drop at vanilla valheim rate (except Deer, which always drops.) This is the same as Trophy Rush
- Combat is set to Normal difficulty
- Resource Rate is set to 2x (you get double all resources picked up, dropped by enemies, or found in chests)
- Death penalty is -30 points
- Relog penalty is -10 points
- Metal Ores "insta-smelt" when you pick them up. If you pick up ore from the ground or out of a chest, it becomes the equivalent metal bar instantly.
- Mining is twice as productive
- Portals allow **all** items through (metals, eggs, etc.)
- Modified Enemy Loot
	- Greylings drop various useful Meadows and Black Forest items, including Finewood
	- Trolls have a 66% chance to drop Megingjord
	- Biome Boss Minions now have a chance to drop Boss Items
	  - Black Forest: Greydwarf Brute has a chance to drop Crypt Key
	  - Swamp: Oozers have a chance to drop Wishbone
	  - Mountains: Drakes have a chance to drop Moder's Tears
	  - Plains: Fuling Shaman and Berserker have a chance to drop Torn Spirit
	  - Mistlands: Seeker Soldiers have a chance to drop Majestic Carapace
	  - Hildir Mini-bosses always drop their Biome's Boss Item
	- Dverger drop a dozen or so pieces of Yggrasil wood when they die
	- Rancid Remains always drops his mace (Iron Mace, non-poisonous)
- Production buildings that take time to process **all** take only seconds
  - Fermenter
  - Beehive
  - Charcoal Kiln
  - Spinning Wheel
  - Windmill
  - Sap Extractor
  - Eitr Refiner (this also **no longer requires Soft Tissue**, just Sap)
  - NOTE: Cooking buildings are unchanged from vanilla (Cooking Station, Mead Ketill, Iron Cooking Station, Cauldron, Stone Oven)
- Pickaxes are more productive when mining
- All planted seeds and saplings grow to maturity within 10 seconds (as in previous version)
- All Player Skills are learned at level 20 and then increase normally.

## Culinary Saga
- Like Trophy Saga, but you score by making food.


## Change Log
v0.9.11
- Added `/togglescorebackground` Console command to enable/disable a black background behind the score for increased legibility.

v0.9.10
- Internal housekeeping

v0.9.9
- Saga Modes
  - Adjusted Boss item drop method to prevent mob vs. mob violence causing player to miss dropped Boss items

v0.9.8
- Changes for latest Valheim (Patch 0.220.3):
  - Fixed crash caused by recent Valheim update (Platform DLL was added by Iron Gate)
  - Removed custom Intro text, as that no longer patched correctly

v0.9.7
- Synchronized Thunderstore version number with internal version number.

v0.9.5
- Trophy Saga
  - The Queen trophy is now worth 400 points
  - Fader trophy is now worth 400 points

v0.9.4
- Trophy Saga
  - Changed Megingjord drop logic to stop dropping off of Greylings or Trolls only after you've picked one up, rather than after one has dropped at all. Helps prevent missing the drop if mob vs. mob violence offscreen results in a Megingjord dropping. May result in multiple Megingjords dropping during play.
  - Removed some debug logging for ore/bar conversion.

v0.9.3
- Trophy Saga
  - Fixed a bug with insta-smelt where duplicate metal bars would be generated and placed in the world at a random location while manipulating ores.

v0.9.2
- Removed debug logging calls for new leaderboard support.

v0.9.1
- Switched authentication to use implicit grant OAUTH2 workflow for increased security.

v0.9.0
- Discord Authentication for going Online with TrophyHuntMod!
  - Now, to report gameplay data to the Trophy Hunt server, you must Go Online via authentication with Discord. 
  - A Discord account is required to "go online" with TrophyHuntMod.
  - "Going online" means you will report your gameplay data to the Trophy Hunt server, which maintains leaderboards for live Trophy Hunt tournament events.
  - You need not "go online" with TrophyHuntMod unless you're competing in a live tournament and wish to report your gameplay to the online Tracker and Leaderboard.
  - At the main menu, use the Discord Login or Discord Logout button to change your online status.
- In-game Leaderboard support for when you're Online.
  - New Golden Trophy icon in the lower left corner of the HUD indicates that you're participating in a live online event 
  - Hovering over the icon shows the current names and scores of the players in the event
- Support for new valheim.help database backend for tracking Trophy Hunt events
- Support for future ad hoc or unofficial trophy hunt events created at valheim.help

v0.8.8
- Disabled live Tracker/Leaderboard reporting. Any Trophy collection data will need to be entered manually on the website for tournaments. 
  - The mod no longer reports live updates to http://valheim.help until some privacy issues are ironed out. @jv is aware of the issues.
- Whitelisted TubaWalk mod so that it can be used without violating tournament rules. Bring the tuba guys!

v0.8.7
- Fixed bug with player path data being restored from previous save when it shouldn't have been

v0.8.6
- Fixed bug where fishing bait was dropping outside of Culinary Saga game mode

v0.8.5
- Saga game modes (Trophy Saga, Culinary Saga, Casual Saga)
  - Battering Ram no longer eats through wood at an alarming rate and now operates normally.
  - Timer display is now OFF by default (use `/timer show` to make it visible again) since the Saga timer doesn't adhere to Tournament rules.
  - The first boss you kill past Eikthyr will drop a stack of Ymir Flesh
- Culinary Saga
  - Fishing without Haldor!
  - Powerful boss minions in each biome now have a chance to drop a Fishing Rod
  - Various fishing baits can drop directly from the enemies whose trophies you would trade to Haldor, except for Necks, which drop basic fishing bait
	 | Bait | Dropping Enemy |
	 | -------------------- | ---------------- |
	 | Fishing Bait		    | Neck |
	 | Hot Fishing Bait	    | Charred Warrior |
	 | Cold Fishing Bait    | Fenring |
	 | Frosty Fishing Bait	|   Drake |        
	 | Mossy Fishing Bait	|   Troll |        
	 | Misty Fishing Bait	|   Lox |        
	 | Heavy Fishing Bait	|   Serpent |
	 | Stingy Fishing Bait	|   Fuling |      
	 | Sticky Fishing Bait	|   Abomination |


v0.8.4
- Casual Saga HOTFIX:
  - Fixed a bug that would glitch your inventory when you picked up a trophy, causing all sorts of madness and mayhem. D'oh!

v0.8.3
- Added console command `/showonlydeaths` which toggles show/hide of everything but the death counter (requested by Helga Blue)

v0.8.2
- Fixed a bug with the timer where it would reset on death/respawn to what it was at the last save

v0.8.1
- Added "Casual Saga" game mode
  - No scoring, no time limit, no leaderboards. Just Saga rule set and free-play mode. 
- Save/Load of player data for all game modes
  - Enables multi-session play
    - Retains all trophy and food stats (player enemy kills/trophy pickups as well as all enemy deaths/trophy drops) between sessions
    - Saves `/showpath` data with the player/world
	- Stores current timer value in modes that use a game timer
  - Prevents data loss of Logouts/slash-die counts if Valheim should crash
- Saga
  - Slight boat speed adjustment

v0.7.6
- Saga Modes (Trophy and Culinary)
  - Speed Chickens!
	- I can't tell you how fun it was to type that into the release notes.
	- Chickens are more promiscuous, fertile and grow up really fast.
- Culinary Saga
  - Rebalanced point values by biome for more early-game points (WIP)
	- Points:
	  - Meadows foods: 10 points
	  - Black Forest foods: 20 points
	  - Swamp foods: 30 points
	  - Mountains foods: 30 points
	  - Ocean foods: 40 points
	  - Plains foods: 40 points
	  - Mistlands foods: 50 points
	  - Ashlands foods: 60 points
- Trophy Fiesta
  - Fixed a bug where festival lighting was left on when switching to Culinary Saga (thanks @Xmal)

v0.7.5
- Culinary Saga! >.<
  - New non-Trophy based game mode added! (EXPERIMENTAL!)
  - Cook one of each food to score points. 
	- Meadows foods: 10 Points
	- Black Forest foods: 10 Points
	- Swamp Foods: 20 Points
	- Mountains Foods: 20 Points
	- Plains Foods: 30 Points
	- Mistlands Foods: 40 Points
	- Ashlands Foods: 50 Points
  - Standard "Saga" rules apply (same game modifiers as Trophy Saga)
	- Fast Production Buildings and Fast Crops
	- Insta-Smelt ores
	- Fast Boats!
	- Special drops from Boss Minions, Greylings, Trolls, Dvergr
	- More Ooze from Blobs
	- Mining is twice as productive
	- Rancid Remains drops an Iron Mace
	- Head-start on all Skills
  - No Feasts yet (WIP)

v0.7.2
- Trophy Saga
  - HOTFIX: Fixed potential object leak which could lead to crashes in some situations.

v0.7.1
- HOTFIX: Debugging code left in was causing lag when many items were in autopickup range.

v0.7.0
- Trophy Saga
  - Ready for Prime Time!
  - Removed "EXPERIMENTAL" tag from Main Menu Text
  - No longer color the score green to indicate "invalid for tournament play"
- Trophy Rush
  - Added "Biome Bonus" pop up text and stinger audio when you get a bonus

v0.6.24
- Trophy Saga
  - HOTFIX: Fixed a bug where you couldn't craft anything. LOL.

v0.6.23
- Trophy Saga
  - Fixed a bug with Insta-Smelt where ores wouldn't auto-pickup in some cases
  - Tweaks to Greyling drop rates a little, just a little more leather

v0.6.22
- Trophy Saga
  - Base skill for all skills raised from 10 to 20. All skills acquire at 20 and then are raised normally.

v0.6.21
- Trophy Saga
  - Trophies now ALWAYS drop from enemies until you have one and then drop at normal rate (except Deer which always drops.) This is the same as Trophy Rush.

v0.6.20
- Trophy Saga
  - Biome Boss Minions all have a **very high** chance to drop the boss key items. Like almost entirely nearly always.
  - Trolls now have a **very high** chance to drop the Megingjord
  - Greylings have a low chance to drop the Megingjord again. They're little keptomaniacs with sticky fingers. Who knows what they find lying about.

v0.6.19
- Fixed intro text bug
- Trophy Saga
  - Patched Fermenter differently to fix bug where Fermenter would sometimes work at normal speed instead of bonkers-fast as intended.
  - Upped Greydwarf Brute boss item drop rate slightly

v0.6.18
- Added mod whitelist support and whitelisted "Wearable Trophies" mod by Jere Kuusela (https://thunderstore.io/c/valheim/p/JereKuusela/Wearable_Trophies/)
- Edits to flavor text
- Trophy Saga
  - Adjusted Greyling drop rates to reduce inventory clutter and provide better early game experience

v0.6.17
- Increased Point value of Bonemass to 100 (from 80)
- Increased Point value of Yagluth to 160 (from 120)

v0.6.16
- Adjusted Point Value of Kvastur trophy to 25 (from 35)
- Adjusted Point Value of Serpent trophy to 45 (from 25)
- Changed Luck-o-Meter to use ACTUAL kill/drop values instead of player ones so it's now a very accurate assessment of how lucky your run was
- Added Point value for Trophies to tooltips

v0.6.15
- New intro text for all game modes
- Trophy Saga
  - Logouts are cheaper, -10 points instead of -15

v0.6.14
- Trophy Saga
  - Reduced amount of Finewood Greylings can drop by half
  - Nerfed beehives as they were OP, now twice as fast as vanilla and same capacity

v0.6.13
- Kvastur Trophy
  - Added support for Kvastur Trophy for those who want to go whack a broom in the face.
  - Provisional score set to 35 points, pending review by the Trophy Hunt staff

v0.6.12
- Trophy Saga
  - Greylings **no longer drop Megingjord**, but now have a 100% chance to drop some amount of Finewood%
  - **Trolls** have a 66% chance to drop **Megingjord**, and drop five more hide than they normally would.
  - Mining is now **twice** as productive!
  - Oozers and Blobs drop more Ooze when killed
  - Beehives now hold twice as much honey and bees poop it out super fast
  - New intro text for Saga

v0.6.10
- Trophy Saga
  - **Removed Biome Bonuses!**
	- This feature was deemed inappropriate for Trophy Saga mode. Trophy Rush still has Biome Bonuses.
  - Trophies now drop at 2x the normal rate (capped at 50%)

v0.6.9
- Trophy Saga
  - Adjusted Greyling and Boss Minion drop rates
	- Greyling
	  - drops more Finewood and Corewood and drops them more frequently
	  - increased chance to drop Megingjord
	  - other small adjustments
	- Drake
	  - increased chance to drop Moder's Tears and can drop up to two at once
	- Fuling Shaman and Berserker
	  - increased chance to drop Torn Spirit
	- Seeker Soldier
	  - increased chance to drop Majestic Carapace
  - Slightly increased sailing speed
  - Slightly increased Trophy drop rates

v0.6.8
- Trophy Saga
  - Sap Collector extracts 100 times faster, 20 sap per tap. Thanks @RustyCali
  - Increased drop rates of Boss Key Items from Boss Minions
  - Dverger and Dverger mages all drop a dozen or so Yggdrasil wood

v0.6.7
- Trophy Saga
  - Hot Tub no longer chews through your wood, preventing you from getting a good hot soak (caused by fast-processing change for other buildings)

v0.6.6
- Trophy Saga
  - Adjusted drop rates for Boss Minions
	- Greydwarf Brute drops Crypt Key more frequently
	- Oozer drops Wishbone more frequently
	- Drake drops Moder's Tears slightly more frequently and Geirrhafa drops more tears when killed
	- Fuling Shaman AND Fuling Berserker both have a chance to drop Torn Spirit, and chance increased
	- Seeker Soldier drops Majestic Carapace slightly less frequently

v0.6.5
- Fixed a bug with game timer where it would show up when it wasn't supposed to, and refuse to go away.
- Trophy Saga
  - All Player Skills are learned at level 10 and then increase normally.
- A little overdue code cleanup

v0.6.4
- Trophy Rush
  - Trophies stop dropping 100% and drop at normal rate after you've picked one up. Avoids flooding player inventory with extra trophies. The exception is Deer, which still always drops.

v0.6.3
- Ack, documentation (README.MD) didn't update correctly

v0.6.2
- Tested against Bog Witch update on Public Test and verified it works (had to make a small change) and prepared for some changes in that update when they go live (new trophy!)
- Updated documentation (this document) to make it more complete and informative
- Added Game Timer
  - On by default in Saga (counting down) as this will eventually be used for gameplay
  - Off by default in Trophy Hunt and Trophy Rush game modes
  - Starts when character initially spawns in the world
  - Always running, but can be shown and hidden in other modes with `/timer show` or `/timer hide`
	- `/timer start` - start the timer if stopped
	- `/timer stop` - stop/pause the timer
	- `/timer reset` - reset the timer to zero seconds elapsed
	- `/timer show` - show the on-screen HUD timer
	- `/timer hide` - hide the on-screen HUD timer
	- `/timer set` - allows you to specify how much time has elapsed to manually set the timer. One hour fifteen minutes and five seconds would be entered as `/timer set 01:15:05`
	- `/timer toggle` - switches between countdown mode (default) and count up mode (red when counting down, yellow when counting up)
  - Automatically pauses at ESC pause menu while playing
  - Support for auto-updating the timer based on @jv's event timer at http://valheim.help when tournaments are active!
- A few UI tweaks. Resolution scaling seems to be working, made some text read better.

v0.6.1
- Added loading time indicator to see that TrophyHuntMod is running
- Found a solution for icon and text scaling that should be resolution independent, making the score text, deaths and logs icons and text look correct on various resolutions. Should fix the "tiny on 4k screens" issue as well as the "bloated on small laptops" issue

v0.6.0
- Trophy Saga
  - Combat Difficulty is now set to **Normal** (was Hard)
  - Portals now allow **all** items through (metals, eggs, etc.)
  - Biome Boss Minions now have a chance to drop Boss Items
	- Black Forest: Greydwarf Brute has a chance to drop Crypt Key
	- Swamp: Oozers have a chance to drop Wishbone
	- Mountains: Drakes have a chance to drop Moder's Tears
	- Plains: Fuling Shaman have a chance to drop Torn Spirit
	- Mistlands: Seeker Soldiers have a chance to drop Majestic Carapace
  - Hildir Mini-bosses always drop their Biome's Boss Item
  - Rancid Remains always drops his mace (Iron Mace, non-poisonous)
  - Production buildings that take time to process **all** take only seconds
	- Fermenter (as in previous version)
	- Charcoal Kiln
	- Spinning Wheel
	- Windmill
	- Eitr Refiner (this also **no longer requires Soft Tissue**, just Sap)
	- NOTE: Cooking buildings are unchanged from vanilla (Cooking Station, Iron Cooking Station, Cauldron, Stone Oven)
  - All planted seeds and saplings grow to maturity within 10 seconds (as in previous version)
  - Greyling drop rates adjusted again, Greylings have a chance to drop Megingjord
- Fixed a glitch with Geirrhafa's trophy where it would stick to the player in game modes where Biome Bonuses were enabled
- Updated Trophy Saga description text
 
v0.5.16
- Trophy Saga
  - Bah! Forgot to update the main menu description text for Saga. Again. Nothing else changed, just the description text.

v0.5.15
- Trophy Saga
  - Yeast and bacteria collected from soil at the roots of the World Tree have been dispersed throughout the tenth world. These ancient micro-organisms were once known only to the three goddesses of past, present and future. The Norns' secret has escaped!
	- Fermenters now create mead in seconds instead of days
	- Crops grow to full maturity in seconds instead of days
  - Loki continues to toy with the kleptomaniac Greylings!
	- Drop rates of Greyling items adjusted

v0.5.14
- Trophy Saga
  - Tweaks to Greyling drop rates after some playtesting

v0.5.13
- Trophy Saga
  - Loki has been meddling. Greylings can now drop various useful items. One of them is **VERY** useful.
  - Svartalfar Fermenter overclocking has resulted in double output for all Fermenters

v0.5.12
- Added "Earned Points/Penalties" lines to Score tooltip to make it easier to read and to reinforce what a disaster you are for dying at all. 
- Trophy Saga balance changes
  - Enabled Biome Bonuses for Saga mode
  - Increased Resource Rate to 2x (was 1.5x)
  - Slightly increased Trophy drop rate
  - Fixed Trophy Saga description text. Forgot to add "Raids Disabled"

v0.5.11
- Decreased distance between Map pins for `/showpath` for better player path display during `exploremap` sessions after runs
- Trophy Saga
  - Disabled Raids in Saga mode to decrease potential player time near their bases
- Experimental
  - Added `/elderpowercutsalltrees` Chat Console command (off by default, invalidates session for tournament play) which allows all trees to be cut down by all axes if The Elder Forsaken Power is **currently active**

v0.5.10
- Trophy Saga: Fixed a bug with insta-smelt where ores from chests would register as 1-weight when moved to player inventory. Thanks @SobewanKenob!
- Trophy Saga: Added ability to insta-smelt directly out of chests (no long requires tossing ores on the ground and picking them up)

v0.5.9
- Fixed a Trophy Saga side effect where ore weight was used to calculate inventory weight instead of metal bar weight, which often inexplicably weighs more than the ore it comes from
- Reduced Trophy drop rate in Trophy Saga (still higher than default wiki trophy drop rates)

v0.5.8
- Trophy Saga
	- Made unfound trophies dark blue to denote Saga mode
	- Upped Death Penalty to -30 points
	- Upped Logout Penalty to -15 points

v0.5.7
- Trophy Hunt and Trophy Rush balance changes!
	- Trophy Huntw
		- Removed Biome Bonuses from Trophy Hunt game mode.
	- Trophy Rush
		- Decreased Trophy Rush resource drop rate to 2x (was 3x)
		- Increased Trophy Rush death penalty to -10 Points (was -5 Points)
		- Added Trophy Rush "/die" penalty at -20 Points
- Added Trophy Saga game mode (experimental, use at your own risk)
	- Resources drop rate at 1.5x
	- Combat mode is Hard
	- Trophies drop at higher than wiki rates
	- No biome bonuses
	- Svartalfar powers!
		- All ores and scrap player picks up *off the ground* Insta-Smelt(tm) when they hit your inventory thanks to svartalf hand magic
		- Boats are twice as fast due to svartalf sail strength
	- Logouts and Deaths are both -5 Points
- Accidentally put the Trophy Rush clarification text on the Trophy Hunt description, oops.. moved it to the Trophy Rush description as intended.
- Fixed a bug where clicking the "Toggle Game Mode" button and then hitting enter at the main menu would press the button again AND advance the Valheim UI screen, resulting in the wrong game mode being selected accidentally.
- Cleaned up some UI code to make room for future changes.
- Fixed a bug where changing game modes or Worlds wouldn't reset stats, logouts and other session-specific data

v0.5.6
- Fixed a bug where `/showalltrophystats` would persist when switching characters and/or game modes
- Enabled reporting of gamemode to Tracker backend at valheim.help for future experiments and embiggenings
- Added clarifying UI text for Trophy Rush if using it on an existing world.

v0.5.5
- Added Biome Completion Bonuses!
	- Implemented this so we can try it out and see what we think. Using suggested score values from @Archy
	- Adds additional points for completing *all* of the trophies in a given Biome (bosses and Hildir quest trophies excluded)
	    ```+20  Meadows
	    +40  Black Forest
	    +40  Swamp
	    +60  Mountains
	    +60  Plains
	    +80  Mistlands
	    +100 Ashlands
	- Added festive animation to Trophy icons for when you complete a Biome. You spin me right 'round, baby, right 'round.
	- Thanks to @Warband for the suggestion of Biome Bonuses!
- Added Biome Bonus tally to Score tooltip
- Removed "Total Score" from Score tooltip since it was redundant and took up space needed for Biome Bonus tallying
- Fixed a bug with Score tooltip where it got cut off on the left side of the screen.

v0.5.0
- Official support for two Game Modes (**Trophy Hunt**, and **Trophy Rush**) in UI and HUD
	- "Toggle Trophy Rush" button on Main Menu replaced with "Toggle Game Mode" which cycles between Trophy Hunt and Trophy Rush game modes
	- Game Mode Rules are listed under the game mode on the Main Menu including Logout and Death penalties
	- Trophy Rush
		- Creating new world with this option enabled will default to Trophy Rush settings in World Modifiers
			- Resources x3
			- Very Hard Combat
			- Logout Penalty: -5 points
			- Death Penalty: -5 points
		- Once the world is created, you can still change World Modifiers for the world however you like, but having Trophy Rush enabled when creating a New World will create a world with the above modifiers by default.
	- Trophy Hunt
		- Standard "Normal Settings" world modifiers are applied as per normal for Valheim
			- Resources x1
			- Normal Combat
			- Logout Penalty: -10 points
			- Death Penalty: -20 points
	- Wrote code to report game mode to online Tracker along with other data, disabled for now
	- No longer color score green in Trophy Rush showing that it's invalid for tournament play, since this is being considered for a tournament variant. Unfound trophy icons are still dark red, indicating Trophy Rush is enabled.
- Reordered the Trophy icons in the HUD by moving Ocean (Serpent) and the four Hildir's Quest trophies to the end of the list. Thanks for the suggestion @Warband
- Added Score Tooltip showing score breakdown and penalty costs for the current game mode (Trophy Hunt or Trophy Rush)
- Show All Trophy Stats Feature
	- Added `/showalltrophystats` Chat Console command to replace the poorly-named and hard to find `/showallenemydeaths` console command. Thanks @Spazzyjones and @Threadmenace for having a hard time finding it.
	- Removed the button from the Main Menu since the command line toggle is available in game.
	- Fixed a bug where the tooltip would lose its shit and flash erratically when hovering over a trophy with `/showalltrophystats` enabled.
- Fixed visual bug with Logouts count where it wouldn't update when you first started playing a new character if you'd previous had a logout on another one (actual score was still correct, though)
- Fixed formatting error with Luck-O-Meter tooltip on luckiest and unluckiest percentages where it would display "a million trillion" decimal places.
- Fixed Luck-O-Meter tooltip getting clipped off the left edge of the screen
- Increased Score font size slightly
- Made trophy pickup animation even more eye catching for better stream visibility

v0.4.1
- A few visual tweaks to the Relog counter icon
- Fixed a bug with animating Trophy offset

v0.4.0
- Killed/Trophy-drop Tooltips Nerfed!
	- These were discussed and determined to be OP.
	- Killed/Trophy Drop tooltips *now use PLAYER enemy kills and PLAYER trophy pickups* rather than world enemy deaths and world trophy drops!
		- This was done to prevent a cheese where you could monitor world trophy drops after, say, dragging Growths over to a Fuling village, letting madness ensue, and then checking world trophy drops to see if you should run over there and hunt for trophies.
		- For a Trophy to count towards stats in the tooltips, it must be picked up into your inventory
	- Luck-O-Meter now only counts Player enemy kills and picked up Trophy drops when calculating Luck Rating, Luckiest and Unluckies mobs
	- BUT! You can now use `/showallenemydeaths` Chat Console command to see all the info including world kills and world trophy drops (this adds the data we had before back to the tooltips).
		- WARNING: This invalidates your run for Tournament play and colors your score bright green to indicate this. EX: Use this at the end of a run to inspect actual world drops.
	- Added a new button on the main menu "Show All Enemy Deaths" to enable/disable this prior to gameplay for the console-command shy.
	- Note that once it's been enabled your run is invalid even if you disable it again (score remains bright green.)
- Added "Logs: X" text to HUD to display how many Relogs have been done. Much requested. Note that `/trophyhunt` also still displays this information in the chat console and log file.
- Added `/ignorelogouts` Chat Console command to make it so that logouts no longer count against your score. (@gregscottbailey request)
	- WARNING: This invalidates the run for Tournament play. "Logs:" text will display dimmed and score bright green if you use this.
- Changed Trophy animation when you get your first trophy to read better on streams (Yes, again. I think it's less jarring AND more visible now.)
- Repositioned Deaths counter on the HUD to read better and take up less space
- Removed Luck-O-Meter "Luck" text and repositioned icon in HUD
- Reworked Luck-O-Meter tooltip to make it easier to read and less deluxe
- Added black outline to Score text to make it more readable against light backgrounds like the Mountains or staring at the sun.
- Fixed a bug where logging out, deleting your character, creating a new character with the same name and entering play would retain old player data for kills, trophy drops and /showpath pins (thanks @da_Keepa and @Xmal!)
- Fixed a bug with Luck Rating where my logic was reversed and it would display luck ratings if no luck was calculable yet
- Fixed a bug where animating trophy would drift upwards on screen if pausing while it was flashing

v0.3.3
- Added `/scorescale` chat console command to alter the score text size. 1.0 is the default, can go as low or high as you like. Use `/scorescale 1.5` to increase the text size by 50%. Thanks @turbero.
- Added `/trophyspacing` chat console command to pack them closer together or farther apart at the bottom of the screen. Negative values pack them tighter, positive ones space them out. 1.5 looks pretty good for me at 1920x1080 running in a window. YMMV. Thanks @Daizzer.
- Animate the discovered trophies upwards while they pulsate and flash to make them **even more** obvious and eye-catching for players.

v0.3.2
- Fixed UI text overrun on overall Luck tooltip for long enemy names
- Don't display luck rating in Trophy or Luck-O-Meter tooltips if not enough enemies have been killed to really tell
- added `/showtrophies` chat console command to toggle show/hide of trophy icons, Score, Deaths and Luck counters still display

v0.3.1
- Simplified the Luck-o-Meter and added luckiest and unluckiest trophies to the tooltip

v0.3.0
- Added "Luck-o-Meter" as suggested by @da_Keepa. 
	- Luck icon is on the left of the HUD above the Deaths counter
	- Hover text shows luck percentage as well as overall Luck Rating
	- Individual Trophy icons how show Luck Rating for that type of trophy
	- Luck is calculated as actual drop rates versus documented droprates. Overall luck is aggregate luck for all trophy capabable enemies that have died at least once.
- Trophy Rush Changes (Experimental Feature)
	- Fixed a bug that was preventing trophies from spawning in all circumstances
	- Added TrophyRush button to Main Menu below new logo position
- Made trophy icons animate a little longer when they're picked up
- Fixed a display-only bug where -10 would display as your starting score if you were playing another character, logged out, and made a new one. This just displayed wrong, and would correct itself on next score update and didn't ACTUALLY count against your score.
- Adjusted un-found Trophy icons in the tray to be more readable, is this better? Let me know
- Reduced size of TrophyHuntMod logo and moved it to the right side of the screen as suggested by @Kr4ken92 to play nicer with other mods

v0.2.4
- Swanky new Main Menu logo displaying "Trophy Hunt!" and the mod's version number
- Added tooltips to the Trophy icons at the bottom screen if you pause the game (ESC) and hover the mouse over them.
	- Only available in-game at the Pause (ESC) menu (not in-play with the Inventory screen open!)
	- This displays:
		- Trophy name
		- The number of enemies killed that could drop that Trophy
		- Number of trophies actually dropped by those enemies
		- Actual drop percentage
		- Wiki-documented drop percentage
- Experimental F5 console command `trophyrush` at the main menu, which enables Trophy Rush Mode.
	- Trophy Rush mode causes every enemy that WOULD drop a Trophy to drop a Trophy 100% of the time. This was suggested by @FizzyP as a potential new trophy hunt contest type so it's in there for experimentation.
	- This can only be enabled at the Main Menu via the F5 console command
	- Unfound Trophies will be colored RED in the hud to indicate Trophy Rush is enabled.
	- NOTE! This is the ONLY feature of TrophyHuntMod which modifies the behavior of Valheim. Please use with caution!

v0.2.3
- Removed "TrophyDraugrFem" from the trophy list since it's not supported in the game and does not drop.
- Decreased default HUD trophy size slightly
- TrophyHuntMod now detects whether it's the only mod running and reports this to the log file and displays the score in light blue instead of yellow.
	Yellow score means it's the only mod, which is required for the Trophy Hunt events.
	Light Blue score means other mods are present.
- Corrected the readme which listed the trophy HUD scaling command as `trophysize` instead of `/trophyscale` which is the correct command.

v0.2.2
- Increased the base size of trophies so they read better on screen for the stream audience.
- Added `/trophyscale` console command to allow the user to scale the size of the trophies at the bottom of the screen. Default is 1.0, and can be set as low as 0.1 and as high as you like. This will help adjust trophies to be more readable for streamers at some screen sizes.
	To increase the size of the trophies, hit <enter> to bring up the Chat Console and type `/trophyscale 1.5` for example. This would increase the trophy sizes by 50%
- Made the animation that plays when you collect a trophy more visible by flashing it on and off as well as animating the size. This makes it easier for runners to know when they picked one up without hunting for it on the trophy bar at the bottom.

## Trophy Hunt Mod Features

Displays a tray at the bottom of the game screen with the computed Trophy Hunt score on the left, and each Trophy running to the right. Trophies are grouped by Biome, and are displayed in silhouette when not yet acquired, and in full color when acquired.

A death counter appears to the left of the health and food bar, as deaths count against point totals in Trophy Hunt.

## Console Commands

`/trophyhunt`

	The Chat console (and F5 console) both support the console command `/trophyhunt` which prints out the Trophy Hunt scoring in detail like so:

	```
	[Trophy Hunt Scoring]
	Trophies:
	  TrophyBoar: Score: 10 Biome: Meadows
	  TrophyDeer: Score: 10 Biome: Meadows
	  TrophyNeck: Score: 10 Biome: Meadows
	  TrophyEikthyr: Score: 40 Biome: Meadows
	  TrophyGreydwarf: Score: 20 Biome: Forest
	Trophy Score Total: 90
	Penalties:
	  Deaths: 2 Score: -40
	  Logouts: 0 Score: 0
	Total Score: 50
	```
`/timer`

	Allows control of an in game timer display for four hour Trophy runs. Works in all game modes.

	- `/timer start` - start the timer if stopped
	- `/timer stop` - stop/pause the timer
	- `/timer reset` - reset the timer to zero seconds elapsed
	- `/timer show` - show the on-screen HUD timer
	- `/timer hide` - hide the on-screen HUD timer
	- `/timer set` - allows you to specify how much time has elapsed to manually set the timer. One hour fifteen minutes and five seconds would be entered as `/timer set 01:15:05`
	- `/timer toggle` - switches between countdown mode (default) and count up mode (red when counting down, yellow when counting up)

`/showpath`

	This will display pins on the in-game Map showing the path that the Player has traveled during the session. One pin every 100 meters or so.

`/trophyscale`

	This allows the user to scale the trophy sizes (1.0 is default) for better readability at some screen resolutions. 

`/trophyspacing`

	Allows you space out the trophies to your liking. Negative values spaces them tighter, positive values space them out more. They may wrap off the end of the screen with large values.

`/scorescale`

	Allows the user to scale the Score text size (1.0 is default) for better readability at some screen resolutions.

`/showtrophies`

	Toggles the display of Trophy icons at the bottom of the screen for when you can't even, or the display conflicts with other mods

`/showalltrophystats` 

	Chat Console command to see all the info including world kills and world trophy drops (this adds the data we had before back to the tooltips).
	- WARNING: This invalidates your run for Tournament play and colors your score bright green to indicate this.

`/ignorelogouts`
	
	Chat Console command to make it so that logouts no longer count against your score. 
	- WARNING: This invalidates your run for Tournament play and colors your score bright green and fades Logs: text to gray to indicate this.

## Support the Valheim Speedrunning Community!
If you'd like to donate a dollar or two to the speedrunners and the Trophy Hunt Events, please consider donating via CashApp or PayPal. All the money goes directly into the prize pool for future Trophy Hunt events! 

You can learn more on the Valheim Speedrun Discord channel here: https://discord.gg/9bCBQCPH

	CashApp: $ARCHYCooper 
	PayPal: https://www.paypal.com/paypalme/expertarchy

## Known issues

## Feature Requests

- Report score and trophies to the valheim.help tracker during runs
- Dropshadow or add dark background field to Score (Weih (Henrik))
- Collect player kills/drops as default, enable all kills/drops as options


## Where to Find
You can find the github at: https://github.com/smariotti/TrophyHuntMod

Note, this was originally built with Jotunn, using their example mod project structure, though Jotunn is no longer a requirement to run it. You just need to have BepInEx installed.
# TrophyHuntMod

This is a BepInEx mod for Valheim for the Valheim Trophy Hunt that displays discovered/undiscovered trophies at the bottom edge of the screen along with a computed score for the Trophy Hunt based on current scoring rules. 

It offers three modes, details are provided below.
- **Trophy Hunt**
  - This is the normal Trophy Hunt. Valheim is not modified in any way. You play on default settings. No aspects of Valheim are modified in any way.
- **Trophy Rush**
  - This is an advanced version of the Trophy Hunt. Combat Difficulty is set to Very Hard, Resources drop at 2x rate and all Trophy-bearing enemies drop them 100% of the time.
- **Trophy Saga**
  - This is a highly modified Trophy Hunt where many game mechanics are altered to speed up progression such as production building speeds, enemy drops, and crop growth times

Available here:

https://thunderstore.io/c/valheim/p/oathorse/TrophyHuntMod/

Requires a BepInEx install.

https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/

## Installation (manual)

Two Options:
- Use r2modman to automatically install it from Thunderstore and launch Valheim. R2modman is the mod manager available for download at https://thunderstore.io
- Manual: Download and install BepinEx_Valheim, then simply copy the contents of the archive into the BepinEx/Plugins directory. This is usually found somewhere like 'C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins' if you've installed BepInEx according to the instructions. (https://www.nitroserv.com/en/guides/installing-mods-on-valheim-with-bepinex)

# Trophy Hunt Game Modes

- You have 4 hours to score as many points as you can.
- Points are scored by picking up trophies, with point values assigned by biome. You only get points for the first Trophy of that type, and it must enter your inventory to count. You can drop it immediately if you like.
- Points are subtracted for dying or re-logging to the main menu (to clear aggro, etc)
- Some game modes offer additional score bonuses

## Point Values

Scores are awareded from the following points table:
```
**Enemies**
Meadows Trophies - 10 points each
Black Forest & Swamp Trophies - 20 points each
Mountain/Plains Trophies - 30 points each
Mistland Trophies -  40 points each
Ashland Trophies - 50 points each
Serpent Trophy - 45 points
Kvastur Trophy - 25 points

**Mini-Bosses**
Brenna Trophy - 25 points
Geirrhafa Trophy - 45 points
Zil & Thungr Trophies - 65 points

**Bosses**
Eikthyr Trophy - 40 points
Elder Trophy - 60 points
Bonemass Trophy - 100 points
Moder Trophy - 100 points
Yagluth Trophy - 160 points
Queen/Fader Trophy - 1000 points

(Queen/Fader points will get nerfed when/if someone completes)
```

## Trophy Hunt

This is the standard Trophy Hunt game mode, played at completely stock/vanilla Valheim settings for "Normal" game mode.

- Trophies drop at standard Valheim rates (documented on the wiki and displayed in the Trophy tooltips in-game, pause the game to see the tooltips on the HUD Trophies)
- Death penalty is -20 points
- Relog penalty is -10 points

## Trophy Rush

This Trophy Hunt variant has the following changes from Trophy Hunt

- Trophies drop at 100% drop rate for any enemy that can drop one
- Combat is set to Very Hard (Normal in Trophy Hunt)
- Resource Rate is set to 2x (you get double all resources picked up, dropped by enemies, or found in chests)
- Death penalty is -10 points (half as much as Trophy Hunt)
- Relog penalty is -5 points (half as much as Trophy Hunt)
- /die penalty is an additional -10 points (total -20 points for using /die Console command)

In addition, this mode offers "Biome Bonuses" for completing the full set of non-boss Trophies for a given Biome:
```
**Biome Bonuses**
+20 Meadows
+40 Black Forest
+40  Swamp
+60  Mountains
+60  Plains
+80  Mistlands
+100 Ashlands
```

## Trophy Saga

This Trophy Hunt variant implements a good number of modifications to standard Valheim play to speed progression. Bosses no longer gate upgrade progression since boss dropped items can now drop from powerful minions in that boss' biome.

- Trophies drop at 2x normal rate (capped at 50% chance)
- Combat is set to Normal difficulty
- Resource Rate is set to 2x (you get double all resources picked up, dropped by enemies, or found in chests)
- Death penalty is -30 points
- Relog penalty is -15 points
- Metal Ores "insta-smelt" when you pick them up. If you pick up ore from the ground or out of a chest, it becomes the equivalent metal bar instantly.
- Mining is twice as productive
- Portals allow **all** items through (metals, eggs, etc.)
- Modified Enemy Loot
	- Greylings drop various useful Meadows and Black Forest items, including Finewood
	- Trolls have a 66% chance to drop Megingjord
	- Biome Boss Minions now have a chance to drop Boss Items
	  - Black Forest: Greydwarf Brute has a chance to drop Crypt Key
	  - Swamp: Oozers have a chance to drop Wishbone
	  - Mountains: Drakes have a chance to drop Moder's Tears
	  - Plains: Fuling Shaman and Berserker have a chance to drop Torn Spirit
	  - Mistlands: Seeker Soldiers have a chance to drop Majestic Carapace
	  - Hildir Mini-bosses always drop their Biome's Boss Item
	- Dverger drop a dozen or so pieces of Yggrasil wood when they die
	- Rancid Remains always drops his mace (Iron Mace, non-poisonous)
- Production buildings that take time to process **all** take only seconds
  - Fermenter
  - Beehive
  - Charcoal Kiln
  - Spinning Wheel
  - Windmill
  - Sap Extractor
  - Eitr Refiner (this also **no longer requires Soft Tissue**, just Sap)
  - NOTE: Cooking buildings are unchanged from vanilla (Cooking Station, Mead Ketill, Iron Cooking Station, Cauldron, Stone Oven)
- Pickaxes are more productive when mining
- All planted seeds and saplings grow to maturity within 10 seconds (as in previous version)
- All Player Skills are learned at level 10 and then increase normally.

## What's New?
v0.6.17
- Increased Point value of Bonemass to 100 (from 80)
- Increased Point value of Yagluth to 160 (from 120)

## Previous Changes
v0.6.16
- Adjusted Point Value of Kvastur trophy to 25 (from 35)
- Adjusted Point Value of Serpent trophy to 45 (from 25)
- Changed Luck-o-Meter to use ACTUAL kill/drop values instead of player ones so it's now a very accurate assessment of how lucky your run was
- Added Point value for Trophies to tooltips

v0.6.15
- New intro text for all game modes
- Trophy Saga
  - Logouts are cheaper, -10 points instead of -15

v0.6.14
- Trophy Saga
  - Reduced amount of Finewood Greylings can drop by half
  - Nerfed beehives as they were OP, now twice as fast as vanilla and same capacity

v0.6.13
- Kvastur Trophy
  - Added support for Kvastur Trophy for those who want to go whack a broom in the face.
  - Provisional score set to 35 points, pending review by the Trophy Hunt staff

v0.6.12
- Trophy Saga
  - Greylings **no longer drop Megingjord**, but now have a 100% chance to drop some amount of Finewood%
  - **Trolls** have a 66% chance to drop **Megingjord**, and drop five more hide than they normally would.
  - Mining is now **twice** as productive!
  - Oozers and Blobs drop more Ooze when killed
  - Beehives now hold twice as much honey and bees poop it out super fast
  - New intro text for Saga

v0.6.10
- Trophy Saga
  - **Removed Biome Bonuses!**
	- This feature was deemed inappropriate for Trophy Saga mode. Trophy Rush still has Biome Bonuses.
  - Trophies now drop at 2x the normal rate (capped at 50%)

v0.6.9
- Trophy Saga
  - Adjusted Greyling and Boss Minion drop rates
	- Greyling
	  - drops more Finewood and Corewood and drops them more frequently
	  - increased chance to drop Megingjord
	  - other small adjustments
	- Drake
	  - increased chance to drop Moder's Tears and can drop up to two at once
	- Fuling Shaman and Berserker
	  - increased chance to drop Torn Spirit
	- Seeker Soldier
	  - increased chance to drop Majestic Carapace
  - Slightly increased sailing speed
  - Slightly increased Trophy drop rates

v0.6.8
- Trophy Saga
  - Sap Collector extracts 100 times faster, 20 sap per tap. Thanks @RustyCali
  - Increased drop rates of Boss Key Items from Boss Minions
  - Dverger and Dverger mages all drop a dozen or so Yggdrasil wood

v0.6.7
- Trophy Saga
  - Hot Tub no longer chews through your wood, preventing you from getting a good hot soak (caused by fast-processing change for other buildings)

v0.6.6
- Trophy Saga
  - Adjusted drop rates for Boss Minions
	- Greydwarf Brute drops Crypt Key more frequently
	- Oozer drops Wishbone more frequently
	- Drake drops Moder's Tears slightly more frequently and Geirrhafa drops more tears when killed
	- Fuling Shaman AND Fuling Berserker both have a chance to drop Torn Spirit, and chance increased
	- Seeker Soldier drops Majestic Carapace slightly less frequently

v0.6.5
- Fixed a bug with game timer where it would show up when it wasn't supposed to, and refuse to go away.
- Trophy Saga
  - All Player Skills are learned at level 10 and then increase normally.
- A little overdue code cleanup

v0.6.4
- Trophy Rush
  - Trophies stop dropping 100% and drop at normal rate after you've picked one up. Avoids flooding player inventory with extra trophies. The exception is Deer, which still always drops.

v0.6.3
- Ack, documentation (README.MD) didn't update correctly

v0.6.2
- Tested against Bog Witch update on Public Test and verified it works (had to make a small change) and prepared for some changes in that update when they go live (new trophy!)
- Updated documentation (this document) to make it more complete and informative
- Added Game Timer
  - On by default in Saga (counting down) as this will eventually be used for gameplay
  - Off by default in Trophy Hunt and Trophy Rush game modes
  - Starts when character initially spawns in the world
  - Always running, but can be shown and hidden in other modes with `/timer show` or `/timer hide`
	- `/timer start` - start the timer if stopped
	- `/timer stop` - stop/pause the timer
	- `/timer reset` - reset the timer to zero seconds elapsed
	- `/timer show` - show the on-screen HUD timer
	- `/timer hide` - hide the on-screen HUD timer
	- `/timer set` - allows you to specify how much time has elapsed to manually set the timer. One hour fifteen minutes and five seconds would be entered as `/timer set 01:15:05`
	- `/timer toggle` - switches between countdown mode (default) and count up mode (red when counting down, yellow when counting up)
  - Automatically pauses at ESC pause menu while playing
  - Support for auto-updating the timer based on @jv's event timer at http://valheim.help when tournaments are active!
- A few UI tweaks. Resolution scaling seems to be working, made some text read better.

v0.6.1
- Added loading time indicator to see that TrophyHuntMod is running
- Found a solution for icon and text scaling that should be resolution independent, making the score text, deaths and logs icons and text look correct on various resolutions. Should fix the "tiny on 4k screens" issue as well as the "bloated on small laptops" issue

v0.6.0
- Trophy Saga
  - Combat Difficulty is now set to **Normal** (was Hard)
  - Portals now allow **all** items through (metals, eggs, etc.)
  - Biome Boss Minions now have a chance to drop Boss Items
	- Black Forest: Greydwarf Brute has a chance to drop Crypt Key
	- Swamp: Oozers have a chance to drop Wishbone
	- Mountains: Drakes have a chance to drop Moder's Tears
	- Plains: Fuling Shaman have a chance to drop Torn Spirit
	- Mistlands: Seeker Soldiers have a chance to drop Majestic Carapace
  - Hildir Mini-bosses always drop their Biome's Boss Item
  - Rancid Remains always drops his mace (Iron Mace, non-poisonous)
  - Production buildings that take time to process **all** take only seconds
	- Fermenter (as in previous version)
	- Charcoal Kiln
	- Spinning Wheel
	- Windmill
	- Eitr Refiner (this also **no longer requires Soft Tissue**, just Sap)
	- NOTE: Cooking buildings are unchanged from vanilla (Cooking Station, Iron Cooking Station, Cauldron, Stone Oven)
  - All planted seeds and saplings grow to maturity within 10 seconds (as in previous version)
  - Greyling drop rates adjusted again, Greylings have a chance to drop Megingjord
- Fixed a glitch with Geirrhafa's trophy where it would stick to the player in game modes where Biome Bonuses were enabled
- Updated Trophy Saga description text
 
v0.5.16
- Trophy Saga
  - Bah! Forgot to update the main menu description text for Saga. Again. Nothing else changed, just the description text.

v0.5.15
- Trophy Saga
  - Yeast and bacteria collected from soil at the roots of the World Tree have been dispersed throughout the tenth world. These ancient micro-organisms were once known only to the three goddesses of past, present and future. The Norns' secret has escaped!
	- Fermenters now create mead in seconds instead of days
	- Crops grow to full maturity in seconds instead of days
  - Loki continues to toy with the kleptomaniac Greylings!
	- Drop rates of Greyling items adjusted

v0.5.14
- Trophy Saga
  - Tweaks to Greyling drop rates after some playtesting

v0.5.13
- Trophy Saga
  - Loki has been meddling. Greylings can now drop various useful items. One of them is **VERY** useful.
  - Svartalfar Fermenter overclocking has resulted in double output for all Fermenters

v0.5.12
- Added "Earned Points/Penalties" lines to Score tooltip to make it easier to read and to reinforce what a disaster you are for dying at all. 
- Trophy Saga balance changes
  - Enabled Biome Bonuses for Saga mode
  - Increased Resource Rate to 2x (was 1.5x)
  - Slightly increased Trophy drop rate
  - Fixed Trophy Saga description text. Forgot to add "Raids Disabled"

v0.5.11
- Decreased distance between Map pins for `/showpath` for better player path display during `exploremap` sessions after runs
- Trophy Saga
  - Disabled Raids in Saga mode to decrease potential player time near their bases
- Experimental
  - Added `/elderpowercutsalltrees` Chat Console command (off by default, invalidates session for tournament play) which allows all trees to be cut down by all axes if The Elder Forsaken Power is **currently active**

v0.5.10
- Trophy Saga: Fixed a bug with insta-smelt where ores from chests would register as 1-weight when moved to player inventory. Thanks @SobewanKenob!
- Trophy Saga: Added ability to insta-smelt directly out of chests (no long requires tossing ores on the ground and picking them up)

v0.5.9
- Fixed a Trophy Saga side effect where ore weight was used to calculate inventory weight instead of metal bar weight, which often inexplicably weighs more than the ore it comes from
- Reduced Trophy drop rate in Trophy Saga (still higher than default wiki trophy drop rates)

v0.5.8
- Trophy Saga
	- Made unfound trophies dark blue to denote Saga mode
	- Upped Death Penalty to -30 points
	- Upped Logout Penalty to -15 points

v0.5.7
- Trophy Hunt and Trophy Rush balance changes!
	- Trophy Huntw
		- Removed Biome Bonuses from Trophy Hunt game mode.
	- Trophy Rush
		- Decreased Trophy Rush resource drop rate to 2x (was 3x)
		- Increased Trophy Rush death penalty to -10 Points (was -5 Points)
		- Added Trophy Rush "/die" penalty at -20 Points
- Added Trophy Saga game mode (experimental, use at your own risk)
	- Resources drop rate at 1.5x
	- Combat mode is Hard
	- Trophies drop at higher than wiki rates
	- No biome bonuses
	- Svartalfar powers!
		- All ores and scrap player picks up *off the ground* Insta-Smelt(tm) when they hit your inventory thanks to svartalf hand magic
		- Boats are twice as fast due to svartalf sail strength
	- Logouts and Deaths are both -5 Points
- Accidentally put the Trophy Rush clarification text on the Trophy Hunt description, oops.. moved it to the Trophy Rush description as intended.
- Fixed a bug where clicking the "Toggle Game Mode" button and then hitting enter at the main menu would press the button again AND advance the Valheim UI screen, resulting in the wrong game mode being selected accidentally.
- Cleaned up some UI code to make room for future changes.
- Fixed a bug where changing game modes or Worlds wouldn't reset stats, logouts and other session-specific data

v0.5.6
- Fixed a bug where `/showalltrophystats` would persist when switching characters and/or game modes
- Enabled reporting of gamemode to Tracker backend at valheim.help for future experiments and embiggenings
- Added clarifying UI text for Trophy Rush if using it on an existing world.

v0.5.5
- Added Biome Completion Bonuses!
	- Implemented this so we can try it out and see what we think. Using suggested score values from @Archy
	- Adds additional points for completing *all* of the trophies in a given Biome (bosses and Hildir quest trophies excluded)
	    ```+20  Meadows
	    +40  Black Forest
	    +40  Swamp
	    +60  Mountains
	    +60  Plains
	    +80  Mistlands
	    +100 Ashlands
	- Added festive animation to Trophy icons for when you complete a Biome. You spin me right 'round, baby, right 'round.
	- Thanks to @Warband for the suggestion of Biome Bonuses!
- Added Biome Bonus tally to Score tooltip
- Removed "Total Score" from Score tooltip since it was redundant and took up space needed for Biome Bonus tallying
- Fixed a bug with Score tooltip where it got cut off on the left side of the screen.

v0.5.0
- Official support for two Game Modes (**Trophy Hunt**, and **Trophy Rush**) in UI and HUD
	- "Toggle Trophy Rush" button on Main Menu replaced with "Toggle Game Mode" which cycles between Trophy Hunt and Trophy Rush game modes
	- Game Mode Rules are listed under the game mode on the Main Menu including Logout and Death penalties
	- Trophy Rush
		- Creating new world with this option enabled will default to Trophy Rush settings in World Modifiers
			- Resources x3
			- Very Hard Combat
			- Logout Penalty: -5 points
			- Death Penalty: -5 points
		- Once the world is created, you can still change World Modifiers for the world however you like, but having Trophy Rush enabled when creating a New World will create a world with the above modifiers by default.
	- Trophy Hunt
		- Standard "Normal Settings" world modifiers are applied as per normal for Valheim
			- Resources x1
			- Normal Combat
			- Logout Penalty: -10 points
			- Death Penalty: -20 points
	- Wrote code to report game mode to online Tracker along with other data, disabled for now
	- No longer color score green in Trophy Rush showing that it's invalid for tournament play, since this is being considered for a tournament variant. Unfound trophy icons are still dark red, indicating Trophy Rush is enabled.
- Reordered the Trophy icons in the HUD by moving Ocean (Serpent) and the four Hildir's Quest trophies to the end of the list. Thanks for the suggestion @Warband
- Added Score Tooltip showing score breakdown and penalty costs for the current game mode (Trophy Hunt or Trophy Rush)
- Show All Trophy Stats Feature
	- Added `/showalltrophystats` Chat Console command to replace the poorly-named and hard to find `/showallenemydeaths` console command. Thanks @Spazzyjones and @Threadmenace for having a hard time finding it.
	- Removed the button from the Main Menu since the command line toggle is available in game.
	- Fixed a bug where the tooltip would lose its shit and flash erratically when hovering over a trophy with `/showalltrophystats` enabled.
- Fixed visual bug with Logouts count where it wouldn't update when you first started playing a new character if you'd previous had a logout on another one (actual score was still correct, though)
- Fixed formatting error with Luck-O-Meter tooltip on luckiest and unluckiest percentages where it would display "a million trillion" decimal places.
- Fixed Luck-O-Meter tooltip getting clipped off the left edge of the screen
- Increased Score font size slightly
- Made trophy pickup animation even more eye catching for better stream visibility

v0.4.1
- A few visual tweaks to the Relog counter icon
- Fixed a bug with animating Trophy offset

v0.4.0
- Killed/Trophy-drop Tooltips Nerfed!
	- These were discussed and determined to be OP.
	- Killed/Trophy Drop tooltips *now use PLAYER enemy kills and PLAYER trophy pickups* rather than world enemy deaths and world trophy drops!
		- This was done to prevent a cheese where you could monitor world trophy drops after, say, dragging Growths over to a Fuling village, letting madness ensue, and then checking world trophy drops to see if you should run over there and hunt for trophies.
		- For a Trophy to count towards stats in the tooltips, it must be picked up into your inventory
	- Luck-O-Meter now only counts Player enemy kills and picked up Trophy drops when calculating Luck Rating, Luckiest and Unluckies mobs
	- BUT! You can now use `/showallenemydeaths` Chat Console command to see all the info including world kills and world trophy drops (this adds the data we had before back to the tooltips).
		- WARNING: This invalidates your run for Tournament play and colors your score bright green to indicate this. EX: Use this at the end of a run to inspect actual world drops.
	- Added a new button on the main menu "Show All Enemy Deaths" to enable/disable this prior to gameplay for the console-command shy.
	- Note that once it's been enabled your run is invalid even if you disable it again (score remains bright green.)
- Added "Logs: X" text to HUD to display how many Relogs have been done. Much requested. Note that `/trophyhunt` also still displays this information in the chat console and log file.
- Added `/ignorelogouts` Chat Console command to make it so that logouts no longer count against your score. (@gregscottbailey request)
	- WARNING: This invalidates the run for Tournament play. "Logs:" text will display dimmed and score bright green if you use this.
- Changed Trophy animation when you get your first trophy to read better on streams (Yes, again. I think it's less jarring AND more visible now.)
- Repositioned Deaths counter on the HUD to read better and take up less space
- Removed Luck-O-Meter "Luck" text and repositioned icon in HUD
- Reworked Luck-O-Meter tooltip to make it easier to read and less deluxe
- Added black outline to Score text to make it more readable against light backgrounds like the Mountains or staring at the sun.
- Fixed a bug where logging out, deleting your character, creating a new character with the same name and entering play would retain old player data for kills, trophy drops and /showpath pins (thanks @da_Keepa and @Xmal!)
- Fixed a bug with Luck Rating where my logic was reversed and it would display luck ratings if no luck was calculable yet
- Fixed a bug where animating trophy would drift upwards on screen if pausing while it was flashing

v0.3.3
- Added `/scorescale` chat console command to alter the score text size. 1.0 is the default, can go as low or high as you like. Use `/scorescale 1.5` to increase the text size by 50%. Thanks @turbero.
- Added `/trophyspacing` chat console command to pack them closer together or farther apart at the bottom of the screen. Negative values pack them tighter, positive ones space them out. 1.5 looks pretty good for me at 1920x1080 running in a window. YMMV. Thanks @Daizzer.
- Animate the discovered trophies upwards while they pulsate and flash to make them **even more** obvious and eye-catching for players.

v0.3.2
- Fixed UI text overrun on overall Luck tooltip for long enemy names
- Don't display luck rating in Trophy or Luck-O-Meter tooltips if not enough enemies have been killed to really tell
- added `/showtrophies` chat console command to toggle show/hide of trophy icons, Score, Deaths and Luck counters still display

v0.3.1
- Simplified the Luck-o-Meter and added luckiest and unluckiest trophies to the tooltip

v0.3.0
- Added "Luck-o-Meter" as suggested by @da_Keepa. 
	- Luck icon is on the left of the HUD above the Deaths counter
	- Hover text shows luck percentage as well as overall Luck Rating
	- Individual Trophy icons how show Luck Rating for that type of trophy
	- Luck is calculated as actual drop rates versus documented droprates. Overall luck is aggregate luck for all trophy capabable enemies that have died at least once.
- Trophy Rush Changes (Experimental Feature)
	- Fixed a bug that was preventing trophies from spawning in all circumstances
	- Added TrophyRush button to Main Menu below new logo position
- Made trophy icons animate a little longer when they're picked up
- Fixed a display-only bug where -10 would display as your starting score if you were playing another character, logged out, and made a new one. This just displayed wrong, and would correct itself on next score update and didn't ACTUALLY count against your score.
- Adjusted un-found Trophy icons in the tray to be more readable, is this better? Let me know
- Reduced size of TrophyHuntMod logo and moved it to the right side of the screen as suggested by @Kr4ken92 to play nicer with other mods

v0.2.4
- Swanky new Main Menu logo displaying "Trophy Hunt!" and the mod's version number
- Added tooltips to the Trophy icons at the bottom screen if you pause the game (ESC) and hover the mouse over them.
	- Only available in-game at the Pause (ESC) menu (not in-play with the Inventory screen open!)
	- This displays:
		- Trophy name
		- The number of enemies killed that could drop that Trophy
		- Number of trophies actually dropped by those enemies
		- Actual drop percentage
		- Wiki-documented drop percentage
- Experimental F5 console command `trophyrush` at the main menu, which enables Trophy Rush Mode.
	- Trophy Rush mode causes every enemy that WOULD drop a Trophy to drop a Trophy 100% of the time. This was suggested by @FizzyP as a potential new trophy hunt contest type so it's in there for experimentation.
	- This can only be enabled at the Main Menu via the F5 console command
	- Unfound Trophies will be colored RED in the hud to indicate Trophy Rush is enabled.
	- NOTE! This is the ONLY feature of TrophyHuntMod which modifies the behavior of Valheim. Please use with caution!

v0.2.3
- Removed "TrophyDraugrFem" from the trophy list since it's not supported in the game and does not drop.
- Decreased default HUD trophy size slightly
- TrophyHuntMod now detects whether it's the only mod running and reports this to the log file and displays the score in light blue instead of yellow.
	Yellow score means it's the only mod, which is required for the Trophy Hunt events.
	Light Blue score means other mods are present.
- Corrected the readme which listed the trophy HUD scaling command as `trophysize` instead of `/trophyscale` which is the correct command.

v0.2.2
- Increased the base size of trophies so they read better on screen for the stream audience.
- Added `/trophyscale` console command to allow the user to scale the size of the trophies at the bottom of the screen. Default is 1.0, and can be set as low as 0.1 and as high as you like. This will help adjust trophies to be more readable for streamers at some screen sizes.
	To increase the size of the trophies, hit <enter> to bring up the Chat Console and type `/trophyscale 1.5` for example. This would increase the trophy sizes by 50%
- Made the animation that plays when you collect a trophy more visible by flashing it on and off as well as animating the size. This makes it easier for runners to know when they picked one up without hunting for it on the trophy bar at the bottom.

## Trophy Hunt Mod Features

Displays a tray at the bottom of the game screen with the computed Trophy Hunt score on the left, and each Trophy running to the right. Trophies are grouped by Biome, and are displayed in silhouette when not yet acquired, and in full color when acquired.

A death counter appears to the left of the health and food bar, as deaths count against point totals in Trophy Hunt.

## Console Commands

`/trophyhunt`

	The Chat console (and F5 console) both support the console command `/trophyhunt` which prints out the Trophy Hunt scoring in detail like so:

	```
	[Trophy Hunt Scoring]
	Trophies:
	  TrophyBoar: Score: 10 Biome: Meadows
	  TrophyDeer: Score: 10 Biome: Meadows
	  TrophyNeck: Score: 10 Biome: Meadows
	  TrophyEikthyr: Score: 40 Biome: Meadows
	  TrophyGreydwarf: Score: 20 Biome: Forest
	Trophy Score Total: 90
	Penalties:
	  Deaths: 2 Score: -40
	  Logouts: 0 Score: 0
	Total Score: 50
	```
`/timer`

	Allows control of an in game timer display for four hour Trophy runs. Works in all game modes.

	- `/timer start` - start the timer if stopped
	- `/timer stop` - stop/pause the timer
	- `/timer reset` - reset the timer to zero seconds elapsed
	- `/timer show` - show the on-screen HUD timer
	- `/timer hide` - hide the on-screen HUD timer
	- `/timer set` - allows you to specify how much time has elapsed to manually set the timer. One hour fifteen minutes and five seconds would be entered as `/timer set 01:15:05`
	- `/timer toggle` - switches between countdown mode (default) and count up mode (red when counting down, yellow when counting up)

`/showpath`

	This will display pins on the in-game Map showing the path that the Player has traveled during the session. One pin every 100 meters or so.

`/trophyscale`

	This allows the user to scale the trophy sizes (1.0 is default) for better readability at some screen resolutions. 

`/trophyspacing`

	Allows you space out the trophies to your liking. Negative values spaces them tighter, positive values space them out more. They may wrap off the end of the screen with large values.

`/scorescale`

	Allows the user to scale the Score text size (1.0 is default) for better readability at some screen resolutions.

`/showtrophies`

	Toggles the display of Trophy icons at the bottom of the screen for when you can't even, or the display conflicts with other mods

`/showalltrophystats` 

	Chat Console command to see all the info including world kills and world trophy drops (this adds the data we had before back to the tooltips).
	- WARNING: This invalidates your run for Tournament play and colors your score bright green to indicate this.

`/ignorelogouts`
	
	Chat Console command to make it so that logouts no longer count against your score. 
	- WARNING: This invalidates your run for Tournament play and colors your score bright green and fades Logs: text to gray to indicate this.

## Support the Valheim Speedrunning Community!
If you'd like to donate a dollar or two to the speedrunners and the Trophy Hunt Events, please consider donating via CashApp or PayPal. All the money goes directly into the prize pool for future Trophy Hunt events! 

You can learn more on the Valheim Speedrun Discord channel here: https://discord.gg/9bCBQCPH

	CashApp: $ARCHYCooper 
	PayPal: https://www.paypal.com/paypalme/expertarchy

## Known issues

## Feature Requests

- Report score and trophies to the valheim.help tracker during runs
- Dropshadow or add dark background field to Score (Weih (Henrik))
- Collect player kills/drops as default, enable all kills/drops as options


## Where to Find
You can find the github at: https://github.com/smariotti/TrophyHuntMod

Note, this was originally built with Jotunn, using their example mod project structure, though Jotunn is no longer a requirement to run it. You just need to have BepInEx installed.
