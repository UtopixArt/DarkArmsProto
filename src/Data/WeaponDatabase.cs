using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DarkArmsProto.Data
{
    /// <summary>
    /// Centralized weapon database loaded from JSON.
    /// Provides access to all weapon configurations.
    /// </summary>
    public static class WeaponDatabase
    {
        private static Dictionary<string, WeaponData> weaponsByName =
            new Dictionary<string, WeaponData>();
        private static Dictionary<int, Dictionary<Core.SoulType, WeaponData>> weaponsByStage =
            new Dictionary<int, Dictionary<Core.SoulType, WeaponData>>();
        private static bool isLoaded = false;

        public static void Load()
        {
            if (isLoaded)
                return;

            string path = Path.Combine("resources", "data", "weapons.json");

            if (!File.Exists(path))
            {
                Console.WriteLine($"[WeaponDatabase] ERROR: {path} not found!");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);

                // Configure JSON options to be case-insensitive and use snake_case
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                };

                var config = JsonSerializer.Deserialize<WeaponsConfig>(json, options);

                if (config == null || config.Weapons == null)
                {
                    Console.WriteLine("[WeaponDatabase] ERROR: Failed to parse weapons.json");
                    return;
                }

                // Build lookup dictionaries
                foreach (var weapon in config.Weapons)
                {
                    // By name (lowercase for case-insensitive lookup)
                    string key = weapon.Name.ToLower().Replace(" ", "_");
                    weaponsByName[key] = weapon;

                    // By stage + soul type (for evolution)
                    if (!string.IsNullOrEmpty(weapon.SoulType))
                    {
                        Core.SoulType soulType = weapon.SoulType switch
                        {
                            "Beast" => Core.SoulType.Beast,
                            "Undead" => Core.SoulType.Undead,
                            "Demon" => Core.SoulType.Demon,
                            _ => Core.SoulType.Undead,
                        };

                        if (!weaponsByStage.ContainsKey(weapon.Stage))
                        {
                            weaponsByStage[weapon.Stage] =
                                new Dictionary<Core.SoulType, WeaponData>();
                        }

                        weaponsByStage[weapon.Stage][soulType] = weapon;
                    }
                }

                isLoaded = true;
                // Console.WriteLine(
                //     $"[WeaponDatabase] Loaded {config.Weapons.Count} weapons from {path}"
                // );
            }
            catch (Exception e)
            {
                Console.WriteLine($"[WeaponDatabase] ERROR: {e.Message}");
            }
        }

        /// <summary>
        /// Get weapon data by name (case-insensitive)
        /// </summary>
        public static WeaponData? GetByName(string name)
        {
            if (!isLoaded)
                Load();

            string key = name.ToLower().Replace(" ", "_");
            return weaponsByName.ContainsKey(key) ? weaponsByName[key] : null;
        }

        /// <summary>
        /// Get weapon for evolution (by stage + dominant soul type)
        /// </summary>
        public static WeaponData? GetForEvolution(int targetStage, Core.SoulType soulType)
        {
            if (!isLoaded)
                Load();

            if (
                weaponsByStage.ContainsKey(targetStage)
                && weaponsByStage[targetStage].ContainsKey(soulType)
            )
            {
                return weaponsByStage[targetStage][soulType];
            }

            return null;
        }

        /// <summary>
        /// Get starting weapon (stage 1)
        /// </summary>
        public static WeaponData GetStartingWeapon()
        {
            if (!isLoaded)
                Load();

            return GetByName("plasma_laser")!;
        }
    }
}
