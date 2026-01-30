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

        [JsonProperty("KickMessage")]
        public string KickMessage { get; set; } = "Kicked for inappropriate language! You have {remaining} more warnings before a {duration} minute ban.";

        [JsonProperty("BanMessage")]
        public string BanMessage { get; set; } = "Banned for {duration} minutes for repeated use of inappropriate language.";

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

