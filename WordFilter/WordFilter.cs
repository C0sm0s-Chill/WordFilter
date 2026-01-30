using System.Text.RegularExpressions;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using TShockAPI.DB;
using MySql.Data.MySqlClient;

namespace WordFilter
{
    [ApiVersion(2, 1)]
    public class WordFilter : TerrariaPlugin
    {
        public override string Name => "WordFilter";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "C0sm0s";
        public override string Description => "Filters and replaces words in chat messages";

        private readonly Dictionary<string, string> _filteredWords = new();
        private readonly Dictionary<string, Regex> _compiledRegex = new();
        private Config _config = new();
        private readonly string _configPath = Path.Combine(TShockAPI.TShock.SavePath, "WordFilterConfig.json");
        private WarningManager _warningManager = null!;
        private System.Timers.Timer? _warningTimer;

        public WordFilter(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.ServerChat.Register(this, OnChat);
            Commands.Initialize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _warningTimer?.Stop();
                _warningTimer?.Dispose();
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
            }
            base.Dispose(disposing);
        }

        private void OnInitialize(EventArgs args)
        {
            _config = Config.Read(_configPath);
            TShock.Log.ConsoleInfo($"[WordFilter] ChatAboveHead: {_config.ChatAboveHead}");
            TShock.Log.ConsoleInfo($"[WordFilter] Warnings System: {_config.EnableWarnings}");

            try
            {
                TShock.DB.Query(
                    "CREATE TABLE IF NOT EXISTS WordFilter (" +
                    "Word VARCHAR(255) PRIMARY KEY, " +
                    "Replacement VARCHAR(255)" +
                    ")");

                TShock.DB.Query(
                    "CREATE TABLE IF NOT EXISTS WordFilterWarnings (" +
                    "UUID VARCHAR(128) PRIMARY KEY, " +
                    "PlayerName VARCHAR(255), " +
                    "WarningCount INT NOT NULL DEFAULT 0, " +
                    "LastWarningTime DATETIME" +
                    ")");
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[WordFilter] Error creating table: {ex.Message}");
            }

            _warningManager = new WarningManager();

            LoadFilteredWords();

            if (_config.EnableWarnings && _config.ResetWarningsAfterMinutes > 0)
            {
                _warningTimer = new System.Timers.Timer(300000);
                _warningTimer.Elapsed += (s, e) => _warningManager.CheckAndResetExpiredWarnings(_config.ResetWarningsAfterMinutes);
                _warningTimer.Start();
            }
        }

        private void LoadFilteredWords()
        {
            _filteredWords.Clear();

            using (var reader = TShock.DB.QueryReader("SELECT * FROM WordFilter"))
            {
                while (reader.Read())
                {
                    string word = reader.Get<string>("Word");
                    string replacement = reader.Get<string>("Replacement");
                    _filteredWords[word.ToLower()] = replacement;
                }
            }

            TShock.Log.ConsoleInfo($"[WordFilter] Loaded {_filteredWords.Count} filtered words");
        }

        private void OnChat(ServerChatEventArgs args)
        {
            if (args.Handled)
                return;

            var player = TShock.Players[args.Who];
            if (player == null || !player.Active)
                return;

            string originalText = args.Text;

            bool isCommand = originalText.StartsWith(TShockAPI.Commands.Specifier) || 
                            originalText.StartsWith(TShockAPI.Commands.SilentSpecifier);

            string filteredText = FilterMessage(originalText);

            if (filteredText != originalText)
            {
                args.Handled = true;

                bool hasImmunity = _config.ImmunityGroups.Contains(player.Group.Name);

                if (_config.EnableWarnings && !hasImmunity)
                {
                    HandleWarning(player);
                }

                string formattedMessage = string.Format(
                    TShock.Config.Settings.ChatFormat,
                    player.Group.Name,
                    player.Group.Prefix,
                    player.Name,
                    player.Group.Suffix,
                    filteredText
                );

                TSPlayer.All.SendMessage(formattedMessage, player.Group.R, player.Group.G, player.Group.B);
                
                if (_config.ChatAboveHead && !isCommand)
                {
                    ShowChatAboveHead(args.Who, filteredText);
                }
            }
            else
            {
                if (_config.ChatAboveHead && !isCommand)
                {
                    ShowChatAboveHead(args.Who, originalText);
                }
            }
        }

