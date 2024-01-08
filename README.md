# ToxicOmega Tools

*Tools triggered using the in-game chat. The main purpose of this mod is for debugging and testing other mods.*

Some important notes:
* All commands only register if you are the host.
* Text chat will stay enabled while dead if you are the host.
* Where applicable, certain symbols can be used when determining a destination:
	* $: Using "$" indicates random/natural destination. For players this will act as an inverse-teleporter putting them inside the factory. For items it will choose a normal item spawnpoint. For enemies it will either use vents or outside spawnpoints depending on the type of enemy.
	* !: Using "!" chooses the ships terminal as a target. This is only applicable for teleportation.
	* @(int): Using "@" followed by a number will choose the waypoint with that index as the target.
	* #(int): Using "#" followed by a number will choose the player with that Client ID as the target.

---

## Commands

### **Item/Items (Page Number)**

Displays a page of the items list. Includes item names and ID numbers.

Arguments:
* Page Number: Specific page of the item list to view. Will default to the last page viewed.

Example: "item 2" displays page two of the items list

---
### **Give (Item ID) Optional: (Amount) (Value) (Target)**

Spawns an item based on given ID number. Able to specify how many items, their value, and what player it spawns on. "$" as the player will spawn the item in a random location inside the factory.

Arguments:
* Item ID: Numerical ID of item you want to spawn. In the future names may be supported.
* Amount (Default: 1): How many copies of the item should be spawned.
* Value (Default: Random): Override the default value of the item with a given number.
* Target (Default: Host Player): Where to spawn the item, supports natural spawning with "$" and waypoints with @(int).

Example: "give 17 1 420 #3" will spawn one Airhorn worth $420 on the player who's ID is 3.

---
### **En/Enemy/Enemies (Page Number)**

Displays a page of the enemies list. Includes enemy names and ID numbers.

Arguments:
* Page Number: Specific page of the enemies list to view. Will default to the last page viewed.

Example: "en 2" displays page two of the enemies list

---
### **Sp/Spawn (Enemy ID) Optional: (Amount) (Target)**

Spawns an enemy based on given ID number. Able to specify how many enemies, and where they spawn.

Arguments:
* Enemy ID: Numerical ID of enemy you want to spawn. In the future names may be supported.
* Amount (Default: 1): How many copies of the enemy should be spawned.
* Target (Default: Natural): Player target, also supports natural spawning with "$" and waypoints with @(int).

Example: "sp 0 1" will spawn one enemy of ID zero naturally. Different moons assign different enemy ID's so make sure you check the "enemy" command to find the ID of the enemy you want to spawn.

---
### **TP/Tele/Teleport Optional: (Target A) (Target B)**

Teleports a given player to a given destination. Player being teleported can not be dead. Will automatically sync lighting if your destination is inside or outside. If no arguments are provided, the host will be teleported to the ship's console.

Arguments:
* Target A: If this is the only argument given it supports "!", "@(int)", and "$". Otherwise it must be a player.
* Target B: Can be a player, "!", "@(int)", or "$"

Example: "tp #0 $" will teleport the player with ID to a random location inside the factory.

---
### **WP/Waypoint/Waypoints Optional: Add/Clear/Door

Lists or creates a waypoint to use as a destination. Waypoints are cleared when leaving a moon.

Arguments (The text added after is the only argument accepted. If not provided it will list all waypoints):
* Add: Will create a waypoint at your current position.
* Clear: Will delete all waypoints.
* Door: Will create a waypoint inside the factory at the main entrance.

---
### **Ch/Charge Optional: (Player Target)**

Charges a players held item.

Arguments: 
* Player Target (Default: Host Player): Only supports player names/id.

Example: "ch" will simply charge the host's held item.

---
### **Heal/Save Optional: (Player Target)**

Fully refills a players health and stamina. Will save a player if they are currently grabbed by a forest giant or have a snare flea on their head. If target player is dead, they will be revived at the ship's terminal.

Arguments: 
* Player Target (Default: Host Player): Only supports player names/id.

Example: "heal John" will heal a player who's name starts with (or is) John.

---
### **List/Player/Players**

Lists all players currently in the server with their ID numbers.

---
### **Credit/Credits/Money Optional (Amount)**

Lists or adjusts the current amount of group credits in the terminal. If amount argument is not given it will just display the current amount of credits.

Arguments:
* Amount: The amount is the adjustment to be made to the current amount of credits.

Example: "credit -10" will subtract 10 from the current amount of group credits.

---
### **Code/Codes (Code)**

Toggles doors/turrets/mines by using their terminal code. If no argument is given it will list all terminal objects on the map.

Arguments:
* Code: The code that appears on the ship's map corresponding to the object.

Example: "code d2" will toggle all objects on the map with code d2.

---
### **Br/Breaker**

Toggles the breaker box's state.

---