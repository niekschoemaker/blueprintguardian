using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust;

namespace Oxide.Plugins
{
    [Info("Blueprint Guardian", "Misstake", "0.1.1")]
    [Description("Saves blueprints and enables you to give them back to players, even after forced wipes.")]

    class BlueprintGuardian : RustPlugin
    {
        #region Fields
        private bool _debug;
        private bool _autoRestore;
        private bool _isActivated;
        private bool _isNewSave = false;
        private bool _targetNeedsPermission;
        int _authLevel = 1;
        private DynamicConfigFile _blueprintData;
        private BgData _bgData;
        private Dictionary<ulong, PlayerInfo> _cachedPlayerInfo;

        #endregion

        #region Hooks

        private void Loaded()
        {
            _blueprintData = Interface.Oxide.DataFileSystem.GetFile("BlueprintGuardian");
        }

        private void OnServerInitialized()
        {
            LoadDefaultMessages();
            permission.RegisterPermission("blueprintguardian.use", this);
            permission.RegisterPermission("blueprintguardian.admin", this);
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
            if(IsUser(player))
            {
                if (_cachedPlayerInfo.ContainsKey(player.userID) && _cachedPlayerInfo[player.userID].RestoreOnce)
                {
                    Puts(Lang("NotRestoredYet"));
                    return;
                }

                SaveBlueprints(player);
            }
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if(IsUser(player))
            {
                if (!_isActivated)
                    return;
                if(_cachedPlayerInfo.ContainsKey(player.userID) && _cachedPlayerInfo[player.userID].RestoreOnce)
                {
                    if (_autoRestore)
                    {
                        RestoreBlueprints(player);
                        PrintToChat(player, Lang(player, "BlueprintsRestoredOwn"));
                    }
                    else
                        PrintToChat(player, Lang(player, "NewWipe"));
                    
                }
            }
        }
        #endregion

        #region Functions
        private bool IsUser(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "blueprintguardian.use"))
                return true;

