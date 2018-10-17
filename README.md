# Features:
* Can save blueprints on player disconnect (if they have blueprintguardian.use)
* Can restore blueprints automatically after a forced wipe.
* Give notifications that a player has saved data after a wipe (if auto restore is turned off)
* Get a list of player's unlocked blueprints.
* Unlock all blueprints for a player.
* Reset the blueprints of a player.
* Full language support.


*Note:* I locked most commands to edit other players behind permissions on the target. Chat commands are only for restoring and saving your own blueprints.

# Console Commands:
* bg save <playername> - save the blueprints of the specified player.
* bg restore <playername> - restore the blueprints of the specified player.
* bg delsaved <playername> - deletes the saved blueprints of the specified player.
* bg unlocked <playername> - lists the unlocked blueprints of the specified player.
* bg unlockall <playername> - unlocks all blueprints of the specified player.
* bg reset <playername> - resets the blueprints of the specified player.
* bg listsaved - gives a list of players who have saved blueprints/data.
* bg listsaved <playername> - lists the saved blueprints of the specified player.
* bg toggle - turns the plugin on and off (for the chat commands only)
* bg autorestore - turns autorestore on and off
* bg help - Gives a list of commands.



# Chat Commands:
* /bg save - saves your own blueprints
* /bg restore - restore your own blueprints

# Permissions:
* blueprintguardian.use
* blueprintguardian.admin


# Auto restore:

If the plugin is turned on and autoRestore is also turned on (by default both of these are off) the plugin will automatically detect if there's a new wipe and restore the blueprints of the players with the blueprintguardian.use permission.