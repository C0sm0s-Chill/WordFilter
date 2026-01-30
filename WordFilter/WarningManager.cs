using Newtonsoft.Json;
using TShockAPI;
using TShockAPI.DB;

namespace WordFilter
{
    public class PlayerWarning
    {
        public string PlayerName { get; set; } = string.Empty;
        public string UUID { get; set; } = string.Empty;
        public int WarningCount { get; set; } = 0;
        public DateTime LastWarningTime { get; set; } = DateTime.MinValue;

        public PlayerWarning() { }

        public PlayerWarning(string playerName, string uuid)
        {
            PlayerName = playerName;
            UUID = uuid;
            WarningCount = 0;
            LastWarningTime = DateTime.MinValue;
        }

        public bool ShouldResetWarnings(int resetAfterMinutes)
        {
            if (resetAfterMinutes <= 0)
                return false;

            return (DateTime.Now - LastWarningTime).TotalMinutes >= resetAfterMinutes;
        }

        public void AddWarning()
        {
            WarningCount++;
            LastWarningTime = DateTime.Now;
        }

        public void ResetWarnings()
        {
            WarningCount = 0;
            LastWarningTime = DateTime.MinValue;
        }
    }

    public class WarningManager
    {
        private readonly Dictionary<string, PlayerWarning> _warnings = new();
        private readonly object _lock = new();

        public WarningManager()
        {
            // Nothing here for now
        }

        public PlayerWarning GetOrCreateWarning(string uuid, string playerName)
        {
            lock (_lock)
            {
                if (!_warnings.ContainsKey(uuid))
                {
                    LoadPlayerWarning(uuid, playerName);
                }
                else
                {
                    _warnings[uuid].PlayerName = playerName;
                }

                return _warnings[uuid];
            }
        }

        public void AddWarning(string uuid, string playerName)
        {
            var warning = GetOrCreateWarning(uuid, playerName);
            warning.AddWarning();
            SaveToDatabase(warning);
        }

        public void ResetWarnings(string uuid)
        {
            lock (_lock)
            {
                if (_warnings.ContainsKey(uuid))
                {
                    _warnings[uuid].ResetWarnings();
                    SaveToDatabase(_warnings[uuid]);
                }
            }
        }

        public void CheckAndResetExpiredWarnings(int resetAfterMinutes)
        {
            lock (_lock)
            {
                LoadFromDatabase();

                bool changed = false;
                foreach (var warning in _warnings.Values.ToList())
                {
                    if (warning.ShouldResetWarnings(resetAfterMinutes))
                    {
                        warning.ResetWarnings();
                        SaveToDatabase(warning);
                        changed = true;
                    }
                }

                if (changed)
                {
                    TShock.Log.ConsoleInfo($"[WordFilter] Expired warnings reset automatically.");
                }
            }
        }

        public Dictionary<string, PlayerWarning> GetAllWarnings()
        {
            lock (_lock)
            {
                LoadFromDatabase();
                return new Dictionary<string, PlayerWarning>(_warnings);
            }
        }

        private void LoadPlayerWarning(string uuid, string playerName)
        {
            try
            {
                if (TShock.DB == null)
                {
                    _warnings[uuid] = new PlayerWarning(playerName, uuid);
                    return;
                }

                using (var reader = TShock.DB.QueryReader("SELECT * FROM WordFilterWarnings WHERE UUID = @0", uuid))
                {
                    if (reader.Read())
                    {
                        var warning = new PlayerWarning
                        {
                            UUID = reader.Get<string>("UUID"),
                            PlayerName = reader.Get<string>("PlayerName"),
                            WarningCount = reader.Get<int>("WarningCount"),
                            LastWarningTime = reader.Get<DateTime>("LastWarningTime")
                        };
                        _warnings[uuid] = warning;
                    }
                    else
                    {
                        _warnings[uuid] = new PlayerWarning(playerName, uuid);
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[WordFilter] Error loading player warning: {ex.Message}");
                _warnings[uuid] = new PlayerWarning(playerName, uuid);
            }
        }

        private void LoadFromDatabase()
        {
            try
            {
                if (TShock.DB == null)
                {
                    TShock.Log.ConsoleError("[WordFilter] TShock.DB is null, cannot load warnings.");
                    return;
                }

                _warnings.Clear();
                using (var reader = TShock.DB.QueryReader("SELECT * FROM WordFilterWarnings"))
                {
                    while (reader.Read())
                    {
                        var warning = new PlayerWarning
                        {
                            UUID = reader.Get<string>("UUID"),
                            PlayerName = reader.Get<string>("PlayerName"),
                            WarningCount = reader.Get<int>("WarningCount"),
                            LastWarningTime = reader.Get<DateTime>("LastWarningTime")
                        };
                        _warnings[warning.UUID] = warning;
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[WordFilter] Error loading warnings from database: {ex.Message}");
            }
        }

        public void SaveToDatabase(PlayerWarning warning)
        {
            try
            {
                if (TShock.DB == null)
                {
                    TShock.Log.ConsoleError("[WordFilter] TShock.DB is null, cannot save warning.");
                    return;
                }

                TShock.DB.Query(
                    "REPLACE INTO WordFilterWarnings (UUID, PlayerName, WarningCount, LastWarningTime) VALUES (@0, @1, @2, @3)",
                    warning.UUID,
                    warning.PlayerName,
                    warning.WarningCount,
                    warning.LastWarningTime
                );
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[WordFilter] Error saving warning to database: {ex.Message}");
            }
        }
    }
}

