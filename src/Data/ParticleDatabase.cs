using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Raylib_cs;

namespace DarkArmsProto.Data
{
    public static class ParticleDatabase
    {
        private static Dictionary<string, ParticleEffectData> effects =
            new Dictionary<string, ParticleEffectData>();

        public static void Load()
        {
            string path = "resources/data/particles.json";
            if (!File.Exists(path))
            {
                Console.WriteLine("Particle database not found, creating default.");
                CreateDefaultDatabase(path);
                return;
            }

            try
            {
                // Console.WriteLine($"Loading particles from: {Path.GetFullPath(path)}");
                string json = File.ReadAllText(path);
                var list = JsonSerializer.Deserialize<List<ParticleEffectData>>(json);
                if (list != null)
                {
                    foreach (var effect in list)
                    {
                        effects[effect.Name] = effect;
                    }
                }
                // Console.WriteLine($"Loaded {effects.Count} particle effects.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to load particle database: {e.Message}");
            }
        }

        public static ParticleEffectData? GetEffect(string name)
        {
            if (effects.TryGetValue(name, out var effect))
            {
                return effect;
            }
            // Console.WriteLine($"Particle effect '{name}' not found.");
            return null;
        }

        public static List<string> GetEffectNames()
        {
            return new List<string>(effects.Keys);
        }

        public static void Save()
        {
            string path = "resources/data/particles.json";
            try
            {
                var list = new List<ParticleEffectData>(effects.Values);
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(list, options);
                File.WriteAllText(path, json);
                Console.WriteLine($"Saved particles to {path}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to save particles: {e.Message}");
            }
        }

        private static void CreateDefaultDatabase(string path)
        {
            var list = new List<ParticleEffectData>();

            // Default Explosion
            var explosion = new ParticleEffectData { Name = "Explosion" };
            explosion.Emitters.Add(
                new ParticleEmitterData
                {
                    Count = 60,
                    MinSpeed = 2f,
                    MaxSpeed = 6f,
                    MinLifetime = 0.5f,
                    MaxLifetime = 1.0f,
                    MinSize = 0.05f,
                    MaxSize = 0.2f,
                    Gravity = -5f,
                    Color = new SerializedColor(255, 100, 0, 255), // Orange
                    Spread = new System.Numerics.Vector3(1, 1, 1),
                }
            );
            list.Add(explosion);

            // Default Impact
            var impact = new ParticleEffectData { Name = "Impact" };
            impact.Emitters.Add(
                new ParticleEmitterData
                {
                    Count = 15,
                    MinSpeed = 1f,
                    MaxSpeed = 3f,
                    MinLifetime = 0.1f,
                    MaxLifetime = 0.3f,
                    MinSize = 0.02f,
                    MaxSize = 0.1f,
                    Gravity = -5f,
                    Color = new SerializedColor(255, 255, 255, 255),
                    Spread = new System.Numerics.Vector3(1, 0.5f, 1),
                }
            );
            list.Add(impact);

            // Save
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(list, options);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to save default particle database: {e.Message}");
            }
        }
    }
}
