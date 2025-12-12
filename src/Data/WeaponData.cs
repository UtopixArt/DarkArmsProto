using System.Collections.Generic;
using Raylib_cs;

namespace DarkArmsProto.Data
{
    public class ProjectileData
    {
        public int Count { get; set; } = 1;
        public float Speed { get; set; } = 15f;
        public float Size { get; set; } = 0.3f;
        public int[] Color { get; set; } = { 255, 0, 255, 255 };
        public bool Piercing { get; set; } = false;
        public bool Lifesteal { get; set; } = false;
        public bool Homing { get; set; } = false;
        public bool Explosive { get; set; } = false;
        public float ExplosionRadius { get; set; } = 0f;
        public float Spread { get; set; } = 0f;
        public float DamagePerProjectile { get; set; } = 1.0f;
        public bool IsLaser { get; set; } = false;
        public float LaserRange { get; set; } = 100f;
        public float LaserThickness { get; set; } = 0.05f;
        public int LaserBounces { get; set; } = 0; // Number of wall bounces (0 = no bounce)

        public Raylib_cs.Color GetColor()
        {
            return new Raylib_cs.Color(
                (byte)Color[0],
                (byte)Color[1],
                (byte)Color[2],
                (byte)Color[3]
            );
        }
    }

    public class WeaponData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Stage { get; set; } = 1;
        public string SoulType { get; set; } = ""; // "Beast", "Undead", "Demon"
        public float DamageMultiplier { get; set; } = 1.0f;
        public float FireRateMultiplier { get; set; } = 1.0f;
        public List<ProjectileData> Projectiles { get; set; } = new List<ProjectileData>();
    }

    public class WeaponsConfig
    {
        public List<WeaponData> Weapons { get; set; } = new List<WeaponData>();
    }
}
