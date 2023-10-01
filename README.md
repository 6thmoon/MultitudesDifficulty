## Introduction

Looking for a challenge beyond what the standard game modes have to offer? Ever wonder if you could finish that multiplayer run without the help of your friends? Or perhaps you're an Eclipse veteran seeking another modifier.

At the core, this plugin simply increases player count. By leveraging existing functionality to scale up the game for multiplayer, we can unlock a new difficulty option. Enemies will level up faster, and higher spawn rates keep things interesting from the start.

![](https://github.com/6thmoon/MultitudesDifficulty/blob/v0.5.0/Resources/screenshot.png?raw=true)

Inspired by the original [*Multitudes*](https://thunderstore.io/package/wildbook/Multitudes/) by **wildbook**, this is a complete re-implementation from the ground up. In addition to improved customization and ease of use, this version allows the user to reduce advantages that come with an increased player count. After all, in a real multiplayer game, one would generally expect loot to be split among all players.

## Options

It is recommended that you install with [*r2modman*](https://thunderstore.io/package/ebkr/r2modman/), and modify the following entries using the configuration editor present within. Please be aware that the file `local.difficulty.multitudes.cfg` is generated upon initial launch, and will not be accessible prior to this point. Unless otherwise specified, a restart is not required - these settings will be reloaded upon entering the lobby or continuing to the next stage.

| Title | Input | Description |
| - | - | - |
| Additional Player Count | *¼ to 250* | Add this many players to the game, increasing the difficulty of enemies. Also affects the other options listed below. |
| Eclipse Mode | *True/False* | Use eclipse modifiers. Please note, this requires a restart in order to take effect. |
| Additional Interactables | *Percent* | Increase this percentage for more loot (i.e. chests, shrines, etc.) on each stage, proportional to player count. |
| Extra Item Rewards | *True/False*  | Enable to drop additional items from the teleporter event, other bosses, and hidden realms. |
| Income Penalty | *Percent* | Gold is typically split between all players. Lower this value to lessen this effect, increasing player income. |
| Bonus Health | *Percent* | Certain enemies receive bonus health in multiplayer. Reduce the amount granted to teleporter bosses and unique encounters. |
| Teleporter Duration | *Percent* | The extent at which player count is considered when determining charge rate for holdout zones. Not recommended. |
| Ignore Disconnected Players | *True/False* | By default, players that leave a multiplayer lobby are still taken into account, until they reconnect. |
| Force Enable | *True/False* | For use with other difficulty options. Apply the increase to player count regardless of selection. |

Note that values of **0**% result in identical behavior to a singleplayer game with respect to each parameter. By contrast, selecting **100**% provides the full effect of *Additional Player Count*. If an intermediate value is chosen, the outcome will scale linearly in between.

## Known Issues

- In multiplayer, clients may notice inaccurate information presented in the user interface regarding difficulty level. This is purely visual and should not affect gameplay. Only the host needs to have this installed.

Please report any feedback or issues discovered [here](https://github.com/6thmoon/MultitudesDifficulty/issues). Feel free to check out my [other](https://thunderstore.io/package/6thmoon/?ordering=top-rated) work as well.

## Version History

#### `0.5.1`
- Prevent a couple of minor display errors in certain circumstances.

#### `0.5.0`
- Now supports fractional values for player count increase.
- Implement option for disconnected players.

#### `0.4.2`
- Reload configuration file on scene transition.
- Add player count indicator.

#### `0.4.1`
- Fix issue present in the original game that results in incorrect player count after leaving a multiplayer lobby.

#### `0.4.0`
- Add option to limit penalties to player income and boss health.

#### `0.3.4`, `0.3.5`
- Update for **R2API** compatibility.

#### `0.3.3`
- Address a couple of inconsistencies with holdout zones on the final stage.

#### `0.3.2` **- Initial Release**
