using TShockAPI;
using TerrariaApi.Server;

namespace WordFilter
{
    public static class Commands
    {
        public static void Initialize(WordFilter plugin)
        {
            TShockAPI.Commands.ChatCommands.Add(new Command("wordfilter.manage", WordFilterCommand, "wordfilter", "wf")
            {
                HelpText = "Manage word filter. Usage: /wordfilter <add|remove|list|reload|config|warnings|resetwarnings> [args]"
            });
        }

        private static void WordFilterCommand(CommandArgs args)
        {
            var plugin = ServerApi.Plugins.FirstOrDefault(p => p.Plugin is WordFilter)?.Plugin as WordFilter;
            if (plugin == null)
            {
                args.Player.SendErrorMessage("WordFilter plugin not found!");
                return;
            }

            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Usage: /wordfilter <add|remove|list|reload|config|warnings|resetwarnings> [args]");
                return;
            }

            string subCommand = args.Parameters[0].ToLower();

            switch (subCommand)
            {
                case "add":
                    if (args.Parameters.Count < 3)
                    {
                        args.Player.SendErrorMessage("Usage: /wordfilter add <word> <replacement>");
                        return;
                    }
                    string word = args.Parameters[1];
                    string replacement = string.Join(" ", args.Parameters.Skip(2));
                    plugin.AddFilteredWord(word, replacement);
                    
                    var addMessage = plugin.GetConfig().CommandAddSuccessMessage
                        .Replace("{word}", word)
                        .Replace("{replacement}", replacement);
                    args.Player.SendSuccessMessage(addMessage);
                    break;

                case "remove":
                case "delete":
                    if (args.Parameters.Count < 2)
                    {
                        args.Player.SendErrorMessage("Usage: /wordfilter remove <word>");
                        return;
                    }
                    word = args.Parameters[1];
                    plugin.RemoveFilteredWord(word);
                    
                    var removeMessage = plugin.GetConfig().CommandRemoveSuccessMessage
                        .Replace("{word}", word);
                    args.Player.SendSuccessMessage(removeMessage);
                    break;

                case "list":
                    var words = plugin.GetFilteredWords();
                    var listCfg = plugin.GetConfig();
                    
                    if (words.Count == 0)
                    {
                        args.Player.SendInfoMessage(listCfg.CommandListNoWordsMessage);
                    }
                    else
                    {
                        var headerMessage = listCfg.CommandListHeaderMessage
                            .Replace("{count}", words.Count.ToString());
                        args.Player.SendInfoMessage(headerMessage);
                        
                        foreach (var kvp in words)
                        {
                            args.Player.SendInfoMessage($"  '{kvp.Key}' -> '{kvp.Value}'");
                        }
                    }
                    break;

                case "reload":
                    plugin.ReloadFilteredWords();
                    plugin.ReloadConfig();
                    args.Player.SendSuccessMessage(plugin.GetConfig().CommandReloadSuccessMessage);
                    break;

                case "config":
                    if (args.Parameters.Count < 2)
                    {
                        var config = plugin.GetConfig();
                        args.Player.SendInfoMessage("=== WordFilter Configuration ===");
                        args.Player.SendInfoMessage($"ChatAboveHead: {config.ChatAboveHead}");
                        args.Player.SendInfoMessage($"ChatAboveHeadDuration: {config.ChatAboveHeadDuration}");
                        args.Player.SendInfoMessage($"MaxMessageLength: {config.MaxMessageLength}");
                        args.Player.SendInfoMessage($"EnableWarnings: {config.EnableWarnings}");
                        args.Player.SendInfoMessage($"WarningsBeforeKick: {config.WarningsBeforeKick}");
                        args.Player.SendInfoMessage($"WarningsBeforeBan: {config.WarningsBeforeBan}");
                        args.Player.SendInfoMessage("Use: /wf config <setting> <value>");
                        return;
                    }

                    string setting = args.Parameters[1].ToLower();
                    if (args.Parameters.Count < 3)
                    {
                        args.Player.SendErrorMessage("Usage: /wordfilter config <setting> <value>");
                        return;
                    }

                    string value = args.Parameters[2];
                    var currentConfig = plugin.GetConfig();

                    switch (setting)
                    {
                        case "chatabovehead":
                            if (bool.TryParse(value, out bool chatAboveHead))
                            {
                                currentConfig.ChatAboveHead = chatAboveHead;
                                currentConfig.Write(Path.Combine(TShockAPI.TShock.SavePath, "WordFilterConfig.json"));
                                plugin.ReloadConfig();
                                args.Player.SendSuccessMessage($"ChatAboveHead set to: {chatAboveHead}");
                            }
                            else
                            {
                                args.Player.SendErrorMessage("Value must be true or false");
                            }
                            break;

                        case "maxmessagelength":
                            if (int.TryParse(value, out int maxLength))
                            {
                                currentConfig.MaxMessageLength = maxLength;
                                currentConfig.Write(Path.Combine(TShockAPI.TShock.SavePath, "WordFilterConfig.json"));
                                plugin.ReloadConfig();
                                args.Player.SendSuccessMessage($"MaxMessageLength set to: {maxLength}");
                            }
                            else
                            {
                                args.Player.SendErrorMessage("Value must be a number");
                            }
                            break;

                        case "enablewarnings":
                            if (bool.TryParse(value, out bool enableWarnings))
                            {
                                currentConfig.EnableWarnings = enableWarnings;
                                currentConfig.Write(Path.Combine(TShockAPI.TShock.SavePath, "WordFilterConfig.json"));
                                plugin.ReloadConfig();
                                args.Player.SendSuccessMessage($"EnableWarnings set to: {enableWarnings}");
                            }
                            else
                            {
                                args.Player.SendErrorMessage("Value must be true or false");
                            }
                            break;

                        default:
                            args.Player.SendErrorMessage($"Unknown setting: {setting}");
                            args.Player.SendErrorMessage("Available settings: ChatAboveHead, MaxMessageLength, EnableWarnings");
                            break;
                    }
                    break;

                case "warnings":
                case "warn":
                    if (args.Parameters.Count < 2)
                    {
                        var allWarnings = plugin.GetAllWarnings();
                        var activeWarnings = allWarnings.Where(w => w.Value.WarningCount > 0).ToList();
                        var cfg = plugin.GetConfig();
                        
                        if (activeWarnings.Count == 0)
                        {
                            args.Player.SendInfoMessage(cfg.CommandWarningsNoWarningsMessage);
                            return;
                        }

                        var headerMsg = cfg.CommandWarningsHeaderMessage
                            .Replace("{count}", activeWarnings.Count.ToString());
                        args.Player.SendInfoMessage(headerMsg);
                        
                        foreach (var kvp in activeWarnings.OrderByDescending(w => w.Value.WarningCount))
                        {
                            var timeSince = DateTime.Now - kvp.Value.LastWarningTime;
                            string timeFormat = FormatTime(timeSince, cfg);
                            
                            var infoMsg = cfg.CommandWarningInfoMessage
                                .Replace("{player}", kvp.Value.PlayerName)
                                .Replace("{warnings}", kvp.Value.WarningCount.ToString())
                                .Replace("{time}", timeFormat);
                            args.Player.SendInfoMessage(infoMsg);
                        }
                    }
                    else
                    {
                        var targetPlayers = TSPlayer.FindByNameOrID(args.Parameters[1]);
                        var cfg = plugin.GetConfig();
                        
                        if (targetPlayers.Count == 0)
                        {
                            args.Player.SendErrorMessage(cfg.CommandPlayerNotFoundMessage);
                            return;
                        }

                        var target = targetPlayers[0];
                        var warning = plugin.GetPlayerWarning(target.UUID);
                        
                        if (warning.WarningCount == 0)
                        {
                            var noWarnMsg = cfg.CommandPlayerNoWarningsMessage
                                .Replace("{player}", target.Name);
                            args.Player.SendInfoMessage(noWarnMsg);
                        }
                        else
                        {
                            var timeSince = DateTime.Now - warning.LastWarningTime;
                            string timeFormat = FormatTime(timeSince, cfg);
                            
                            var detailsMsg = cfg.CommandPlayerWarningDetailsMessage
                                .Replace("{player}", target.Name)
                                .Replace("{warnings}", warning.WarningCount.ToString())
                                .Replace("{time}", timeFormat);
                            args.Player.SendInfoMessage(detailsMsg);
                        }
                    }
                    break;

                case "resetwarnings":
                case "clearwarnings":
                    if (args.Parameters.Count < 2)
                    {
                        args.Player.SendErrorMessage("Usage: /wordfilter resetwarnings <player>");
                        return;
                    }

                    var resetTargets = TSPlayer.FindByNameOrID(args.Parameters[1]);
                    var resetCfg = plugin.GetConfig();
                    
                    if (resetTargets.Count == 0)
                    {
                        args.Player.SendErrorMessage(resetCfg.CommandPlayerNotFoundMessage);
                        return;
                    }

                    var resetTarget = resetTargets[0];
                    plugin.ResetPlayerWarnings(resetTarget.UUID);
                    
                    var resetMsg = resetCfg.CommandResetWarningsSuccessMessage
                        .Replace("{player}", resetTarget.Name);
                    args.Player.SendSuccessMessage(resetMsg);
                    TShockAPI.TShock.Log.ConsoleInfo($"[WordFilter] {args.Player.Name} reset warnings for {resetTarget.Name}");
                    break;

                default:
                    args.Player.SendErrorMessage("Unknown subcommand. Use: add, remove, list, reload, config, warnings, or resetwarnings");
                    break;
            }
        }

        private static string FormatTime(TimeSpan timeSpan, Config config)
        {
            if (timeSpan.TotalDays >= 1)
                return string.Format(config.TimeDaysFormat, timeSpan.TotalDays);
            if (timeSpan.TotalHours >= 1)
                return string.Format(config.TimeHoursFormat, timeSpan.TotalHours);
            return string.Format(config.TimeMinutesFormat, timeSpan.TotalMinutes);
        }
    }
}

