//#define DEBUG
//#define DEBUGADV
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Logging;
using Oxide.Game.Rust;

namespace Oxide.Plugins
{
    [Info("Blueprint Guardian", "Misstake", "0.1.6")]
    [Description("Saves blueprints and enables you to give them back to players, even after forced wipes.")]

    class BlueprintGuardian : RustPlugin
    {
#region Fields
        private bool _autoRestore;
        private bool _isActivated;
        private bool _isNewSave;
        private bool _targetNeedsPermission;
        private DynamicConfigFile _blueprintData;
        private BgData _bgData;
        private Dictionary<ulong, PlayerInfo> _cachedPlayerInfo;
        private const string PlayerPermission = "blueprintguardian.use";
        private const string AdminPermission = "blueprintguardian.admin";
#if BENCHMARK || DEBUG
//Is normally not in use, I normally log testing information to client console so is here for that reason.
        private string _developerNameIdorIp = "Misstake";
#endif


#endregion

#region Hooks

        private void Init()
        {
            _blueprintData = Interface.Oxide.DataFileSystem.GetFile(this.Name);
            permission.RegisterPermission(PlayerPermission, this);
            permission.RegisterPermission(AdminPermission, this);
            CheckProtocol();
            LoadVariables();
            LoadData();
        }

        private void Unload()
        {
            SaveData();
            SaveConfigData();
        }

        private void OnNewSave()
        {
            _isNewSave = true;
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (IsUser(player.UserIDString))
            {
                //Check if blueprints still have to be restored and don't save Blueprint if this is the case.
                if (_cachedPlayerInfo.ContainsKey(player.userID) && _cachedPlayerInfo[player.userID].RestoreOnce)
                {
                    
                    if(!_configData.DisableAllLogging && _configData.NotifyNotRestoredOnLogout)
                        Puts(Lang("NotRestoredYet", null, player.displayName));
                    return;
                }

                SaveBlueprints(player);
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (IsUser(player.UserIDString) || IsAdmin(player))
            {
                if (!_isActivated)
                    return;
                if (_cachedPlayerInfo.ContainsKey(player.userID) && _cachedPlayerInfo[player.userID].RestoreOnce)
                {
                    if (_autoRestore)
                    {
                        RestoreBlueprints(player, true);
                        PrintToChat(player, Lang("BlueprintsRestoredOwn", player.UserIDString));
                    }
                    else
                        PrintToChat(player, Lang("NewWipe", player.UserIDString));

                }
            }
        }
        #endregion

#region Functions

        private bool IsUser(string id) => permission.UserHasPermission(id, PlayerPermission);

        private bool IsAdmin(string userId) => permission.UserHasPermission(userId, AdminPermission);

        private bool IsAdmin(BasePlayer player) => permission.UserHasPermission(player.UserIDString, AdminPermission) || player.IsAdmin;

        private HashSet<string> GetPlayerBlueprints(BasePlayer player)
        {
            var bpList = ItemManager.GetBlueprints();
            var unlocked = new HashSet<string>();
            foreach (var item in bpList)
            {
                if (!item.defaultBlueprint)
                {
                    if (player.blueprints.IsUnlocked(item.targetItem))
                    {
                        unlocked.Add(item.targetItem.shortname);
                    }
                }
            }

            return unlocked;

        }

        private void SaveBlueprints(BasePlayer player)
        {
            var blueprints = GetPlayerBlueprints(player);
            if (blueprints == null)
            {
                SendWarning(null, "no blueprints object found for player.");
                return;
            }
            if (!_cachedPlayerInfo.ContainsKey(player.userID))
                _cachedPlayerInfo.Add(player.userID, new PlayerInfo());

            foreach (var blueprint in blueprints)
                _cachedPlayerInfo[player.userID].UnlockedBlueprints.Add(blueprint);

            SaveData();
        }

        private string RestoreBlueprints(BasePlayer player, bool autoRestore = false)
        {
            if (!_cachedPlayerInfo.ContainsKey(player.userID))
            {
                return null;
            }

            StringBuilder sb = new StringBuilder(Lang("RestoredBPList", null, player.displayName, player.UserIDString));
            foreach (String blueprint in _cachedPlayerInfo[player.userID].UnlockedBlueprints)
            {
                ItemDefinition itemDefinition = ItemManager.FindItemDefinition(blueprint);
                player.blueprints.Unlock(itemDefinition);
                sb.Append(itemDefinition.displayName.english  + ", ");
            }

            if (_cachedPlayerInfo[player.userID].RestoreOnce)
            {
                _cachedPlayerInfo[player.userID].RestoreOnce = false;
                SaveData();
            }
            // if logging is disabled just skip all the checks and return StringBuilder directly
            if(_configData.DisableAllLogging)
                return sb.ToString();

            if ((!autoRestore || _configData.LogAutoRestore) && !_configData.NeverPrintRestoredList)
                Puts(sb.ToString());
            else
                Puts(Lang("BlueprintsRestored", null, player.displayName, _cachedPlayerInfo[player.userID].UnlockedBlueprints.Count));

            return sb.ToString();
        }

        private bool RemoveSavedBlueprints(BasePlayer player)
        {
            if (_cachedPlayerInfo.ContainsKey(player.userID))
            {
                _cachedPlayerInfo.Remove(player.userID);
                SaveData();
                return true;
            }
            return false;
        }

        private void CheckProtocol()
        {
            Interface.Oxide.DataFileSystem.WriteObject("BlueprintGuardian_bak", _bgData);
            if (_isNewSave)
            {
                foreach (var entry in _cachedPlayerInfo)
                {
                    entry.Value.RestoreOnce = true;
                }

                SaveData();
                if(!_configData.DisableAllLogging)
                    Puts("Map wipe detected! Activating Auto Restore for all saved blueprints");
            }
        }

#endregion

#region commands
        [ChatCommand("bg")]
        private void BgChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!IsAdmin(player) && !IsUser(player.UserIDString))
            {
                SendReply(player, Lang("NoPermission", player.UserIDString));
                return;
            }

