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
Most if not all messages support localization. The messages should be pretty self explanatory.

**Default lang file:**
```
{
  "NoPermission": "You don't have permission to use this command.",
  "NoPermTarget": "{0} Doesn't have the required permissions.",
  "BlueprintsSaved": "{0} has the following blueprints saved: \n \n",
  "BlueprintsRestoredOwn": "Your blueprints have been restored.",
  "BlueprintsSavedOwn": "Your blueprints have been saved.",
  "BlueprintsRestored": "{1} blueprints have been restored for {0}.",
  "BlueprintsReset": "Blueprints of {0} have been reset.",
  "BlueprintsUnlocked": "{0} has the following blueprints unlocked: \n \n",
  "AllBlueprintsUnlocked": "All blueprints for {0} have been unlocked.",
  "BlueprintsSavedTarget": "Blueprints of {0} have succesfully been saved.",
  "NotRestoredYet": "{0} has disconnected and hasn't restored his blueprints yet, type \" bg restore {0}\" to do it for him/her",
  "NewWipe": "There's a new wipe detected, use \"/bg restore\" to restore your blueprints.",
  "NoItemFound": "Can't find an item with that shortname.",
  "PluginNotActivated": "Blueprint Guardian isn't activated, use <color=blue>\"bg toggle\"</color=blue> to activate it.",
  "PluginDeactivated": "Blueprint Guardian has been deactivated.",
  "PluginActivated": "Blueprint Guardian has been activated.",
  "NoSavedDataFound": "No Saved Blueprints found for {0}.",
  "NoPlayerFoundWith": "No player found with {0} in their name.",
  "NoPlayerFound": "No player found with given name.",
  "MultiplePlayersFoundWith": "No player found with {0} in their name.",
  "MultiplePlayersFound": "Multiple players found with given name.",
  "Activated": "Blueprint Guardian has been activated.",
  "Deactivated": "Blueprint Guardian has been deactivated.",
  "AutoRestoreActivated": "Auto restore has been activated.",
  "AutoRestoreDeactivated": "Auto restore has been deactivated.",
  "DebugActivated": "Debug/verbose logging has been activated",
  "DebugDeactivated": "Debug/verbose logging has been deactivated",
  "NoSavedDataFor": "No Saved blueprints/data found for {0}",
  "InvalidArgsConsole": "Invalid argument(s). for help use \"<color=#fda60a>bg help</color>\"",
  "InvalidArgsChat": "Invalid argument(s). for help use \"<color=#fda60a>/bg help</color>\"",
  "ConfirmReset": "Please confirm you want to reset {0}'s blueprints by typing: <color=#fda60a>bg reset {1} confirm</color>",
  "SavedDataPlayerList": "The following players have blueprints/data saved: \n \n",
  "AvailableCommands": "The following commands are available to use: \n \n",
  "HelpConsoleSave": "<color=#fda60a>bg save <playername></color> \t- Saves the given player's blueprints \n",
  "HelpConsoleRestore": "<color=#fda60a>bg restore <playername></color> \t- Restores the given player's blueprints \n",
  "HelpConsoleDelSaved": "<color=#fda60a>bg delsaved <playername></color> \t- Deletes the given player's saved blueprints \n",
  "HelpConsoleUnlocked": "<color=#fda60a>bg unlocked <playername></color> \t- Returns the unlocked/learned blueprints of the given playername \n",
  "HelpConsoleUnlockAll": "<color=#fda60a>bg unlockall <playername></color> \t- Unlocks all blueprints for the given player \n",
  "HelpConsoleReset": "<color=#fda60a>bg reset <playername></color> \t- Resets the blueprints of the given player \n",
  "HelpConsoleList": "<color=#fda60a>bg listsaved</color> \t- Lists all the players who have saved blueprints \n",
  "HelpListPlayerSaved": "<color=#fda60a>bg listsaved <playername></color> \t- Lists all the saved blueprints of the given player \n",
  "HelpConsoleToggle": "<color=#fda60a>bg toggle</color> \t- Turns the plugin on and off, only usable from console \n",
  "SomethingWrongBlueprint": "Oops, something went terribly wrong.",
  "RestoredBPList": "The following blueprints have been restored for {0} : {1}: \n"
}
```