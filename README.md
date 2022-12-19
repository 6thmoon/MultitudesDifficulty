## Introduction

Looking for a challenge beyond what the standard game modes have to offer? Ever wonder if you could finish that multiplayer run without the help of your friends? Or perhaps you're an *Eclipse* veteran seeking an additional modifier to keep things interesting. Whatever the case, you've come to the right place.

At its core, this plugin simply increases the game's internal player count. By leveraging existing functionality to scale up difficulty for multiplayer, we can introduce a harder option while retaining a mostly "vanilla" feel. Enemies will level up faster, and initial spawn rates are also higher - which helps keep the game exciting early on.

![](https://github.com/6thmoon/MultitudesDifficulty/blob/main/screenshot.png?raw=true)

Inspired by the original [*Multitudes*](https://thunderstore.io/package/wildbook/Multitudes/) by **wildbook**, this is a complete re-implementation from the ground up. In addition to improved ease of use via difficulty selection, this mod takes the challenge one step further by introducing additional parameters to reduce advantages that come with an increased player count. After all, in a real multiplayer game, one would generally expect loot to be split among all players.

## Options

It is recommended that you install with [*r2modman*](https://thunderstore.io/package/ebkr/r2modman/), and modify the following entries using the configuration editor present within. Please be aware that the file `BepInEx/config/local.difficulty.multitudes.cfg` is generated upon initial launch, and will not be accessible prior to this point.

| Title | Input | Description |
| - | - | - |
| Additional Player Count | *1 to 255* | Higher values increase difficulty. Although more enemies will spawn, less money is awarded and purchase costs are increased. |
| Eclipse Mode | *True/False* | Use eclipse modifiers. Not for the faint of heart. |
| Additional Interactables | *Percent* | Increasing this percentage results in additional interactables (i.e. chests, shrines, & other loot), relative to player count. |
| Extra Item Rewards | *True/False*  | Enable to drop additional items during the teleporter event, Void Fields, and the Simulacrum. |
| Teleporter Duration | *Percent* | The extent at which player count is considered when determining charge rate for holdout zones. Higher values result in slower charge. |
| Force Enable | *True/False* | Force player count adjustment regardless of difficulty selection. For use with other custom difficulty modes. |

Note that values of **0**% (for *Additional Interactables* & *Teleporter Duration*) and **false** (*Extra Item Rewards*) will result in identical behavior to an unmodded game with respect to each parameter. By contrast, selecting **100**% (or **true**) will provide the full effect of *Additional Player Count*.

## Known Issues

- In multiplayer, clients may notice inaccurate information presented in the user interface regarding difficulty level. This is purely visual and should not affect gameplay nor the host.

Please report any feedback or issues discovered [here](https://github.com/6thmoon/MultitudesDifficulty/issues). Feel free to check out my [other](https://thunderstore.io/package/6thmoon/CurseCatcher/) released content too.

## Version History

#### `0.3.5`
- Update dependencies.

#### `0.3.4`
- Fix compatibility issue with plugins that utilize *R2API* submodule *DifficultyAPI*.

#### `0.3.3`
- *Teleporter Duration* parameter no longer applies to zones that would otherwise be unaffected by player count. Increase escape sequence duration proportionally to ensure sufficient time is always available.

#### `0.3.2` ***- Initial Release***
- Support for both singleplayer and multiplayer lobbies. Only the host needs to have this mod installed.
