using Newtonsoft.Json;

namespace WordFilter
{
    public class Config
    {
        [JsonProperty("ChatAboveHead")]
        public bool ChatAboveHead { get; set; } = true;

        [JsonProperty("ChatAboveHeadDuration")]
        public int ChatAboveHeadDuration { get; set; } = 500;

        [JsonProperty("MaxMessageLength")]
        public int MaxMessageLength { get; set; } = 80;

        [JsonProperty("ShowForDeadPlayers")]
        public bool ShowForDeadPlayers { get; set; } = true;

        [JsonProperty("ExcludedGroups")]
        public List<string> ExcludedGroups { get; set; } = new List<string>();

        [JsonProperty("ExcludedCommands")]
        public List<string> ExcludedCommands { get; set; } = new List<string> { "say", "wf" };

        // Système de sanctions
        [JsonProperty("EnableWarnings")]
        public bool EnableWarnings { get; set; } = true;

        [JsonProperty("WarningsBeforeKick")]
        public int WarningsBeforeKick { get; set; } = 3;

        [JsonProperty("WarningsBeforeBan")]
        public int WarningsBeforeBan { get; set; } = 5;

        [JsonProperty("BanDurationMinutes")]
        public int BanDurationMinutes { get; set; } = 60;

        [JsonProperty("ResetWarningsAfterMinutes")]
        public int ResetWarningsAfterMinutes { get; set; } = 30;

        // Messages configurables
        [JsonProperty("WarningMessage")]
        public string WarningMessage { get; set; } = "[WordFilter] Warning {current}/{max} before kick: Do not use inappropriate language!";

        [JsonProperty("WarningAfterKickMessage")]
        public string WarningAfterKickMessage { get; set; } = "[WordFilter] Warning {current}/{max} before BAN ({duration} minutes): Do not use inappropriate language!";

        [JsonProperty("KickMessage")]
        public string KickMessage { get; set; } = "Kicked for inappropriate language! You have {remaining} more warnings before a {duration} minute ban.";

        [JsonProperty("BanMessage")]
        public string BanMessage { get; set; } = "Banned for {duration} minutes for repeated use of inappropriate language.";

        [JsonProperty("CommandAddSuccessMessage")]
        public string CommandAddSuccessMessage { get; set; } = "Added word filter: '{word}' -> '{replacement}'";

        [JsonProperty("CommandRemoveSuccessMessage")]
        public string CommandRemoveSuccessMessage { get; set; } = "Removed word filter: '{word}'";

        [JsonProperty("CommandListNoWordsMessage")]
        public string CommandListNoWordsMessage { get; set; } = "No filtered words configured.";

        [JsonProperty("CommandListHeaderMessage")]
        public string CommandListHeaderMessage { get; set; } = "Filtered words ({count}):";

        [JsonProperty("CommandReloadSuccessMessage")]
        public string CommandReloadSuccessMessage { get; set; } = "Word filter and config reloaded from database.";

        [JsonProperty("CommandWarningsNoWarningsMessage")]
        public string CommandWarningsNoWarningsMessage { get; set; } = "No players have warnings.";

        [JsonProperty("CommandWarningsHeaderMessage")]
        public string CommandWarningsHeaderMessage { get; set; } = "=== Player Warnings ({count}) ===";

        [JsonProperty("CommandWarningInfoMessage")]
        public string CommandWarningInfoMessage { get; set; } = "{player}: {warnings} warnings (Last: {time} ago)";

        [JsonProperty("CommandPlayerNoWarningsMessage")]
        public string CommandPlayerNoWarningsMessage { get; set; } = "{player} has no warnings.";

        [JsonProperty("CommandPlayerWarningDetailsMessage")]
        public string CommandPlayerWarningDetailsMessage { get; set; } = "{player}: {warnings} warnings - Last warning: {time} ago";

        [JsonProperty("CommandResetWarningsSuccessMessage")]
        public string CommandResetWarningsSuccessMessage { get; set; } = "Warnings reset for {player}";

        [JsonProperty("CommandPlayerNotFoundMessage")]
        public string CommandPlayerNotFoundMessage { get; set; } = "Player not found.";

        [JsonProperty("TimeMinutesFormat")]
        public string TimeMinutesFormat { get; set; } = "{0:F1}m";

        [JsonProperty("TimeHoursFormat")]
        public string TimeHoursFormat { get; set; } = "{0:F1}h";

        [JsonProperty("TimeDaysFormat")]
        public string TimeDaysFormat { get; set; } = "{0:F1}d";

        [JsonProperty("ImmunityGroups")]
        public List<string> ImmunityGroups { get; set; } = new List<string> { "superadmin", "admin" };

        public static Config Read(string path)
        {
            if (!File.Exists(path))
            {
                var config = new Config();
                File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
                return config;
            }

            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path)) ?? new Config();
        }

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}

