# ToxicOmega Tools

*Commands triggered using the in-game chat. The main purpose of this mod is for exploring the technical aspects of the game and testing other mods.*

Some important notes:
* All commands only register if you are the host.
* It is not required for all players to have the mod installed, but it may lead to desyncs if a player doesn't have it.
* Text chat will stay enabled while dead if you are the host.
* Only the beginning of commands as well as player/item/enemy names are required, the closest matching item will be selected.
* There are several ways to target different entities/areas when running commands:
  * The network ID (shown in list command and hud) can be used to target already existing items/enemies.
  * $: Using "$" indicates random/natural destination. For teleporting this will act as an inverse-teleporter putting the teleport target randomly inside the factory. For spawning items it will choose a normal scrap spawnpoint. For enemies it will either use vents or outside spawnpoints depending on the type of enemy.
  * !: Using "!" chooses the ships terminal as a target. This is only applicable for teleportation.
  * @(num): Using "@" followed by a number will choose the waypoint with that index as the target.
  * #(num): Using "#" followed by a number will choose the player with that Client ID as the target.
  * +(num) & -(num): Using "+" or "-" followed by a number will choose the location in front of or behind the local player (or the player being spectated if you are dead). Using "+" without a number after will shoot a RayCast to use the location of wherever you are looking at no matter how far it is..
  * Typing the beginning of a players name will target that player. 

---

## Commands

<details>
  <summary><h3>Help (Page Number)</h3></summary>

Displays a page of the commands list. Includes brief descriptions of each command and its purpose.

Arguments:
* Page Number: Specific page of the commands list to view.

Example: "help" displays page one of the commands list.
</details>

---

<details>
  <summary><h3>Items (Page Number)</h3></summary>

Displays a page of the spawnable items list.

Arguments:
* Page Number: Specific page of the items list to view. Will default to the last page viewed.

Example: "item 2" displays page two of the items list.
</details>

---

<details>
  <summary><h3>Enemies (Page Number)</h3></summary>

Displays a page of the spawnable enemies list.

Arguments:
* Page Number: Specific page of the enemies list to view. Will default to the last page viewed.

Example: "en 3" displays page three of the enemies list.
</details>

---

<details>
  <summary><h3>Traps (Page Number)</h3></summary>

Displays a page of the spawnable traps list.

Arguments:
* Page Number: Specific page of the traps list to view. Will default to the last page viewed.

Example: "tr 1" displays page one of the traps list.
</details>

---

<details>
  <summary><h3>Spawn (Prefab Name) Optional: (Target) (Amount) (Scrap Value*)</h3></summary>

Spawns an item/enemy/trap by name. Able to specify where it spawns, how many are spawned, and the value of the item if you are spawning a scrap item.

Arguments:
* Prefab Name: Name of the item/enemy/trap you want to spawn. Will auto-complete name if the beginning is given. Use underscores "_" in place of spaces.
* Target (Default: Natural): Where to spawn the prefab, supports all targeting methods listed at the beginning of this readme.
* Amount (Default: 1): How many copies should be spawned.
* Scrap Value (Default: Natural): How much should the item be worth. Does not do anything if spawning enemies/traps.

Example: "sp flower ToxicOmega 5" will spawn five flowermen (Brackens) at the player named ToxicOmega.
</details>

---

<details>
  <summary><h3>Give (Item Name) Optional: (Player Target)</h3></summary>

Puts an item directly into a players inventory. If their inventory is full it will drop on the ground beneath them.

Arguments:
* Item Name: Name of the item you want to give.
* Player Target (Default: Self): Only supports player name/ID.

Example: "give jar" will give a jar of pickles to yourself.
</details>

---

<details>
  <summary><h3>List Optional: (List Name) (Page Number)</h3></summary>

Toggles a GUI showing all currently existing players, items, enemies, and terminal objects. If used with arguments it will display a page from the list of currently existing players, items, enemies, terminal objects, or waypoints. Network ID's will be listed in the items and enemies lists. Will smart-search for the list name.

Arguments:
* List Name: Which list to view. Supports "players", "items", "enemy/enemies", "codes", and "wp/waypoints".

Example: "li e" will list page one every enemy that is currently spawned and active in the scene. In most situations simply using "list" on its own to show the full GUI is more useful.
</details>

---

<details>
  <summary><h3>GUI/HUD</h3></summary>

