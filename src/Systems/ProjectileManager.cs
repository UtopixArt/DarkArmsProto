using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components;
using DarkArmsProto.Components.Behaviors;
using DarkArmsProto.Core;

namespace DarkArmsProto.Systems
{
    /// <summary>
    /// System for managing all projectiles in the game.
    /// Replaces manual projectile list management with automatic tracking via GameWorld.
    /// </summary>
    public class ProjectileManager
    {
        private List<ColliderComponent> currentWalls = new();
        private List<GameObject> currentEnemies = new();
        private List<GameObject> currentPlayerProjectiles = new();
        private List<GameObject> currentEnemyProjectiles = new();

        /// <summary>
        /// Update all projectiles (they update themselves, we just provide dependencies)
        /// </summary>
        public void Update(float deltaTime)
        {
            UpdateProjectileList(currentPlayerProjectiles, deltaTime);
            UpdateProjectileList(currentEnemyProjectiles, deltaTime);
        }

        //UpdateProjectileList(playerProjectiles, deltaTime);

        // Update enemy projectiles
        //UpdateProjectileList(enemyProjectiles, deltaTime);

        private void UpdateProjectileList(List<GameObject> projectiles, float deltaTime)
        {
            var enemies = GameWorld.Instance.GetAllEnemies();

            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var proj = projectiles[i];

                // Skip inactive projectiles
                if (!proj.IsActive)
                {
                    // Remove from our tracking list
                    projectiles.RemoveAt(i);
                    continue;
                }

                var projComp = proj.GetComponent<ProjectileComponent>();
                if (projComp == null)
                    continue;

                // Update homing behavior dependencies (called BEFORE GameWorld.Update)
                foreach (var behavior in projComp.GetBehaviors())
                {
                    if (behavior is HomingBehavior homing)
                    {
                        homing.SetEnemies(enemies);
                    }
                }

                // DON'T call proj.Update() here - GameWorld handles that automatically
                // We only update dependencies like enemy lists for homing behavior
            }
        }

        /// <summary>
        /// Render all projectiles
        /// </summary>
        public void Render()
        {
            foreach (var proj in currentPlayerProjectiles)
            {
                proj.Render();
            }

            foreach (var proj in currentEnemyProjectiles)
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
            var obj = GameWorld.Instance.Register(projectile, tag);
            obj.GetComponent<ProjectileComponent>().WallColliders = currentWalls;

            if (isEnemyProjectile)
            {
                currentEnemyProjectiles.Add(obj);
            }
            else
            {
                currentPlayerProjectiles.Add(obj);
            }
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
    }
}
