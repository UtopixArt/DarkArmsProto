using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Audio;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.VFX;
using DarkArmsProto.World; // Added
using Raylib_cs;

namespace DarkArmsProto.Systems
{
    public struct DamageNumber
    {
        public Vector3 Position;
        public float Damage;
        public float Lifetime;
    }

    public class CombatSystem
    {
        private GameObject player;
        private SoulManager soulManager;
        private ParticleManager particleManager;
        private LightManager lightManager;
        private RoomManager roomManager; // Added reference
        private List<DamageNumber> damageNumbers;
        private int kills;

        public int Kills => kills;
        public List<DamageNumber> DamageNumbers => damageNumbers;

        public CombatSystem(
            GameObject player,
            SoulManager soulManager,
            ParticleManager particleManager,
            LightManager lightManager,
            RoomManager roomManager // Added parameter
        )
        {
            this.player = player;
            this.soulManager = soulManager;
            this.particleManager = particleManager;
            this.lightManager = lightManager;
            this.roomManager = roomManager;
            this.damageNumbers = new List<DamageNumber>();
            this.kills = 0;
        }

        /// <summary>
        /// Check projectile collisions with enemies, apply damage, handle deaths
        /// </summary>
        public void ProcessProjectileCollisions(
            List<GameObject> projectiles,
            List<GameObject> enemies,
            float deltaTime
        )
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var proj = projectiles[i];
                var projComp = proj.GetComponent<ProjectileComponent>();

                if (projComp == null || !proj.IsActive)
                {
                    continue;
                }

                bool hit = false;

                if (projComp.IsEnemyProjectile)
                {
                    // Check collision with player
                    var playerCollider = player.GetComponent<ColliderComponent>();
                    bool collision = false;

                    if (playerCollider != null)
                    {
                        collision = playerCollider.CheckPointCollision(proj.Position);
                    }
                    else
                    {
                        collision = Vector3.Distance(proj.Position, player.Position) < 1.0f;
                    }

                    if (collision)
                    {
                        var health = player.GetComponent<HealthComponent>();
                        if (health != null)
                        {
                            health.TakeDamage(projComp.Damage);
                        }

                        // Impact particles
                        var projMesh = proj.GetComponent<MeshRendererComponent>();
                        Color impactColor = projMesh != null ? projMesh.Color : Color.Red;
                        particleManager.SpawnImpact(proj.Position, impactColor, 5);
                        lightManager.AddImpactLight(proj.Position, impactColor);

                        hit = true;
                    }
                }
                else
                {
                    for (int j = enemies.Count - 1; j >= 0; j--)
                    {
                        var enemy = enemies[j];
                        var enemyCollider = enemy.GetComponent<ColliderComponent>();

                        // Check collision using box collider if available, otherwise fallback to distance
                        bool collision = false;
                        if (enemyCollider != null)
                        {
                            collision = enemyCollider.CheckPointCollision(proj.Position);
                        }
                        else
                        {
                            collision = Vector3.Distance(proj.Position, enemy.Position) < 1.0f;
                        }

                        if (collision)
                        {
                            var health = enemy.GetComponent<HealthComponent>();
                            if (health != null)
                            {
                                health.TakeDamage(projComp.Damage);

                                // Play hit sound
                                AudioManager.Instance.PlaySound(SoundType.Hit, 0.2f);

                                // Impact particles
                                var projMesh = proj.GetComponent<MeshRendererComponent>();
                                Color impactColor = projMesh != null ? projMesh.Color : Color.White;
                                particleManager.SpawnImpact(proj.Position, impactColor, 10); // Increased from 6 to 10

                                // Impact light
                                lightManager.AddImpactLight(proj.Position, impactColor);

                                // Add damage number
                                damageNumbers.Add(
                                    new DamageNumber
                                    {
                                        Position = enemy.Position,
                                        Damage = projComp.Damage,
                                        Lifetime = 1f,
                                    }
                                );

                                // Check if enemy died
                                if (health.IsDead)
                                {
                                    HandleEnemyDeath(enemy, enemies, j);
                                }
                            }

                            // Handle behaviors (Explosion, Lifesteal, Piercing check)
                            bool shouldDestroy = projComp.OnHit(enemy, proj.Position);

                            if (shouldDestroy)
                            {
                                hit = true;
                                break;
                            }
                        }
                    }
                }