Toggles a HUD that displays useful information. Current coordinates, time of day, and godmode status will be displayed at the bottom. Listed along the top will be nearby items, enemies, traps, blast doors, and players. Using the "List" command with zero arguments after will enable the GUI but show items/enemies/etc from ANY distance instead nearby. Network ID is listed next to items and enemies. Terminal Code is listed next to traps and blast doors.

Example: "hud" will enable the hud if it is currently hidden.
</details>

---

<details>
  <summary><h3>TP/Teleport Optional: (Target A) (Target B)</h3></summary>

Teleports a given player to a given destination. Targeting a dead player will target their corpse. Will automatically sync lighting if your destination is inside or outside. If no arguments are provided, the host will be teleported to the ship's console.

Arguments:
* Target A: If this is the only argument given it supports all targeting methods. Otherwise, it will only accept player names/IDs or item/enemy network IDs.
* Target B: Can any targeting method listed at the beginning of this readme.

Example: "tp #0 $" will teleport the player with ID 0 to a random location inside the factory.
</details>

---

<details>
  <summary><h3>WP/Waypoints Optional: Add/Clear/Door/Entrance</h3></summary>

Lists or creates a waypoint to use as a destination. Waypoints are cleared when leaving a moon. If no argument is given it will show page one of the "list wp" command list.

Arguments:
* Add: Will create a waypoint at your current position.
* Clear: Will delete all waypoints.
* Door: Will create a waypoint outside the factory at the front door.
* Entrance: Will create a waypoint inside the factory at the main entrance.

Example: "wp add" will create a waypoint at your current location.
</details>

---

<details>
  <summary><h3>Heal/Save Optional: (Player Target)</h3></summary>

Fully refills a player's health and stamina. Will save a player if they are currently in a kill animation with an enemy. If the target player is dead, they will be revived at the ship's terminal.

Arguments:
* Player Target (Default: Self): Only supports player name/ID.

Example: "heal John" will heal a player whose name starts with (or is) John.
</details>

---

<details>
  <summary><h3>Kill (Target)</h3></summary>

Kills/Destroys a given player or item/enemy (if given their Network ID as the target). Supports a range of item/enemy network IDs by separating them with a "-". Items and invincible enemies are destroyed since they cannot take damage. Players and normal enemies are killed unless they are forced to by destroyed by adding "*" to the end of the target argument.

Arguments:
* Target: Any player name/ID, or network ID of an enemy/item. Able to target network ID range by separating with a "-". Ending command with a "*" will force destroy instead of killing with damage.

Example: "kill 14-82*" will force delete all items/enemies with network IDs ranging from 14-82.
</details>

---

<details>
  <summary><h3>GM/GodMode</h3></summary>

Toggle whether or not you take damage.

Example: "gm" while godmode is off will toggle it on.
</details>

---

<details>
  <summary><h3>Codes (Code)</h3></summary>

Toggles blast doors and traps by using their terminal code. If no argument is given it will show page one of the "list codes" command list.

Arguments:
* Code: The code that appears on the ship's map corresponding to the blast door or trap.

Example: "code d2" will toggle all terminal objects on the map with code d2.
</details>

---

<details>
  <summary><h3>Breaker</h3></summary>

Toggles the breaker box's state.

Example: "br" while the breaker is on will toggle it to be off.

</details>

---

<details>
  <summary><h3>Credits/Money Optional (Amount)</h3></summary>

Lists or adjusts the current amount of group credits in the terminal. If the amount argument is not given it will just display the current amount of credits.

Arguments:
* Amount: The amount is the adjustment to be made to the current amount of credits.

Example: "credit -10" will subtract 10 from the current amount of group credits.
</details>

---

<details>
  <summary><h3>Suit Optional: (Suit Name) (Player Target)</h3></summary>

Changes a players suit. If no arguments are given it will instead list all available suits.

Arguments:
* Suit Name: The name of the suit you want to equip.
* Player Target (Default: Self): The player who's suit will be changed.

Example: "suit purple Joe" will change the suit of player named Joe to be the purple suit.
</details>

---

<details>
  <summary><h3>Charge Optional: (Player Target)</h3></summary>

Charges a player's held item.

Arguments:
* Player Target (Default: Self): Only supports player name/ID.

Example: "ch" will simply charge the host's held item.
</details>

---