            if (args == null)
            {
                SendReply(player, Lang("InvalidArgsChat", player.UserIDString));
                return;
            }

            if (args.Length == 1)
            {
                switch (args[0].ToLower())
                {
                    case "save":
                        SaveBlueprints(player);
                        SendReply(player, Lang("BlueprintsSavedOwn", player.UserIDString));
                        return;
                    case "restore":
                        RestoreBlueprints(player);
                        SendReply(player, Lang("BlueprintsRestoredOwn", player.UserIDString));
                        return;
                    case "default":
                        SendReply(player, Lang("InvalidArgsChat", player.UserIDString));
                        return;
                }
            }
        }

        [ConsoleCommand("bg")]
        private void BgConsoleCommand(ConsoleSystem.Arg arg)
        {
            string userIdString = arg.Connection?.userid.ToString();
            if (arg.Connection != null)
            {
                if (!arg.IsAdmin && !IsAdmin(arg.Connection.userid.ToString()))
                {
                    SendReply(arg, Lang("NoPermission", userIdString));
                    return;
                }
            }

            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, Lang("InvalidArgsConsole", userIdString));
                return;
            }

            if (arg.Args.Length >= 0)
            {

                switch (arg.Args[0].ToLower())
                {
                    case "unlocked":
                        if (arg.Args.Length == 2)
                        {
                            BasePlayer target = RustCore.FindPlayer(arg.Args[1]);
                            if (target == null || target.blueprints == null)
                            {
                                SendReply(arg, Lang("NoPlayerFoundWith", userIdString, arg.Args[1]));
                                return;
                            }

                            String replyString = Lang("BlueprintsUnlocked", userIdString, target.displayName);
                            foreach (String a in GetPlayerBlueprints(target))
                            {
                                replyString += a + ", ";
                            }

                            SendReply(arg, replyString);
                        }
                        return;

                    case "save":
                        if (arg.Args.Length == 2)
                        {
                            BasePlayer target = RustCore.FindPlayer(arg.Args[1]);
                            if (target == null)
                            {
                                SendReply(arg, Lang("NoPlayerFoundWith", userIdString, arg.Args[1]));
                                return;
                            }
                            if (_targetNeedsPermission && !IsAdmin(target) && !IsUser(target.UserIDString))
                            {
                                SendReply(arg, Lang("NoPermTarget", userIdString, target.displayName));
                                return;
                            }
                            SaveBlueprints(target);
                            SendReply(arg, Lang("BlueprintsSavedTarget", userIdString, target.displayName));
                        }
                        return;

                    case "restore":
                        if (!_isActivated)
                        {
                            SendReply(arg, Lang("PluginNotActivated", userIdString));
                            return;
                        }
                        if (arg.Args.Length == 2)
                        {
                            BasePlayer target = RustCore.FindPlayer(arg.Args[1]);
                            if (target == null)
                            {
                                SendReply(arg, Lang("NoPlayerFoundWith", userIdString, arg.Args[1]));
                                return;
                            }

                            if (_targetNeedsPermission && !IsAdmin(target) && !IsUser(target.UserIDString))
                            {
                                SendReply(arg, Lang("NoPermTarget", userIdString, target.displayName));
                                return;
                            }

                            if (!_cachedPlayerInfo.ContainsKey(target.userID))
                            {
                                SendReply(arg, Lang("NoSavedDataFound", userIdString, target.displayName));
                                return;
                            }

                            if(arg.Connection != null)
                            {
                                SendReply(arg, RestoreBlueprints(target));
                            }
                        }
                        return;
                    case "unlock":
                        if (arg.Args.Length == 3)
                        {
                            BasePlayer target = RustCore.FindPlayer(arg.Args[1]);
                            if (target == null)
                            {
                                SendReply(arg, Lang("NoPlayerFoundWith", userIdString, arg.Args[1]));
                                return;
                            }

                            ItemDefinition itemDefinition = ItemManager.FindItemDefinition(arg.Args[2]);

                            if (itemDefinition == null)
                            {
                                SendReply(arg, Lang("NoItemFound", userIdString));
                                return;
                            }

                            if(!_configData.DisableAllLogging)
                                Puts($"{arg.Connection?.username ?? "Server Console"} unlocked blueprint {itemDefinition.displayName} for [{target.displayName}/{target.userID}]");
                            target.blueprints?.Unlock(itemDefinition);
                        }

                        return;

                    case "unlockall":
                        if (arg.Args.Length == 2)
                        {
                            BasePlayer target = RustCore.FindPlayer(arg.Args[1]);
                            if (target == null)
                            {
                                SendReply(arg, Lang("NoPlayerFoundWith", userIdString, arg.Args[1]));
                                return;
                            }

                            if (_targetNeedsPermission && !IsAdmin(target) && !IsUser(target.UserIDString))
                            {
                                SendReply(arg, Lang("NoPermTarget", userIdString, target.displayName));
                                return;
                            }

                            var blueprints = target.blueprints;
                            if (blueprints == null)
                                return;

                            blueprints.UnlockAll();
                            SendReply(arg, Lang("AllBlueprintsUnlocked", userIdString, target.displayName));
                            if(!_configData.DisableAllLogging)
                                Puts(Lang("AllBlueprintsUnlocked", userIdString, target.displayName));
                        }
                        return;

                    case "reset":
                        if (arg.Args.Length == 3)
                        {
                            BasePlayer target = RustCore.FindPlayer(arg.Args[1]);
                            if (target == null)
                            {
                                SendReply(arg, Lang("NoPlayerFoundWith", userIdString, arg.Args[1]));
                                return;
                            }

                            if (arg.Args[2].ToLower() == "confirm")
                            {
                                if (_targetNeedsPermission && !IsAdmin(target) && !IsUser(target.UserIDString))
                                {
                                    SendReply(arg, Lang("NoPermTarget", userIdString, target.displayName));
                                    return;
                                }

                                var blueprints = target.blueprints;
                                if (blueprints == null)
                                    return;

                                //Use buildin Rust function to reset blueprints
                                blueprints.Reset();

                                //Check if blueprints are present in data and if so clear it
                                PlayerInfo playerInfo;
                                if (_cachedPlayerInfo.TryGetValue(target.userID, out playerInfo))
                                {
                                    playerInfo.UnlockedBlueprints.Clear();
                                }

                                if(!_configData.DisableAllLogging)
                                    Puts(Lang("BlueprintsReset", null, target.displayName));
                                if (arg.Connection != null)
                                    SendReply(arg, Lang("BlueprintsReset", userIdString, target.displayName));
                            }
                            else
                                SendReply(arg, Lang("ConfirmReset", userIdString, target.displayName, arg.Args[2]));
                        }
                        else if (arg.Args.Length == 2)
                        {
                            BasePlayer target = RustCore.FindPlayer(arg.Args[1]);
                            if (target == null)
                            {
                                SendReply(arg, Lang("NoPlayerFoundWith", userIdString, arg.Args[1]));
                                return;
                            }

                            SendReply(arg, Lang("ConfirmReset", userIdString, target.displayName, arg.Args[1]));
                        }
                        else
                            SendReply(arg, Lang("InvalidArgsConsole", userIdString));
                        return;

                    case "toggle":
                        _isActivated = !_isActivated;
                        SaveConfigData();
                        SendReply(arg, _isActivated ? Lang("Activated", userIdString) : Lang("Deactivated", userIdString));
                        return;

                    case "autorestore":
                        _autoRestore = !_autoRestore;
                        SaveConfigData();
                        SendReply(arg, _isActivated ? Lang("AutoRestoreActivated", userIdString) : Lang("AutoRestoreDeactivated", userIdString));
                        return;

                    case "testautorestore":
                        _isNewSave = true;
                        SendReply(arg, $"isNewSave: {_isNewSave}");
                        CheckProtocol();
                        return;

                    case "delsaved":
                        if (arg.Args.Length == 2)
                        {
                            BasePlayer target = RustCore.FindPlayer(arg.Args[1]);
                            if (target == null)
                            {
                                SendReply(arg, Lang("NoPlayerFoundWith", userIdString, arg.Args[1]));
                                return;
                            }

                            if (!RemoveSavedBlueprints(target))
                                SendReply(arg, Lang("NoSavedDataFound", userIdString, target.displayName));
                            return;
                        }
                        else
                        {
                            SendReply(arg, Lang("InvalidArgs", userIdString));
                        }
                        return;

                    case "listsaved":
                        if (arg.Args.Length == 1)
                        {
                            StringBuilder sb = new StringBuilder(Lang("SavedDataPlayerList", userIdString));
                            foreach (ulong userId in _cachedPlayerInfo.Keys)
                            {
                                BasePlayer target = RustCore.FindPlayerById(userId);
                                sb.Append($"{userId} : {target?.displayName} \n");
                            }

                            SendReply(arg, sb.ToString());
                        }
                        if (arg.Args.Length == 2)
                        {
                            BasePlayer target = RustCore.FindPlayer(arg.Args[1]);
                            if (target == null)
                            {
                                SendReply(arg, Lang("NoPlayerFoundWith", userIdString, arg.Args[1]));
                                return;
                            }

                            if (!_cachedPlayerInfo.ContainsKey(target.userID))
                            {
                                SendReply(arg, Lang("NoSavedDataFor", userIdString, target.displayName));
                                return;
                            }

                            HashSet<String> blueprints = _cachedPlayerInfo[target.userID].UnlockedBlueprints;
                            if (blueprints == null)
                                return;
                            SendReply(arg, Lang("BlueprintsSaved", userIdString, target.displayName) + String.Join(", ", blueprints));
                        }

                        return;
                    case "default":
                    case "help":
                        StringBuilder _sb = new StringBuilder(Lang("AvailableCommands", userIdString));
                        _sb.Append(Lang("HelpConsoleSave", userIdString));
                        _sb.Append(Lang("HelpConsoleRestore", userIdString));
                        _sb.Append(Lang("HelpConsoleDelSaved", userIdString));
                        _sb.Append(Lang("HelpConsoleUnlocked", userIdString));
                        _sb.Append(Lang("HelpConsoleUnlockAll", userIdString));
                        _sb.Append(Lang("HelpConsoleReset", userIdString));
                        _sb.Append(Lang("HelpConsoleList", userIdString));
                        _sb.Append(Lang("HelpListPlayerSaved", userIdString));
                        _sb.Append(Lang("HelpConsoleToggle", userIdString));

                        SendReply(arg,
                            arg.Connection == null ? Regex.Replace(_sb.ToString(), "<[^>]*>", "") : _sb.ToString());
                        return;
                }
            }
        }
