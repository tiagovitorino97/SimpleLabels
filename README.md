# SimpleLabels

MelonLoader mod for **Schedule I** that adds custom text labels to storage units and crafting stations. Labels appear in-world and on the clipboard for clear, immersive organization. Built with C# and game-native UI.

## Features

- **Custom labels** – Add and edit text on any storage or station.
- **Clipboard integration** – Labels show on the clipboard view for an at-a-glance overview of your production line.
- **Per-save storage** – Labels are stored per save in `{SaveFolder}/SimpleLabels/Labels.json`; no cross-save mixing.
- **Color customization** – Optional label colors with auto-color based on item contents.
- **Multiplayer** – Bidirectional label syncing between host and clients (requires [SteamNetworkLib](https://www.nexusmods.com/schedule1/mods/1396) in UserLibs).
- **Mod Manager** – Settings manageable via [Mod Manager & Phone App](https://www.nexusmods.com/schedule1/mods/397) or MelonPreferences.

## Requirements

- [MelonLoader](https://melonwiki.xyz/) (Schedule I / Il2Cpp)

**Optional**

- **SteamNetworkLib** ([Nexus](https://www.nexusmods.com/schedule1/mods/1396)) – Place `SteamNetworkLib-IL2Cpp.dll` in the game’s **UserLibs** folder (not Mods) for multiplayer label sync. Without it, the mod runs in single-player mode.
- **Mod Manager & Phone App** – For in-game settings UI.

## Installation

1. Install [MelonLoader](https://melonwiki.xyz/) for Schedule I.
2. Download the mod and place **SimpleLabels.dll** in the game’s **Mods** folder.
3. Launch the game.

Multiplayer: install SteamNetworkLib and put `SteamNetworkLib-IL2Cpp.dll` in **UserLibs** (see link above).


## Links

- [GitHub](https://github.com/tiagovitorino97/SimpleLabels)
- [Nexus Mods](https://www.nexusmods.com/schedule1/mods/680)
- [Releases / changelog](https://github.com/tiagovitorino97/SimpleLabels/releases)

## Feedback

Bugs or suggestions: open an issue on GitHub or use the [Nexus Mods page](https://www.nexusmods.com/schedule1/mods/680).
