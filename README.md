## Features

* Can save blueprints on player disconnect *(if they have blueprintguardian.use)*
* Can restore blueprints automatically after a forced wipe.
* Give notifications that a player has saved data after a wipe *(if auto restore is turned off)*
* Get a list of player's unlocked blueprints.
* Unlock all blueprints for a player.
* Reset the blueprints of a player.
* Full language support.

*Note:* I locked most commands to edit other players behind permissions on the target. Chat commands are only for restoring and saving your own blueprints.

## Permissions

* `blueprintguardian.use` - allows player to use the chat commands and base functionality
* `blueprintguardian.admin` - allows player to use the admin console ommands

## Commands

### Console

* `bg save <playername>` - save the blueprints of the specified player.
* `bg restore <playername>` - restore the blueprints of the specified player.
* `bg delsaved <playername>` - deletes the saved blueprints of the specified player.
* `bg unlocked <playername>` - lists the unlocked blueprints of the specified player.
* `bg unlockall <playername>` - unlocks all blueprints of the specified player.
* `bg reset <playername>` - resets the blueprints of the specified player.
* `bg listsaved` - gives a list of players who have saved blueprints/data.
* `bg listsaved <playername>` - lists the saved blueprints of the specified player.
* `bg toggle` - turns the plugin on and off (for the chat commands only)
* `bg autorestore` - turns autorestore on and off
* `bg help` - Gives a list of commands.

### Chat

* `/bg save` - saves your own blueprints
* `/bg restore` - restore your own blueprints

## Auto Restore

If the plugin is turned on and autoRestore is also turned on (by default both of these are off) the plugin will automatically detect if there's a new wipe and restore the blueprints of the players with the `blueprintguardian.use` permission.

## Configuration

* Version - Do not touch this value, is used for internal handling of updates
* AutoRestore true/false - Wether blueprints should automatically be restored after a wipe
* IsActivated true/false - Wether the plugin should allow chat commands, AutoRestore and auto save
* TargetNeedsPermission true/false - Blocks admin commands on players who do not have the permission
* LogAutoRestore true/false - Wether the auto restoring of blueprint gets logged to the console
* NeverPrintRestoredList true/false - Wether the full list of blueprints which have been restored for a player gets printed
* NotifyNotRestoredOnLogout true/false - Wether you get a message in the console if a player has logged of but hasn't yet restored their blueprints (if AutoRestore is false)
* DisableAllLogging true/false - Disables all logging of the plugin, besides warnings and errors.

**Default Config:**
```
{
  "Version": "0.1.6",
  "AutoRestore": true,
  "IsActivated": true,
  "TargetNeedsPermission": true,
  "LogAutoRestore": false,
  "NeverPrintRestoredList": true,
  "NotifyNotRestoredOnLogout": true,
  "DisableAllLogging": false
}
```
## Stored Data

This plugin stores data in the `data/BlueprintGuardian.json` file to keep track of unlocked blueprints, even after wipes. This file should not be deleted in most cases unless you want the unlocked blueprints stored there to be lost.

## Localization