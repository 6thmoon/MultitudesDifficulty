## Introduction

Looking for a challenge beyond what the standard game modes have to offer? Ever wonder if you could finish that multiplayer run without the help of your friends? Or perhaps you're an *Eclipse* veteran seeking an additional modifier to keep things interesting. Whatever the case, you've come to the right place.

At its core, this plugin simply increases the game's internal player count. By leveraging existing functionality to scale up difficulty for multiplayer, we can introduce a harder option while retaining a mostly "vanilla" feel. Enemies will level up faster, and initial spawn rates are also higher - which helps keep the game exciting early on.

![](https://github.com/6thmoon/MultitudesDifficulty/blob/v0.4.1/Resources/screenshot.png?raw=true)

Inspired by the original [*Multitudes*]() by **wildbook**, this is a complete re-implementation from the ground up. In addition to improved ease of use, this version takes the challenge one step further by introducing additional parameters to reduce advantages that come with an increased player count. After all, in a real multiplayer game, one would generally expect loot to be split among all players.

## Options

It is recommended that you install with [*r2modman*](https://thunderstore.io/package/ebkr/r2modman/), and modify the following entries using the configuration editor present within. Please be aware that the file `BepInEx/config/local.difficulty.multitudes.cfg` is generated upon initial launch, and will not be accessible prior to this point.

| Title | Input | Description |
| - | - | - |
| Additional Player Count | *1 to 255* | Add this many players to the game, increasing the difficulty of enemies. Also affects the other options listed below. |
| Eclipse Mode | *True/False* | Use eclipse modifiers. Please note, this requires a restart in order to take effect. |
| Additional Interactables | *Percent* | Increase this percentage for more loot (i.e. chests, shrines, etc.) on each stage, proportional to player count. |
| Extra Item Rewards | *True/False*  | Enable to drop additional items from the teleporter event, other bosses, and hidden realms. |
| Income Penalty | *Percent* | Gold is typically split between all players. Lower this value to lessen this effect, increasing player income. |
| Bonus Health | *Percent* | Certain enemies receive bonus health in multiplayer. Reduce the amount granted to teleporter bosses and unique encounters. |
| Teleporter Duration | *Percent* | The extent at which player count is considered when determining charge rate for holdout zones. Not recommended. |
| Ignore Disconnected Players | *True/False* | By default, players that leave a multiplayer lobby are still taken into account, until they reconnect. |
| Force Enable | *True/False* | For use with other difficulty options. Apply the increase to player count regardless of selection. |

Note that values of **0**% result in identical behavior to a singleplayer game with respect to each parameter. By contrast, selecting **100**% will provide the full effect of *Additional Player Count*.

## Known Issues

- In multiplayer, clients may notice inaccurate information presented in the user interface regarding difficulty level. This is purely visual and should not affect gameplay nor the host.

Please report any feedback or issues discovered [here](https://github.com/6thmoon/MultitudesDifficulty/issues). Feel free to check out my [other](https://thunderstore.io/package/6thmoon/?ordering=top-rated) released content too.

## Version History

#### `0.4.1`
- Fix issue present in base game that could result in incorrect player count when transitioning from *Multiplayer* to *Singleplayer* lobby.

#### `0.4.0`
- Add option to limit penalties to player income and boss health.

#### `0.3.5`
- Update dependencies.

#### `0.3.4`
- Fix compatibility issue with plugins that utilize *R2API* submodule *DifficultyAPI*.

#### `0.3.3`
- *Teleporter Duration* parameter no longer applies to zones that would otherwise be unaffected by player count. Increase escape sequence duration proportionally to ensure sufficient time is always available.

#### `0.3.2` ***- Initial Release***
- Support for both singleplayer and multiplayer lobbies. Only the host needs to have this mod installed.
