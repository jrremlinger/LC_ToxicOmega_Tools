# ToxicOmega Tools

*Commands triggered using the in-game chat. The main purpose of this mod is for exploring the technical aspects of the game and testing other mods.*

Some important notes:
* All commands only register if you are the host.
* All players must have this mod installed, along with LC_API, for it to work properly.
* Text chat will stay enabled while dead if you are the host.
* Only the beginning of player names are required when using them in commands (example: searching John will return a player with the name Johnny).
* Where applicable, certain symbols can be used when determining a destination:
	* $: Using "$" indicates random/natural destination. For players this will act as an inverse-teleporter putting them inside the factory. For items it will choose a normal item spawnpoint. For enemies it will either use vents or outside spawnpoints depending on the type of enemy.
	* !: Using "!" chooses the ships terminal as a target. This is only applicable for teleportation.
	* @(int): Using "@" followed by a number will choose the waypoint with that index as the target.
	* #(int): Using "#" followed by a number will choose the player with that Client ID as the target.

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
  <summary><h3>Gi/Give (Item ID) Optional: (Amount) (Value) (Target)</h3></summary>

Spawns an item based on given ID number. Able to specify how many items, their value, and what player it spawns on. "$" as the player will spawn the item in a random location inside the factory.

Arguments:
* Item ID: Numerical ID of item you want to spawn. In the future names may be supported.
* Amount (Default: 1): How many copies of the item should be spawned.
* Value (Default: Random): Override the default value of the item with a given number.
* Target (Default: Host Player): Where to spawn the item, supports natural spawning with "$" and waypoints with @(int).

Example: "give 17 1 420 #3" will spawn one Airhorn worth $420 on the player whose ID is 3.
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
  <summary><h3>Sp/Spawn (Enemy ID) Optional: (Amount) (Target)</h3></summary>

Spawns an enemy based on given ID number. Able to specify how many enemies, and where they spawn.

Arguments:
* Enemy ID: Numerical ID of enemy you want to spawn. In the future names may be supported.
* Amount (Default: 1): How many copies of the enemy should be spawned.
* Target (Default: Natural): Player target, also supports natural spawning with "$" and waypoints with @(int).

Example: "sp 0 1" will spawn one enemy of ID zero naturally. Different moons assign different enemy IDs, so make sure you check the "enemy" command to find the ID of the enemy you want to spawn.
</details>

---

<details>
  <summary><h3>TP/Tele/Teleport Optional: (Target A) (Target B)</h3></summary>

Teleports a given player to a given destination. Player being teleported cannot be dead. Will automatically sync lighting if your destination is inside or outside. If no arguments are provided, the host will be teleported to the ship's console.

Arguments:
* Target A: If this is the only argument given it supports "!", "@(int)", and "$". Otherwise, it must be a player.
* Target B: Can be a player, "!", "@(int)", or "$".

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
  <summary><h3>Ch/Charge Optional: (Player Target)</h3></summary>

Charges a player's held item.

Arguments:
* Player Target (Default: Host Player): Only supports player names/id.

Example: "ch" will simply charge the host's held item.
</details>

---

<details>
  <summary><h3>He/Heal/Save Optional: (Player Target)</h3></summary>

Fully refills a player's health and stamina. Will save a player if they are currently in a kill animation with Snare Fleas, Forest Giants, or Masked Players. If the target player is dead, they will be revived at the ship's terminal.

Arguments:
* Player Target (Default: Host Player): Only supports player names/id.

Example: "heal John" will heal a player whose name starts with (or is) John.
</details>

---

<details>
  <summary><h3>Li/List (List Name) (Page Number)</h3></summary>

Displays a page from the list of currently spawned players, items, or enemies. Will smart-search for the list name.

Arguments:
* List Name (Default: Players): Which list to view, supports "players", "items", and "enemy/enemies".

Example: "li e" will list every enemy currently spawned in the current moon.
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