        private void HandleWarning(TSPlayer player)
        {
            string uuid = player.UUID;
            var warning = _warningManager.GetOrCreateWarning(uuid, player.Name);

            if (warning.ShouldResetWarnings(_config.ResetWarningsAfterMinutes))
            {
                warning.ResetWarnings();
            }

            _warningManager.AddWarning(uuid, player.Name);
            int currentWarnings = warning.WarningCount;

            TShock.Log.ConsoleInfo($"[WordFilter] Player {player.Name} ({uuid}) now has {currentWarnings} warnings.");

            if (_config.WarningsBeforeBan > 0 && currentWarnings == _config.WarningsBeforeBan)
            {
                string banReason = _config.BanMessage
                    .Replace("{warnings}", currentWarnings.ToString())
                    .Replace("{duration}", _config.BanDurationMinutes.ToString());

                TShockAPI.Commands.HandleCommand(
                    TSPlayer.Server,
                    $"/ban add \"{player.Name}\" {_config.BanDurationMinutes}m {banReason}"
                );
                
                _warningManager.ResetWarnings(uuid);
                
                TShock.Log.ConsoleInfo($"[WordFilter] Player {player.Name} BANNED for {_config.BanDurationMinutes} minutes after {currentWarnings} warnings.");
                return;
            }

            if (_config.WarningsBeforeKick > 0 && currentWarnings == _config.WarningsBeforeKick)
            {
                int warningsUntilBan = _config.WarningsBeforeBan - currentWarnings;
                
                string kickReason = _config.KickMessage
                    .Replace("{warnings}", currentWarnings.ToString())
                    .Replace("{remaining}", warningsUntilBan.ToString())
                    .Replace("{duration}", _config.BanDurationMinutes.ToString());

                player.Disconnect(kickReason);
                
                TShock.Log.ConsoleInfo($"[WordFilter] Player {player.Name} KICKED after {currentWarnings} warnings. {warningsUntilBan} warnings until ban.");
                return;
            }

            string warningMessage;
            if (_config.WarningsBeforeKick > 0 && currentWarnings < _config.WarningsBeforeKick)
            {
                warningMessage = _config.WarningMessage
                    .Replace("{current}", currentWarnings.ToString())
                    .Replace("{max}", _config.WarningsBeforeKick.ToString())
                    .Replace("{duration}", _config.BanDurationMinutes.ToString());
            }
            else
            {
                int warningsUntilBan = _config.WarningsBeforeBan - currentWarnings;
                int warningsSinceKick = currentWarnings - _config.WarningsBeforeKick;
                int totalWarningsUntilBan = _config.WarningsBeforeBan - _config.WarningsBeforeKick;
                
                warningMessage = _config.WarningAfterKickMessage
                    .Replace("{current}", warningsSinceKick.ToString())
                    .Replace("{max}", totalWarningsUntilBan.ToString())
                    .Replace("{duration}", _config.BanDurationMinutes.ToString());
            }

            player.SendErrorMessage(warningMessage);
            
            TShock.Log.ConsoleInfo($"[WordFilter] Player {player.Name} warned ({currentWarnings} total).");
        }

        private void ShowChatAboveHead(int playerIndex, string message)
        {
            var tPlayer = TShock.Players[playerIndex];
            if (tPlayer == null || !tPlayer.Active)
                return;

            if (tPlayer.mute)
                return;

            if (!_config.ShowForDeadPlayers && tPlayer.Dead)
                return;

            if (_config.ExcludedGroups.Contains(tPlayer.Group.Name))
                return;

            if (message.Length > _config.MaxMessageLength)
            {
                message = message.Substring(0, _config.MaxMessageLength) + "...";
            }

            var color = new Microsoft.Xna.Framework.Color(tPlayer.Group.R, tPlayer.Group.G, tPlayer.Group.B);
            
            NetMessage.SendData(
                119, 
                -1, 
                -1, 
                Terraria.Localization.NetworkText.FromLiteral(message),
                (int)color.PackedValue,
                tPlayer.X + 8f,
                tPlayer.Y + 32f
            );
        }

        private string FilterMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || _filteredWords.Count == 0)
                return message;

            string result = message;

            foreach (var kvp in _filteredWords)
            {
                string word = kvp.Key;
                string replacement = kvp.Value;

                if (!_compiledRegex.TryGetValue(word, out var regex))
                {
                    string pattern = $@"\b{Regex.Escape(word)}\b";
                    regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    _compiledRegex[word] = regex;
                }

                result = regex.Replace(result, replacement);
            }

            return result;
        }

        public void AddFilteredWord(string word, string replacement)
        {
            TShock.DB.Query("REPLACE INTO WordFilter (Word, Replacement) VALUES (@0, @1)",
                word.ToLower(), replacement);
            _filteredWords[word.ToLower()] = replacement;
            _compiledRegex.Remove(word.ToLower());
        }

        public void RemoveFilteredWord(string word)
        {
            TShock.DB.Query("DELETE FROM WordFilter WHERE Word = @0", word.ToLower());
            _filteredWords.Remove(word.ToLower());
            _compiledRegex.Remove(word.ToLower());
        }

        public Dictionary<string, string> GetFilteredWords()
        {
            return new Dictionary<string, string>(_filteredWords);
        }

        public void ReloadFilteredWords()
        {
            LoadFilteredWords();
        }

        public void ReloadConfig()
        {
            _config = Config.Read(_configPath);
            TShock.Log.ConsoleInfo($"[WordFilter] Config reloaded - ChatAboveHead: {_config.ChatAboveHead}");
        }

        public Config GetConfig()
        {
            return _config;
        }

        public PlayerWarning GetPlayerWarning(string uuid)
        {
            return _warningManager.GetOrCreateWarning(uuid, "");
        }

        public void ResetPlayerWarnings(string uuid)
        {
            _warningManager.ResetWarnings(uuid);
        }

        public Dictionary<string, PlayerWarning> GetAllWarnings()
        {
            return _warningManager.GetAllWarnings();
        }
    }
}
