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
                    args.Player.SendSuccessMessage($"Added word filter: '{word}' -> '{replacement}'");
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
                    args.Player.SendSuccessMessage($"Removed word filter: '{word}'");
                    break;

                case "list":
                    var words = plugin.GetFilteredWords();
                    if (words.Count == 0)
                    {
                        args.Player.SendInfoMessage("No filtered words configured.");
                    }
                    else
                    {
                        args.Player.SendInfoMessage($"Filtered words ({words.Count}):");
                        foreach (var kvp in words)
                        {
                            args.Player.SendInfoMessage($"  '{kvp.Key}' -> '{kvp.Value}'");
                        }
                    }
                    break;

                case "reload":
                    plugin.ReloadFilteredWords();
                    plugin.ReloadConfig();
                    args.Player.SendSuccessMessage("Word filter and config reloaded from database.");
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
                        // Afficher tous les warnings
                        var allWarnings = plugin.GetAllWarnings();
                        if (allWarnings.Count == 0)
                        {
                            args.Player.SendInfoMessage("No players have warnings.");
                            return;
                        }

                        args.Player.SendInfoMessage($"=== Player Warnings ({allWarnings.Count}) ===");
                        foreach (var kvp in allWarnings.OrderByDescending(w => w.Value.WarningCount))
                        {
                            if (kvp.Value.WarningCount > 0)
                            {
                                var timeSince = DateTime.Now - kvp.Value.LastWarningTime;
                                args.Player.SendInfoMessage($"{kvp.Value.PlayerName}: {kvp.Value.WarningCount} warnings (Last: {timeSince.TotalMinutes:F1}m ago)");
                            }
                        }
                    }
                    else
                    {
                        // Afficher les warnings d'un joueur spécifique
                        var targetPlayers = TSPlayer.FindByNameOrID(args.Parameters[1]);
                        if (targetPlayers.Count == 0)
                        {
                            args.Player.SendErrorMessage("Player not found.");
                            return;
                        }

                        var target = targetPlayers[0];
                        var warning = plugin.GetPlayerWarning(target.UUID);
                        
                        if (warning.WarningCount == 0)
                        {
                            args.Player.SendInfoMessage($"{target.Name} has no warnings.");
                        }
                        else
                        {
                            var timeSince = DateTime.Now - warning.LastWarningTime;
                            args.Player.SendInfoMessage($"{target.Name}: {warning.WarningCount} warnings");
                            args.Player.SendInfoMessage($"Last warning: {timeSince.TotalMinutes:F1} minutes ago");
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
                    if (resetTargets.Count == 0)
                    {
                        args.Player.SendErrorMessage("Player not found.");
                        return;
                    }

                    var resetTarget = resetTargets[0];
                    plugin.ResetPlayerWarnings(resetTarget.UUID);
                    args.Player.SendSuccessMessage($"Warnings reset for {resetTarget.Name}");
                    TShockAPI.TShock.Log.ConsoleInfo($"[WordFilter] {args.Player.Name} reset warnings for {resetTarget.Name}");
                    break;

                default:
                    args.Player.SendErrorMessage("Unknown subcommand. Use: add, remove, list, reload, config, warnings, or resetwarnings");
                    break;
            }
        }
    }
}

