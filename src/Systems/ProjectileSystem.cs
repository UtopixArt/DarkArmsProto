using System.Collections.Generic;
using DarkArmsProto.Components;
using DarkArmsProto.Components.Behaviors;
using DarkArmsProto.Core;

namespace DarkArmsProto.Systems
{
    /// <summary>
    /// System for managing all projectiles in the game.
    /// Replaces manual projectile list management with automatic tracking via GameWorld.
    /// </summary>
    public class ProjectileSystem
    {
        private List<ColliderComponent> currentWalls = new();

        /// <summary>
        /// Update all projectiles (they update themselves, we just provide dependencies)
        /// </summary>
        public void Update(float deltaTime)
        {
            // Get all projectiles from GameWorld
            var playerProjectiles = GameWorld.Instance.FindAllWithTag("Projectile");
            var enemyProjectiles = GameWorld.Instance.FindAllWithTag("EnemyProjectile");

            // Update player projectiles
            UpdateProjectileList(playerProjectiles, deltaTime);

            // Update enemy projectiles
            UpdateProjectileList(enemyProjectiles, deltaTime);
        }

        private void UpdateProjectileList(List<GameObject> projectiles, float deltaTime)
        {
            var enemies = GameWorld.Instance.FindAllWithTag("Enemy");

            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var proj = projectiles[i];
                if (!proj.IsActive)
                {
                    GameWorld.Instance.Unregister(proj);
                    continue;
                }

                var projComp = proj.GetComponent<ProjectileComponent>();
                if (projComp == null)
                    continue;

                // Update dependencies (walls are provided by Room)
                projComp.WallColliders = currentWalls;

                // Update homing behavior if present
                foreach (var behavior in projComp.GetBehaviors())
                {
                    if (behavior is HomingBehavior homing)
                    {
                        homing.SetEnemies(enemies);
                    }
                }

                // Update the projectile
                proj.Update(deltaTime);

                // Clean up if inactive
                if (!proj.IsActive)
                {
                    GameWorld.Instance.Unregister(proj);
                }
            }
        }

        /// <summary>
        /// Render all projectiles
        /// </summary>
        public void Render()
        {
            var playerProjectiles = GameWorld.Instance.FindAllWithTag("Projectile");
            var enemyProjectiles = GameWorld.Instance.FindAllWithTag("EnemyProjectile");

            foreach (var proj in playerProjectiles)
            {
                proj.Render();
            }

            foreach (var proj in enemyProjectiles)
            {
                proj.Render();
            }
        }

        /// <summary>
        /// Set current room walls for collision detection
        /// </summary>
        public void SetWalls(List<ColliderComponent> walls)
        {
            currentWalls = walls;
        }

        /// <summary>
        /// Spawn a projectile and register it to GameWorld
        /// </summary>
        public void SpawnProjectile(GameObject projectile, bool isEnemyProjectile = false)
        {
            string tag = isEnemyProjectile ? "EnemyProjectile" : "Projectile";
            GameWorld.Instance.Register(projectile, tag);
        }

        /// <summary>
        /// Spawn multiple projectiles
        /// </summary>
        public void SpawnProjectiles(List<GameObject> projectiles, bool isEnemyProjectile = false)
        {
            foreach (var proj in projectiles)
            {
                SpawnProjectile(proj, isEnemyProjectile);
            }
        }

        /// <summary>
        /// Get count of active projectiles (for debugging/UI)
        /// </summary>
        public int GetProjectileCount()
        {
            return GameWorld.Instance.FindAllWithTag("Projectile").Count
                + GameWorld.Instance.FindAllWithTag("EnemyProjectile").Count;
        }
    }
}
