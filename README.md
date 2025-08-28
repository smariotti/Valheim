----
# oathorse Valheim Projects

https://thunderstore.io/c/valheim/p/oathorse/
----

## Configuring for Building and Running Locally

Here's the recommended setup:

1. Dupe your Steam `Valheim` directory to `Valheim_Dev`
1. Install `BepInExPack_Valheim` in your Valheim_Dev Dir
1. Fix Project paths to point to your Valheim directory and executable

#### Dupe your Steam Valheim Directory
Make a copy of the Valheim folder and rename it to Valheim_Dev

#### Install BepInExPack_Valheim
Open up the included `denikson-BepInExPack_Valheim-*.zip` archive and unzip the contents of the `BepInExPack_Valheim` directory into the root of your `Valheim_Dev` directory.

#### Fix Project Paths
* Edit `Directory.Build.props` to provide the full path to your `Valheim_Dev` directory
* Inside Visual Studio, for each Project (ex: TrophyHuntMod), right-click the project and select `Properties`. Under the `Debug` tab, set the `Start external program` to point to your `valheim.exe` in your `Valheim_Dev` directory.

----
## Mods Included
### TrophyHuntMod
https://thunderstore.io/c/valheim/p/oathorse/TrophyHuntMod/

### Dude, Where's My Portal?
https://thunderstore.io/c/valheim/p/oathorse/DudeWheresMyPortal/

### TubaWalk
https://thunderstore.io/c/valheim/p/oathorse/TubaWalk/

### InstaSmelt
https://thunderstore.io/c/valheim/p/oathorse/InstaSmelt/

### ScytheEverything
https://thunderstore.io/c/valheim/p/oathorse/ScytheEverything/

### ITawtITaw
https://thunderstore.io/c/valheim/p/oathorse/ITawtITaw/

----
Version 0.1 of this document