                // Remove projectile if hit
                if (hit)
                {
                    projectiles.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Check enemy collisions with player, apply touch damage
        /// </summary>
        public void ProcessEnemyPlayerCollisions(List<GameObject> enemies, float deltaTime)
        {
            var playerCollider = player.GetComponent<ColliderComponent>();

            foreach (var enemy in enemies)
            {
                bool collision = false;

                // Use box colliders if both have them
                var enemyCollider = enemy.GetComponent<ColliderComponent>();
                if (playerCollider != null && enemyCollider != null)
                {
                    collision = playerCollider.CheckCollision(enemyCollider);
                }
                else
                {
                    // Fallback to distance check
                    collision =
                        Vector3.Distance(enemy.Position, player.Position)
                        < GameConfig.EnemyCollisionRadius;
                }

                if (collision)
                {
                    var playerHealth = player.GetComponent<HealthComponent>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(GameConfig.EnemyTouchDamagePerSecond * deltaTime);
                    }
                }
            }
        }

        /// <summary>
        /// Update damage numbers (movement and lifetime)
        /// </summary>
        public void UpdateDamageNumbers(float deltaTime)
        {
            for (int i = damageNumbers.Count - 1; i >= 0; i--)
            {
                var dn = damageNumbers[i];
                dn.Lifetime -= deltaTime;
                dn.Position += new Vector3(0, deltaTime * 2, 0);
                damageNumbers[i] = dn;

                if (dn.Lifetime <= 0)
                {
                    damageNumbers.RemoveAt(i);
                }
            }
        }

        private void HandleEnemyDeath(GameObject enemy, List<GameObject> enemies, int enemyIndex)
        {
            var enemyComp = enemy.GetComponent<EnemyComponent>();
            SoulType soulType = enemyComp != null ? enemyComp.Type : SoulType.Undead;

            // Play kill sound
            AudioManager.Instance.PlaySound(SoundType.Kill, 0.4f);

            // Death explosion particles
            var enemyMesh = enemy.GetComponent<MeshRendererComponent>();
            Color enemyColor = enemyMesh != null ? enemyMesh.Color : Color.Red;
            particleManager.SpawnExplosion(enemy.Position, enemyColor, 40); // Increased from 25 to 40

            // Explosion light
            lightManager.AddExplosionLight(enemy.Position, enemyColor);

            // Screen shake on kill
            var screenShake = player.GetComponent<ScreenShakeComponent>();
            if (screenShake != null)
            {
                screenShake.AddTrauma(GameConfig.ScreenShakeOnKill);
            }

            // Spawn soul
            soulManager.SpawnSoul(enemy.Position, soulType);

            // Remove enemy and increment kill counter
            enemies.RemoveAt(enemyIndex);
            kills++;
        }

        public void TriggerExplosion(Vector3 position, float radius, float damage)
        {
            // Visuals
            particleManager.SpawnExplosion(position, Color.Orange, 50);
            lightManager.AddExplosionLight(position, Color.Orange);
            AudioManager.Instance.PlaySound(SoundType.Explosion, 0.5f);

            // Screen shake
            var screenShake = player.GetComponent<ScreenShakeComponent>();
            if (screenShake != null)
            {
                screenShake.AddTrauma(0.5f);
            }

            // Damage enemies
            var enemies = roomManager.GetCurrentRoomEnemies();
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                float dist = Vector3.Distance(position, enemy.Position);

                if (dist <= radius)
                {
                    var health = enemy.GetComponent<HealthComponent>();
                    if (health != null)
                    {
                        // Falloff damage? No, full damage for now.
                        health.TakeDamage(damage);

                        // Add damage number
                        damageNumbers.Add(
                            new DamageNumber
                            {
                                Position = enemy.Position + new Vector3(0, 2, 0),
                                Damage = damage,
                                Lifetime = 1f,
                            }
                        );

                        if (health.IsDead)
                        {
                            HandleEnemyDeath(enemy, enemies, i);
                        }
                    }
                }
            }
        }
    }
}
