# ToxicOmega Tools

*Tools triggered using the in-game chat. The main purpose of this mod is for debugging and testing other mods.*

---

## Commands
---
### **Item/Items (Page Number)**

Displays a page of the items list. Includes item names and ID numbers.

Arguments:
* Page Number: Specific page of the item list to view. Will default to the last page viewed.

Example: "item 2" displays page two of the items list

---
### **Give (Item ID) Optional: (Amount) (Value) (Player Name/ID)**

Spawns an item based on given ID number. Able to specify how many items, their value, and what player it spawns on.

Arguments:
* Item ID: Numerical ID of item you want to spawn. In the future names may be supported.
* Amount (Default: 1): How many copies of the item should be spawned.
* Value (Default: Random): Override the default value of the item with a given number.
* Player Name/ID (Default: Host Player): Name or ID number of the player you want to spawn the item at.

Example: "give 17 1 420 #3" will spawn one Airhorn worth $420 on the player who's ID is 3.

---
### **Eout/Enemyout/Enemiesout (Page Number)**

Displays a page of the outside enemies list. Includes enemy names and ID numbers. Currently in the game there is only one page of outside enemies.

Arguments:
* Page Number: Specific page of the outside enemies list to view. Will default to the last page viewed.

Example: "eout" displays page one of the outside enemies list

---
### **Ein/Enemyin/Enemiesin (Page Number)**

Displays a page of the inside enemies list. Includes enemy names and ID numbers. Currently in the game there is only one page of inside enemies.

Arguments:
* Page Number: Specific page of the inside enemies list to view. Will default to the last page viewed.

Example: "ein" displays page one of the inside enemies list

---
### **Sout/Spawnout (Enemy ID) Optional: (Amount) (Player Name/ID)**

Spawns an outside enemy based on given ID number. Able to specify how many enemies, and where they spawn. Make sure you use the ID from the **outside** enemies list!

Arguments:
* Enemy ID: Numerical ID of enemy you want to spawn. In the future names may be supported.
* Amount (Default: 1): How many copies of the enemy should be spawned.
* Player Name/ID (Default: None): Name or ID number of the player you want to spawn the item at. If no player argument is given the spawn location will default to a random enemy spawnpoint outside.

Example: "sout 0 1" will spawn one enemy of ID zero naturally outside. Different moons assign different enemy ID's so make sure you check "eout" to find the ID of the enemy you want to spawn.

---
### **Sin/Spawnin (Enemy ID) Optional: (Amount) (Player Name/ID)**

Spawns an inside enemy based on given ID number. Able to specify how many enemies, and where they spawn. Make sure you use the ID from the **inside** enemies list!

Arguments:
* Enemy ID: Numerical ID of enemy you want to spawn. In the future names may be supported.
* Amount (Default: 1): How many copies of the enemy should be spawned.
* Player Name/ID (Default: None): Name or ID number of the player you want to spawn the item at. If no player argument is given the spawn location will default to a random vent inside the factory.

Example: "sin 0 1 Bob" will spawn one enemy of ID zero on a player who's name starts with (or is) Bob. Different moons assign different enemy ID's so make sure you check "ein" to find the ID of the enemy you want to spawn.

---
### **TP/Tele/Teleport Optional: (Target A) (Target B)**

Teleports a given player to a given destination. Player being teleported can not be dead. Will automatically sync lighting if your destination is inside or outside. If no arguments are provided, the host will be teleported to the ship's console.

Arguments:
* Target A: This can be either "$" or a player name/ID. If "$" is used it will teleport the host to a random place within the factory as if an inverse-teleporter was used. If this is the only argument the host will be teleported to the given Target A.
* Target B: If target B is given as an argument the teleport will affect the player given as Target A and teleport them to Target B. "$" will teleport Target A to a random position in the factory. "!" will teleport them to the ship's console. Entering a player name/ID will teleport the Target A player to the Target B player.

Example: "tp #0 $" will teleport the player with ID 

---
### **Ch/Charge Optional: (Player Name/ID)**

Charges a players held item.

Arguments: 
* Player Name/ID (Default: Host Player): Name or ID number of the player who's held item you want to charge.

Example: "ch" will simply charge the host's held item.

---
### **Heal/Save Optional: (Player Name/ID)**

Fully refills a players health and stamina. Will save a player if they are currently grabbed by a forest giant or have a snare flea on their head.

Arguments: 
* Player Name/ID (Default: Host Player): Name or ID number of the player you want to heal.

Example: "heal John" will heal a player who's name starts with (or is) John.

---
### **List/Player/Players**

Lists all players currently in the server with their ID numbers.

Example: "list".

---
### **Credit/Credits/Money Optional (Amount)**
Lists or adjusts the current amount of group credits in the terminal. If amount argument is not given it will just display the current amount of credits.

Arguments:
* Amount (Default: None): The amount is the adjustment to be made to the current amount of credits.

Example: "credit -10" will subtract 10 from the current amount of group credits.

---