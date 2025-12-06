using System;
using System.Numerics;
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
        private static Random random = new Random();

        /// <summary>
        /// Create a single projectile from projectile data
        /// </summary>
        public static GameObject CreateProjectile(
            Vector3 position,
            Vector3 direction,
            float baseDamage,
            ProjectileData data,
            bool isEnemyProjectile = false
        )
        {
            var go = new GameObject(position);

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

            // Projectile component
            var projComp = new ProjectileComponent
            {
                Velocity = finalDir * data.Speed,
                Damage = baseDamage * data.DamagePerProjectile,
                Piercing = data.Piercing,
                Lifesteal = data.Lifesteal,
                Homing = data.Homing,
                Explosive = data.Explosive,
                ExplosionRadius = data.ExplosionRadius,
                IsEnemyProjectile = isEnemyProjectile,
            };
            go.AddComponent(projComp);

            // Mesh renderer
            var meshComp = new MeshRendererComponent
            {
                MeshType = MeshType.Sphere,
                Color = data.GetColor(),
                Scale = new Vector3(data.Size),
            };
            go.AddComponent(meshComp);

            // Collider
            var colComp = new ColliderComponent
            {
                Size = new Vector3(data.Size, data.Size, data.Size),
                IsTrigger = true,
                ShowDebug = false,
            };
            go.AddComponent(colComp);

            return go;
        }

        /// <summary>
        /// Create all projectiles for a weapon shot
        /// </summary>
        public static System.Collections.Generic.List<GameObject> CreateProjectiles(
            Vector3 position,
            Vector3 direction,
            float baseDamage,
            WeaponData weaponData
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
                        baseDamage * weaponData.DamageMultiplier,
                        projData,
                        false
                    );

                    projectiles.Add(projectile);
                }
            }

            return projectiles;
        }

        /// <summary>
        /// Create an enemy projectile
        /// </summary>
        public static GameObject CreateEnemyProjectile(
            Vector3 position,
            Vector3 direction,
            float damage,
            SoulType soulType
        )
        {
            // Enemy projectile data (could be moved to JSON too)
            var data = new ProjectileData
            {
                Speed = soulType == SoulType.Undead ? 12f : 20f,
                Size = soulType == SoulType.Undead ? 0.3f : 0.4f,
                Color = soulType switch
                {
                    SoulType.Undead => new int[] { 0, 255, 0, 255 },
                    _ => new int[] { 255, 0, 0, 255 },
                },
            };

            return CreateProjectile(position, direction, damage, data, isEnemyProjectile: true);
        }
    }
}
