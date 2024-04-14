# ToxicOmega Tools

*Commands triggered using the in-game chat. The main purpose of this mod is for exploring the technical aspects of the game and testing other mods.*

Some important notes:
* All commands only register if you are the host.
* It is not required for all players to have the mod installed, but it may lead to desyncs if a player doesn't have it.
* Text chat will stay enabled while dead if you are the host.
* Only the beginning of player/item/enemy names are required when using them in commands.
* There are several ways to target different entities/areas when running commands:
  * The network ID (shown in list command and hud) can be used to target already existing items/enemies.
  * $: Using "$" indicates random/natural destination. For teleporting this will act as an inverse-teleporter putting the teleport target randomly inside the factory. For spawning items it will choose a normal scrap spawnpoint. For enemies it will either use vents or outside spawnpoints depending on the type of enemy.
  * !: Using "!" chooses the ships terminal as a target. This is only applicable for teleportation.
  * @(num): Using "@" followed by a number will choose the waypoint with that index as the target.
  * #(num): Using "#" followed by a number will choose the player with that Client ID as the target.
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
  <summary><h3>It/Item/Items (Page Number)</h3></summary>

Displays a page of the items list. Includes item names and ID numbers.

Arguments:
* Page Number: Specific page of the item list to view. Will default to the last page viewed.

Example: "item 2" displays page two of the items list.
</details>

---

<details>
  <summary><h3>Gi/Give (Item Type) Optional: (Amount) (Value) (Target)</h3></summary>

Spawns an item based on given name or ID number. Able to specify how many items, their value, and where they spawn.

Arguments:
* Item Type: Name or numerical ID of item you want to spawn. Will auto-complete name if the beginning is given. Use underscores "_" in place of spaces.
* Amount (Default: 1): How many copies of the item should be spawned.
* Value (Default: Random): Override the default value of the item with a given number.
* Target (Default: Self): Where to spawn the item, supports all targeting methods listed at the beginning of this readme.

Example: "give gold_bar 1 420 #3" will spawn one gold bar worth $420 on the player whose ID is 3.
</details>

---

<details>
  <summary><h3>En/Enemy/Enemies (Page Number)</h3></summary>

Displays a page of the enemies list. Includes enemy names and ID numbers.

Arguments:
* Page Number: Specific page of the enemies list to view. Will default to the last page viewed.

Example: "en 2" displays page two of the enemies list.
</details>

---

<details>
  <summary><h3>Sp/Spawn (Enemy Type) Optional: (Amount) (Target)</h3></summary>

Spawns an enemy based on given name or ID number. Able to specify how many enemies, and where they spawn.

Arguments:
* Enemy Type: Name or numerical ID of enemy you want to spawn. Will auto-complete name if the beginning is given. Use underscores "_" in place of spaces.
* Amount (Default: 1): How many copies of the enemy should be spawned.
* Target (Default: Natural): Where to spawn the enemy, supports all targeting methods listed at the beginning of this readme.

Example: "sp 0 1" will spawn one enemy of ID zero naturally. Make sure you check the "enemy" command to find the ID of the enemy you want to spawn or just use their name instead.
</details>

---

<details>
  <summary><h3>Tr/Trap (Trap Type) Optional: (Amount) (Target)</h3></summary>

Spawns trap based on given name or ID number. If no arguments are given it will instead list available traps with their ID #'s.

Arguments:
* Type: Name or ID of the trap you want to spawn. Will auto-complete name if the beginning is given.
* Amount (Default: 1): How many copies of the trap should be spawned.
* Target (Default: Natural): Where to spawn the trap, supports all targeting methods listed at the beginning of this readme.

Example: "tr mi 30" will spawn 30 landmines randomly throughout the factory.
</details>

---

<details>
  <summary><h3>Li/List (List Name) (Page Number)</h3></summary>

Displays a page from the list of currently spawned players, items, or enemies. Network ID's will be listed in the items and enemies lists. Will smart-search for the list name.

Arguments:
* List Name (Default: Players): Which list to view, supports "players", "items", and "enemy/enemies".

Example: "li e" will list every enemy currently spawned in the current moon.
</details>

---

