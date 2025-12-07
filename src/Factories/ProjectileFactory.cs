using System;
using System.Numerics;
using DarkArmsProto.Builders;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.Data;

namespace DarkArmsProto.Factories
{
    /// <summary>
    /// Factory for creating projectiles from weapon data.
    /// Centralizes all projectile creation logic.
    /// </summary>
    public static class ProjectileFactory
    {
        private static readonly Random random = new();

        /// <summary>
        /// Create a single projectile from projectile data
        /// </summary>
        public static GameObject CreateProjectile(
            Vector3 position,
            Vector3 direction,
            float baseDamage,
            ProjectileData data,
            bool isEnemyProjectile = false,
            Action<Vector3, float, float>? explosionCallback = null,
            Action<float>? healCallback = null
        )
        {
            // Apply spread to direction
            Vector3 finalDir = direction;
            if (data.Spread > 0)
            {
                float spreadX = (float)(random.NextDouble() * 2 - 1) * data.Spread;
                float spreadY = (float)(random.NextDouble() * 2 - 1) * data.Spread;
                finalDir.X += spreadX;
                finalDir.Y += spreadY;
                finalDir = Vector3.Normalize(finalDir);
            }

            var builder = new ProjectileBuilder()
                .AtPosition(position)
                .WithDirection(finalDir, data.Speed)
                .WithDamage(baseDamage * data.DamagePerProjectile)
                .WithSize(data.Size)
                .WithColor(data.GetColor())
                .WithTrail(data.GetColor());

            if (isEnemyProjectile)
            {
                builder.AsEnemyProjectile();
            }

            // Add behaviors based on data
            if (data.Homing)
            {
                builder.WithHoming();
            }

            if (data.Piercing)
            {
                builder.WithPiercing();
            }

            if (data.Explosive)
            {
                builder.WithExplosion(data.ExplosionRadius, 1.0f, explosionCallback);
            }

            if (data.Lifesteal)
            {
                builder.WithLifesteal(0.3f, healCallback);
            }

            return builder.Build();
        }

        /// <summary>
        /// Create all projectiles for a weapon shot
        /// </summary>
        public static System.Collections.Generic.List<GameObject> CreateProjectiles(
            Vector3 position,
            Vector3 direction,
            float baseDamage,
            WeaponData weaponData,
            Action<Vector3, float, float>? explosionCallback = null,
            Action<float>? healCallback = null
        )
        {
            var projectiles = new System.Collections.Generic.List<GameObject>();

            foreach (var projData in weaponData.Projectiles)
            {
                for (int i = 0; i < projData.Count; i++)
                {
                    // Calculate spread direction for shotguns
                    Vector3 spreadDir = direction;

                    if (projData.Count > 1 && projData.Spread > 0)
                    {
                        // Distribute projectiles in a pattern
                        float spreadAngle = (i - (projData.Count - 1) / 2.0f) * projData.Spread;
                        spreadDir.X += spreadAngle;
                        spreadDir = Vector3.Normalize(spreadDir);
                    }

                    var projectile = CreateProjectile(
                        position,
                        spreadDir,
                        baseDamage,
                        projData,
                        false,
                        explosionCallback,
                        healCallback
                    );

                    projectiles.Add(projectile);
                }
            }

            return projectiles;
        }

        /// <summary>
        /// Create a projectile for an enemy
        /// </summary>
        public static GameObject CreateEnemyProjectile(
            Vector3 position,
            Vector3 direction,
            float damage,
            SoulType type
        )
        {
            float speed = 20.0f;
            Raylib_cs.Color color = Raylib_cs.Color.Red;
            float size = 0.4f;

            if (type == SoulType.Undead)
            {
                speed = 12.0f; // Slower poison projectile
                color = Raylib_cs.Color.Green;
                size = 0.3f;
            }

            return new ProjectileBuilder()
                .AtPosition(position)
                .WithDirection(direction, speed)
                .WithDamage(damage)
                .WithSize(size)
                .WithColor(color)
                .WithTrail(color)
                .AsEnemyProjectile()
                .Build();
        }
    }
}
