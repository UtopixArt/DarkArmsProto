using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DarkArmsProto.Data
{
    /// <summary>
    /// Centralized enemy database loaded from JSON.
    /// Provides access to all enemy type configurations.
    /// </summary>
    public static class EnemyDatabase
    {
        private static Dictionary<Core.SoulType, EnemyData> enemies =
            new Dictionary<Core.SoulType, EnemyData>();
        private static bool isLoaded = false;

        public static void Load()
        {
            if (isLoaded)
                return;

            string path = Path.Combine("resources", "data", "enemies.json");

            if (!File.Exists(path))
            {
                Console.WriteLine($"[EnemyDatabase] ERROR: {path} not found!");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);

                // Configure JSON options to be case-insensitive
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var config = JsonSerializer.Deserialize<EnemiesConfig>(json, options);

                if (config == null || config.Enemies == null)
                {
                    Console.WriteLine("[EnemyDatabase] ERROR: Failed to parse enemies.json");
                    return;
                }

                // Build lookup dictionary by soul type
                foreach (var enemy in config.Enemies)
                {
                    Core.SoulType soulType = enemy.GetSoulType();
                    enemies[soulType] = enemy;
                }

                isLoaded = true;
                Console.WriteLine(
                    $"[EnemyDatabase] Loaded {config.Enemies.Count} enemy types from {path}"
                );
            }
            catch (Exception e)
            {
                Console.WriteLine($"[EnemyDatabase] ERROR: {e.Message}");
            }
        }

        /// <summary>
        /// Get enemy data by soul type
        /// </summary>
        public static EnemyData? Get(Core.SoulType soulType)
        {
            if (!isLoaded)
                Load();

            return enemies.ContainsKey(soulType) ? enemies[soulType] : null;
        }

        /// <summary>
        /// Get all available enemy types
        /// </summary>
        public static Core.SoulType[] GetAllTypes()
        {
            if (!isLoaded)
                Load();

            var types = new Core.SoulType[enemies.Count];
            enemies.Keys.CopyTo(types, 0);
            return types;
        }
    }
}