<details>
  <summary><h3>GUI/HUD</h3></summary>

Toggles a HUD that displays the players coordinates as well as nearby items and enemies. The Network ID of the items/enemies will be listed as well.

Example: "hud" will enable the hud if it is currently hidden.
</details>

---

<details>
  <summary><h3>TP/Tele/Teleport Optional: (Target A) (Target B)</h3></summary>

Teleports a given player to a given destination. Player being teleported cannot be dead. Will automatically sync lighting if your destination is inside or outside. If no arguments are provided, the host will be teleported to the ship's console.

Arguments:
* Target A: If this is the only argument given it supports all targeting methods. Otherwise, it will not accept "!", "$", or "@" since they are not able to be moved.
* Target B: Can any targeting method listed at the beginning of this readme.

Example: "tp #0 $" will teleport the player with ID to a random location inside the factory.
</details>

---

<details>
  <summary><h3>WP/Waypoint/Waypoints Optional: Add/Clear/Door</h3></summary>

Lists or creates a waypoint to use as a destination. Waypoints are cleared when leaving a moon.

Arguments (The text added after is the only argument accepted. If not provided it will list all waypoints):
* Add: Will create a waypoint at your current position.
* Clear: Will delete all waypoints.
* Door: Will create a waypoint inside the factory at the main entrance.

Example: "wp add" will create a waypoint at your current location.
</details>

---

<details>
  <summary><h3>He/Heal/Save Optional: (Player Target)</h3></summary>

Fully refills a player's health and stamina. Will save a player if they are currently in a kill animation with Snare Fleas, Forest Giants, or Masked Players. If the target player is dead, they will be revived at the ship's terminal.

Arguments:
* Player Target (Default: Self): Only supports player name/client ID (enemy ID not supported yet).

Example: "heal John" will heal a player whose name starts with (or is) John.
</details>

---

<details>
  <summary><h3>Ki/Kill (Target)</h3></summary>

Kills/Destroys a given player or item/enemy (if given their Network ID as the target). Items and invincible enemies are destroyed. Players and normal enemies are killed unless they are forced to by destroyed by adding * to the end of the target argument.

Arguments:
* Target: Any player name/client ID, or the network ID of an enemy/item. A "*" can be added anywhere at the end of the target to force the target to be deleted instead of killed.

Example: "kill 69*" will delete whatever enemy or item currently has the network ID 69 rather than killing them with damage.
</details>

---

<details>
  <summary><h3>GM/God/GodMode</h3></summary>

Toggle whether or not you take damage.

Example: "gm" while godmode is off will toggle it on.
</details>

---

<details>
  <summary><h3>Co/Code/Codes (Code)</h3></summary>

Toggles doors/turrets/mines by using their terminal code. If no argument is given it will list all terminal objects on the map.

Arguments:
* Code: The code that appears on the ship's map corresponding to the object.

Example: "code d2" will toggle all objects on the map with code d2.
</details>

---

<details>
  <summary><h3>Br/Breaker</h3></summary>

Toggles the breaker box's state.

Example: "br" while the breaker is on will toggle it to be off.

</details>

---

<details>
  <summary><h3>Cr/Credit/Credits/Money Optional (Amount)</h3></summary>

Lists or adjusts the current amount of group credits in the terminal. If the amount argument is not given it will just display the current amount of credits.

Arguments:
* Amount: The amount is the adjustment to be made to the current amount of credits.

Example: "credit -10" will subtract 10 from the current amount of group credits.
</details>

---

<details>
  <summary><h3>Su/Suit Optional: (Suit Name) (Player Target)</h3></summary>

Changes a players suit. If no arguments are given it will instead list all available suits.

Arguments:
* Suit Name: The name of the suit you want to equip.
* Player Target (Default: Self): The player who's suit will be changed.

Example: "suit purple Joe" will change the suit of player named Joe to be the purple suit.
</details>

---

<details>
  <summary><h3>Ch/Charge Optional: (Player Target)</h3></summary>

Charges a player's held item.

Arguments:
* Player Target (Default: Self): Only supports player name/client ID.

Example: "ch" will simply charge the host's held item.
</details>

---