#endregion

#region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                //Included the color tags in some messages as a workaround to add lines at the end of the sentence.
                ["NoPermission"] = "You don't have permission to use this command.",
                ["NoPermTarget"] = "{0} Doesn't have the required permissions.",

                ["BlueprintsSaved"] = "{0} has the following blueprints saved: \n \n",
                ["BlueprintsRestoredOwn"] = "Your blueprints have been restored.",
                ["BlueprintsSavedOwn"] = "Your blueprints have been saved.",
                ["BlueprintsRestored"] = "{1} blueprints have been restored for {0}.",
                ["BlueprintsReset"] = "Blueprints of {0} have been reset.",
                ["BlueprintsUnlocked"] = "{0} has the following blueprints unlocked: \n \n",
                ["AllBlueprintsUnlocked"] = "All blueprints for {0} have been unlocked.",
                ["BlueprintsSavedTarget"] = "Blueprints of {0} have succesfully been saved.",
                ["NotRestoredYet"] = "{0} has disconnected and hasn't restored his blueprints yet, type \" bg restore {0}\" to do it for him/her",
                ["NewWipe"] = "There's a new wipe detected, use \"/bg restore\" to restore your blueprints.",
                ["NoItemFound"] = "Can't find an item with that shortname.",

                ["PluginNotActivated"] = "Blueprint Guardian isn't activated, use <color=blue>\"bg toggle\"</color=blue> to activate it.",
                ["PluginDeactivated"] = "Blueprint Guardian has been deactivated.",
                ["PluginActivated"] = "Blueprint Guardian has been activated.",
                ["NoSavedDataFound"] = "No Saved Blueprints found for {0}.",
                ["NoPlayerFoundWith"] = "No player found with {0} in their name.",
                ["NoPlayerFound"] = "No player found with given name.",
                ["MultiplePlayersFoundWith"] = "No player found with {0} in their name.",
                ["MultiplePlayersFound"] = "Multiple players found with given name.",
                ["Activated"] = "Blueprint Guardian has been activated.",
                ["Deactivated"] = "Blueprint Guardian has been deactivated.",
                ["AutoRestoreActivated"] = "Auto restore has been activated.",
                ["AutoRestoreDeactivated"] = "Auto restore has been deactivated.",
                ["DebugActivated"] = "Debug/verbose logging has been activated",
                ["DebugDeactivated"] = "Debug/verbose logging has been deactivated",
                ["NoSavedDataFor"] = "No Saved blueprints/data found for {0}",

                ["InvalidArgsConsole"] = "Invalid argument(s). for help use \"<color=#fda60a>bg help</color>\"",
                ["InvalidArgsChat"] = "Invalid argument(s). for help use \"<color=#fda60a>/bg help</color>\"",
                ["ConfirmReset"] = "Please confirm you want to reset {0}'s blueprints by typing: <color=#fda60a>bg reset {1} confirm</color>",
                ["SavedDataPlayerList"] = "The following players have blueprints/data saved: \n \n",
                ["AvailableCommands"] = "The following commands are available to use: \n \n",
                ["HelpConsoleSave"] = "<color=#fda60a>bg save <playername></color> \t- Saves the given player's blueprints \n",
                ["HelpConsoleRestore"] = "<color=#fda60a>bg restore <playername></color> \t- Restores the given player's blueprints \n",
                ["HelpConsoleDelSaved"] = "<color=#fda60a>bg delsaved <playername></color> \t- Deletes the given player's saved blueprints \n",
                ["HelpConsoleUnlocked"] = "<color=#fda60a>bg unlocked <playername></color> \t- Returns the unlocked/learned blueprints of the given playername \n",
                ["HelpConsoleUnlockAll"] = "<color=#fda60a>bg unlockall <playername></color> \t- Unlocks all blueprints for the given player \n",
                ["HelpConsoleReset"] = "<color=#fda60a>bg reset <playername></color> \t- Resets the blueprints of the given player \n",
                ["HelpConsoleList"] = "<color=#fda60a>bg listsaved</color> \t- Lists all the players who have saved blueprints \n",
                ["HelpListPlayerSaved"] = "<color=#fda60a>bg listsaved <playername></color> \t- Lists all the saved blueprints of the given player \n",
                ["HelpConsoleToggle"] = "<color=#fda60a>bg toggle</color> \t- Turns the plugin on and off, only usable from console \n",
                ["SomethingWrongBlueprint"] = "Oops, something went terribly wrong.",
                ["RestoredBPList"] = "The following blueprints have been restored for {0} : {1}: \n"

            }, this);
        }

        private string Lang(string key, string userId = null, params object[] args)
        {
            var message = lang.GetMessage(key, this, userId);
            if (args.Length != 0)
                message = string.Format(message, args);

            return covalence.FormatText(message);
        }