            return false;
        }

        private bool IsUser(ulong userID)
        {
            if (permission.UserHasPermission(Convert.ToString(userID), "blueprintguardian.use"))
                return true;

            return false;
        }

        private bool IsAdmin(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "blueprintguardian.admin") || player.IsAdmin)
                return true;

            return false;
        }

        private bool IsAdmin(ulong userID)
        {
            BasePlayer target = RustCore.FindPlayerById(userID);
            if (target != null)
                return IsAdmin(target);
            if (permission.UserHasPermission(Convert.ToString(userID), "blueprintguardian.admin"))
                return true;

            return false;
        }

        private List<BasePlayer> GetBasePlayers (bool sleepers = true)
        {
            List<BasePlayer> playerList = BasePlayer.activePlayerList;
            if (sleepers)
                playerList.AddRange(BasePlayer.sleepingPlayerList);
            if (playerList == null)
            {
                Puts("No players found");
                return new List<BasePlayer>();
            }
            return playerList;
        }
        
        private BasePlayer FindPlayer(BasePlayer player, string nameOrID, bool sleepers = false)
        {
            var targets = BasePlayer.activePlayerList.FindAll(x => nameOrID.IsSteamId() ? x.UserIDString == nameOrID : x.displayName.ToLower().Contains(nameOrID));
            if(sleepers && targets.Count == 0)
                targets.AddRange(BasePlayer.sleepingPlayerList.FindAll(x => nameOrID.IsSteamId() ? x.UserIDString == nameOrID : x.displayName.ToLower().Contains(nameOrID)));

            if (targets.Count == 1)
                return targets[0];

            //PrintToChat(player, Lang(player, targets.Count == 0 ? "NoPlayerFound" : "MultiplePlayersFound"), nameOrID);
            SendReply(player, targets.Count == 0 ? Lang("NoPlayerFound") : Lang("MultiplePlayersFound"));
            SendReply(player, Convert.ToString(targets.Count));
            return null;
        }

        private BasePlayer FindPlayer(ConsoleSystem.Arg arg, string nameOrID, bool sleepers = false)
        {
            var targets = BasePlayer.activePlayerList.FindAll(x => nameOrID.IsSteamId() ? x.UserIDString == nameOrID : x.displayName.ToLower().Contains(nameOrID));
            if(sleepers && targets.Count == 0)
                targets.AddRange(BasePlayer.sleepingPlayerList.FindAll(x => nameOrID.IsSteamId() ? x.UserIDString == nameOrID : x.displayName.ToLower().Contains(nameOrID)));
            if (targets.Count == 1)
                return targets[0];

            SendReply(arg, targets.Count == 0 ? Lang("NoPlayerFoundWith", arg.Args[1]) : Lang("MultiplePlayersFoundWith", arg.Args[1]));
            return null;
        }

        private List<String> GetPlayerBlueprints(BasePlayer player)
        {
            List<ItemBlueprint> bpList = ItemManager.GetBlueprints();
            List<String> unlocked = new List<String>();
            foreach (ItemBlueprint item in bpList)
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

        private bool SaveBlueprints(BasePlayer player)
        {
            List<String> blueprints = GetPlayerBlueprints(player);
            if (blueprints == null)
            {
                SendWarning(null, "no blueprints object found for player.");
                return false;
            }
            if (!_cachedPlayerInfo.ContainsKey(player.userID))
                _cachedPlayerInfo.Add(player.userID, new PlayerInfo());
            _cachedPlayerInfo[player.userID].UnlockedBlueprints = blueprints;
            SaveData();
            return true;
        }

        private bool RestoreBlueprints(BasePlayer player)
        {
            if (!_cachedPlayerInfo.ContainsKey(player.userID))
            {
                return false;
            }

            foreach (String blueprint in _cachedPlayerInfo[player.userID].UnlockedBlueprints)
            {
                ItemDefinition itemDefinition = ItemManager.FindItemDefinition(blueprint);
                player.blueprints.Unlock(itemDefinition);
                if (_debug) Puts($"Blueprint {blueprint} has been unlocked for player {player.displayName}");
            }

            if (_cachedPlayerInfo[player.userID].RestoreOnce)
            {
                _cachedPlayerInfo[player.userID].RestoreOnce = false;
                SaveData();
            }

            Puts($"Blueprints restored for {player.displayName} : {player.UserIDString}");
            return true;
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
            if (_isNewSave)
            {
                foreach (var entry in _cachedPlayerInfo)
                    entry.Value.RestoreOnce = true;
                SaveData();
                Puts("Map wipe detected! Activating Auto Restore for all saved blueprints");
            }
        }

        #endregion

        #region commands
        [ChatCommand("bg")]
        private void bgChatCommand(BasePlayer player, string command, string[] args)
        {
            if(!IsAdmin(player) && !IsUser(player))
            {
                SendReply(player, Lang(player, "NoPermission"));
                return;
            }

            if(args == null)
            {
                SendReply(player, Lang(player, "InvalidArgsChat"));
                return;
            }

            if(args.Length == 1)
            {
                switch (args[0].ToLower())
                {
                    case "save":
                        SaveBlueprints(player);
                        SendReply(player, Lang(player, "BlueprintsSavedOwn"));
                        return;
                    case "restore":
                        RestoreBlueprints(player);
                        SendReply(player, Lang(player, "BlueprintsRestoredOwn"));
                        return;
                    case "default":
                        SendReply(player, Lang(player, "InvalidArgsChat"));
                        return;
                }
            }
        }

        [ConsoleCommand("bg")]
        private void bgConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                if (arg.Connection.authLevel < _authLevel && !IsAdmin(arg.Connection.userid))
                {
                    SendReply(arg, Lang(arg, "NoPermission"));
                    return;
                }
            }

            if(arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, Lang(arg, "InvalidArgsConsole"));
                return;
            }

            if(arg.Args.Length >= 0)
            {

                switch (arg.Args[0].ToLower())
                {
                    case "unlocked":
                        if (arg.Args.Length == 2)
                        {
                            BasePlayer target = FindPlayer(arg, arg.Args[1], true);
                            if (target == null || target.blueprints == null)
                                return;
                            String replyString = Lang(arg, "BlueprintsUnlocked", target.displayName);
                            foreach(String a in GetPlayerBlueprints(target))
                            {
                                replyString += a + ", ";
                            }

                            SendReply(arg, replyString);
                        }
                        return;

                    case "save":
                        if (arg.Args.Length == 2)
                        {
                            BasePlayer target = FindPlayer(arg, arg.Args[1], true);
                            if (target == null) return;
                            if (_targetNeedsPermission && !IsAdmin(target) && !IsUser(target))
                            {
                                SendReply(arg, Lang(arg, "NoPermTarget", target.displayName));
                                return;
                            }
                            SaveBlueprints(target);
                            SendReply(arg, Lang(arg, "BlueprintsSavedTarget", target.displayName));
                        }
                        return;

                    case "restore":
                        if (!_isActivated)
                        {
                            SendReply(arg, Lang(arg, "PluginNotActivated"));
                            return;
                        }
                        if (arg.Args.Length == 2)
                        {
                            BasePlayer target = FindPlayer(arg, arg.Args[1], true);
                            if (target != null)
                            {
                                if (_targetNeedsPermission && !IsAdmin(target) && !IsUser(target))
                                {
                                    SendReply(arg, Lang(arg, "NoPermTarget", target.displayName));
                                    return;
                                }

                                if (!_cachedPlayerInfo.ContainsKey(target.userID))
                                {
                                    SendReply(arg, Lang(arg, "NoSavedDataFound", target.displayName));
                                    return;
                                }

                                var blueprints = target.blueprints;
                                if (blueprints == null)
                                    return;

                                var count = 0;
                                foreach (String blueprint in _cachedPlayerInfo[target.userID].UnlockedBlueprints)
                                {
                                    ItemDefinition itemDefinition = ItemManager.FindItemDefinition(blueprint);
                                    blueprints.Unlock(itemDefinition);
                                    count++;
                                }
                                SendReply(arg, Lang(arg, "BlueprintsRestored", target.displayName, count));
                                Puts(Lang("BlueprintsRestored", target.displayName, count));
                            }
                        }
                        return;
                    case "unlock":
                        if (arg.Args.Length == 3)
                        {
                            BasePlayer target = FindPlayer(arg, arg.Args[1], true);
                            ItemDefinition itemDefinition = ItemManager.FindItemDefinition(arg.Args[2]);

                            if (itemDefinition == null)
                            {
                                SendReply(arg, Lang(arg, "NoItemFound"));
                                return;
                            }

                            var blueprints = target.blueprints;
                            if (blueprints == null)
                                return;

                            blueprints.Unlock(itemDefinition);
                        }

                        return;

                    case "unlockall":
                        if (arg.Args.Length == 2)
                        {
                            BasePlayer target = FindPlayer(arg, arg.Args[1], true);
                            if (target != null)
                            {
                                if (_targetNeedsPermission && !IsAdmin(target) && !IsUser(target))
                                {
                                    SendReply(arg, Lang(arg, "NoPermTarget", target.displayName));
                                    return;
                                }

                                var blueprints = target.blueprints;
                                if (blueprints == null)
                                    return;

                                blueprints.UnlockAll();
                                SendReply(arg, Lang(arg, "AllBlueprintsUnlocked", target.displayName));
                                Puts(Lang("AllBlueprintsUnlocked", target.displayName));
                            }
                        }
                        return;

                    case "reset":
                        if (arg.Args.Length == 3)
                        {
                            BasePlayer target = FindPlayer(arg, arg.Args[1], true);
                            if (arg.Args[2].ToLower() == "confirm")
                            {
                                if (target != null)
                                {
                                    if (_targetNeedsPermission && !IsAdmin(target) && !IsUser(target))
                                    {
                                        SendReply(arg, Lang(arg, "NoPermTarget", target.displayName));
                                        return;
                                    }

                                    var blueprints = target.blueprints;
                                    if (blueprints == null)
                                        return;

                                    blueprints.Reset();
                                    Puts(Lang("BlueprintsReset", target.displayName));
                                    if(arg.Connection != null)
                                        SendReply(arg, Lang(arg, "BlueprintsReset", target.displayName));
                                }
                            }
                            else
                                SendReply(arg, Lang(arg, "ConfirmReset", target.displayName, arg.Args[2]));
                        }
                        else if (arg.Args.Length == 2)
                        {
                            BasePlayer target = FindPlayer(arg, arg.Args[1], true);
                            if(target != null)
                                SendReply(arg, Lang(arg, "ConfirmReset", target.displayName, arg.Args[1]));
                        }
                        else
                            SendReply(arg, Lang(arg, "InvalidArgsConsole"));
                        return;

                    case "toggle":
                        _isActivated = !_isActivated;
                        SaveConfigData();
                        SendReply(arg, _isActivated ? Lang(arg, "Activated") : Lang(arg, "Deactivated"));
                        return;

                    case "debug":
                        _debug = !_debug;
                        SaveConfigData();
                        SendReply(arg, _debug ? Lang(arg, "DebugActivated") : Lang(arg, "DebugDeactivated"));
                        return;

                    case "autorestore":
                        _autoRestore = !_autoRestore;
                        SaveConfigData();
                        SendReply(arg, _isActivated ? Lang(arg, "AutoRestoreActivated") : Lang(arg, "AutoRestoreDeactivated"));
                        return;

                    case "testautorestore":
                        _isNewSave = true;
                        SendReply(arg, $"isNewSave: {_isNewSave}");
                        CheckProtocol();
                        return;

                    case "delsaved":
                        if (arg.Args.Length == 2)
                        {
                            BasePlayer player = FindPlayer(arg, arg.Args[1], true);
                            if (player != null)
                            {
                                if (!RemoveSavedBlueprints(player))
                                    SendReply(arg, Lang(arg, "NoSavedDataFound", player.displayName));
                                return;
                            }
                            SendReply(arg, Lang(arg, "NoPlayerFound", arg.Args[1]));
                        } else
                        {
                            SendReply(arg, Lang(arg, "InvalidArgs"));
                        }
                        return;

                    case "listsaved":
                        if (arg.Args.Length == 1)
                        {
                            string replyString = Lang(arg, "SavedDataPlayerList");
                            foreach(ulong userId in _cachedPlayerInfo.Keys)
                            {
                                BasePlayer target = RustCore.FindPlayerById(userId);
                                replyString += userId + " : " + target.displayName + "\n";
                            }
                            SendReply(arg, replyString);
                        }
                        if (arg.Args.Length == 2)
                        {
                            BasePlayer target = RustCore.FindPlayerByName(arg.Args[1]);
                            if (target == null)
                                return;

                            if(!_cachedPlayerInfo.ContainsKey(target.userID))
                            {
                                SendReply(arg, Lang(arg, "NoSavedDataFor", target.displayName));
                                return;
                            }

                            List<String> blueprints = _cachedPlayerInfo[target.userID].UnlockedBlueprints;
                            if (blueprints == null)
                                return;
                            SendReply(arg, Lang(arg, "BlueprintsSaved", target.displayName) + String.Join(", ", blueprints));
                        }

                        return;
                    case "default":
                    case "help":
                        SendReply(arg, Lang(arg, "HelpConsoleSave"));
                        SendReply(arg, Lang(arg, "HelpConsoleRestore"));
                        SendReply(arg, Lang(arg, "HelpConsoleDelSaved"));
                        SendReply(arg, Lang(arg, "HelpConsoleUnlocked"));
                        SendReply(arg, Lang(arg, "HelpConsoleUnlockall"));
                        SendReply(arg, Lang(arg, "HelpConsoleReset"));
                        SendReply(arg, Lang(arg, "HelpConsoleList"));
                        SendReply(arg, Lang(arg, "HelpListPlayerSaved"));
                        SendReply(arg, Lang(arg, "HelpConsoleToggle"));
                        return;
                }
            }
        }
        #endregion

        #region Localization

        protected override void LoadDefaultMessages() => RegisterBgMessages();

        private void RegisterBgMessages()
        {
            Puts("Loading default lang messages");
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

                ["InvalidArgsConsole"] = "Invalid argument(s). for help use \"bg help\"",
                ["InvalidArgsChat"] = "Invalid argument(s). for help use \"/bg help\"",
                ["ConfirmReset"] = "Please confirm you want to reset {0}'s blueprints by typing: bg reset {1} confirm",
                ["SavedDataPlayerList"] = "The following players have blueprints/data saved: \n \n",
                ["HelpConsoleSave"] = "bg save <playername> - Saves the given player's blueprints",
                ["HelpConsoleRestore"] = "bg restore <playername> - Restores the given player's blueprints",
                ["HelpConsoleDelSaved"] = "bg delsaved <playername> - Deletes the given player's saved blueprints",
                ["HelpConsoleUnlocked"] = "bg unlocked <playername> - Returns the unlocked/learned blueprints of the given playername",
                ["HelpConsoleUnlockAll"] = "bg unlockall <playername> - Unlocks all blueprints for the given player.",
                ["HelpConsoleReset"] = "bg reset <playername> - Resets the blueprints of the given player.",
                ["HelpConsoleList"] = "bg listsaved - Lists all the players who have saved blueprints",
                ["HelpListPlayerSaved"] = "bg listsaved <playername> - Lists all the saved blueprints of the given player.",
                ["HelpConsoleToggle"] = "bg toggle - Turns the plugin on and off, only usable from console.",
                ["SomethingWrongBlueprints"] = "Oops, something went terribly wrong.",

            }, this);
        }
        private string Lang(BasePlayer player, string key, params object[] args)
        {
            var message = lang.GetMessage(key, this, player == null ? null : player.UserIDString);
            if (args.Length != 0)
                message = string.Format(message, args);

            return covalence.FormatText(message);
        }

        private string Lang(ConsoleSystem.Arg arg, string key, params object[] args)
        {
            var userId = arg?.Connection == null ? null : Convert.ToString(arg.Connection.userid);
            var message = lang.GetMessage(key, this, userId);
            if (args.Length != 0)
                message = string.Format(message, args);

            return covalence.FormatText(message);
        }

        private string Lang(string key, params object[] args)
        {
            var message = lang.GetMessage(key, this);
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
            public List<string> UnlockedBlueprints;
        }

        private void SaveData()
        {
            _bgData.Inventories = _cachedPlayerInfo;
            _blueprintData.WriteObject(_bgData);
        }
        private void SaveLoop() => timer.Once(900, () => { SaveData(); SaveLoop(); });
        private void LoadData()
        {
            try
            {
                _bgData = _blueprintData.ReadObject<BgData>();
                _cachedPlayerInfo = _bgData.Inventories;
                Puts("Loading data.");

            }
            catch
            {
                Puts("Couldn't load player data, creating new datafile");
                _bgData = new BgData();
            }
            if (_bgData == null)
            {
                Puts("Couldn't load player data, creating new datafile");
                _bgData = new BgData();
            }
        }

        #endregion

        #region config

        private ConfigData _configData;
        class ConfigData
        {
            public string Version { get; set; }
            public string MessagesMainColor { get; set; }
            public string MessagesMsgColor { get; set; }
            public int AuthLevel { get; set; }
            public bool AutoRestore { get; set; }
            public bool IsActivated { get; set; }
            public bool Debug { get; set; }
            public bool TargetNeedsPermission { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();

            CheckUpdate(_configData.Version);
            _debug = _configData.Debug;
            _autoRestore = _configData.AutoRestore;
            _isActivated = _configData.IsActivated;
            _authLevel = _configData.AuthLevel;
            _targetNeedsPermission = _configData.TargetNeedsPermission;
            SaveConfig(_configData);
            if (_debug) Puts("Config loaded.");
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
            _configData.AuthLevel = _authLevel;
            _configData.IsActivated = _isActivated;
            _configData.AutoRestore = _autoRestore;
            _configData.Debug = _debug;
            _configData.TargetNeedsPermission = _targetNeedsPermission;
            SaveConfig(_configData);
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                MessagesMainColor = "<color=#FF8C00>",
                MessagesMsgColor = "<color=#939393>",
                AuthLevel = 2,
                AutoRestore = false,
                IsActivated = false,
                Debug = true,
                TargetNeedsPermission = true
            };
            SaveConfig(config);
        }

        protected void ReloadConfig()
        {
            Puts($"Upgrading configuration file from {_configData.Version} to {Version.ToString()}");
            _configData.Version = Version.ToString();

            // NEW CONFIGURATION OPTIONS HERE
            // END NEW CONFIGURATION OPTIONS

            SaveConfig();
        }

        private void LoadConfigVariables() => _configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);

        #endregion
    }
}