#endregion

#region DataManagement

        class BgData
        {
            public Dictionary<ulong, PlayerInfo> Inventories = new Dictionary<ulong, PlayerInfo>();
        }

        private class PlayerInfo
        {
            public bool RestoreOnce;
            public HashSet<string> UnlockedBlueprints = new HashSet<string>();
        }

        private void SaveData()
        {
            _bgData.Inventories = _cachedPlayerInfo;
            _blueprintData.WriteObject(_bgData);
        }

        private void LoadData()
        {
            try
            {
                _bgData = _blueprintData.ReadObject<BgData>();
                _cachedPlayerInfo = _bgData.Inventories;
#if DEBUG
                Puts("Blueprint data loaded");
#endif
            }
            catch
            {
                Puts("Couldn't load player blueprint data, creating new datafile");
                _bgData = new BgData();
            }
            if (_bgData == null)
            {
                Puts("Couldn't load player blueprint data, creating new datafile");
                _bgData = new BgData();
            }
        }

#endregion

#region config

        private ConfigData _configData;
        class ConfigData
        {
            public string Version { get; set; }
            public int AuthLevel { get; set; }
            public bool AutoRestore { get; set; }
            public bool IsActivated { get; set; }
            public bool TargetNeedsPermission { get; set; }
            public bool LogAutoRestore { get; set; }
            public bool NeverPrintRestoredList { get; set; } = true;
            public bool NotifyNotRestoredOnLogout { get; set; }
            public bool DisableAllLogging { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();

            CheckUpdate(_configData.Version);
            _autoRestore = _configData.AutoRestore;
            _isActivated = _configData.IsActivated;
            _targetNeedsPermission = _configData.TargetNeedsPermission;
            SaveConfig(_configData);
        }

        private void CheckUpdate(string version)
        {
            if (version == null)
                ReloadConfig();
            else if (version != Version.ToString())
                ReloadConfig();
        }

        private void SaveConfigData()
        {
            _configData.IsActivated = _isActivated;
            _configData.AutoRestore = _autoRestore;
            _configData.TargetNeedsPermission = _targetNeedsPermission;
            SaveConfig(_configData);
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                AutoRestore = false,
                IsActivated = false,
                TargetNeedsPermission = true,
                LogAutoRestore = true,
                NeverPrintRestoredList = false,
                NotifyNotRestoredOnLogout = true,
                DisableAllLogging = false
            };
            SaveConfig(config);
        }

        // Can be used to upgrade configuration if options are added to config file
        protected void ReloadConfig()
        {
            Puts($"Upgrading configuration file from {_configData.Version} to {Version.ToString()}");
            _configData.Version = Version.ToString();

            // NEW CONFIGURATION OPTIONS HERE
            _configData.NotifyNotRestoredOnLogout = true;
            _configData.DisableAllLogging = false;
            // END NEW CONFIGURATION OPTIONS

            SaveConfig();
        }

        private void LoadConfigVariables() => _configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);

#endregion
    }
}