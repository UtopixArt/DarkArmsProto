using System.Collections.Generic;
using DarkArmsProto.Components;
using DarkArmsProto.Core;

namespace DarkArmsProto.Systems
{
    /// <summary>
    /// Automatic collision detection system (like Unity's Physics system).
    /// Checks all colliders and automatically calls OnCollision on components.
    /// </summary>
    public class CollisionSystem
    {
        private List<ColliderComponent> colliders = new();
        private Dictionary<string, HashSet<string>> collisionPairs = new(); // For trigger tracking

        /// <summary>
        /// Update collision detection and notify components
        /// </summary>
        public void Update(float deltaTime)
        {
            // Get all active colliders
            colliders = GameWorld.Instance.FindComponentsOfType<ColliderComponent>();

            // Check projectiles vs enemies
            CheckCollisionGroup("Projectile", "Enemy");

            // Check enemy projectiles vs player
            CheckCollisionGroup("EnemyProjectile", "Player");

            // Check enemies vs player (for touch damage)
            CheckCollisionGroup("Enemy", "Player");

            // Check projectiles vs walls
            CheckProjectileWallCollisions();
        }

        private void CheckCollisionGroup(string tagA, string tagB)
        {
            var objectsA = GameWorld.Instance.FindAllWithTag(tagA);
            var objectsB = GameWorld.Instance.FindAllWithTag(tagB);

            foreach (var objA in objectsA)
            {
                if (!objA.IsActive)
                    continue;

                var colliderA = objA.GetComponent<ColliderComponent>();
                if (colliderA == null)
                    continue;

                foreach (var objB in objectsB)
                {
                    if (!objB.IsActive)
                        continue;

                    var colliderB = objB.GetComponent<ColliderComponent>();
                    if (colliderB == null)
                        continue;

                    // Check collision
                    bool isColliding = colliderA.CheckCollision(colliderB);

                    if (isColliding)
                    {
                        // Notify all components that implement ICollisionHandler
                        NotifyCollision(objA, objB);
                        NotifyCollision(objB, objA);
                    }
                }
            }
        }

        private void CheckProjectileWallCollisions()
        {
            var projectiles = GameWorld.Instance.FindAllWithTag("Projectile");
            var enemyProjectiles = GameWorld.Instance.FindAllWithTag("EnemyProjectile");
            var walls = GameWorld.Instance.FindAllWithTag("Wall");

            var allProjectiles = new List<GameObject>(projectiles);
            allProjectiles.AddRange(enemyProjectiles);

            foreach (var proj in allProjectiles)
            {
                var projComp = proj.GetComponent<ProjectileComponent>();
                if (projComp == null)
                    continue;

                // Check against walls
                foreach (var wall in walls)
                {
                    var wallCollider = wall.GetComponent<ColliderComponent>();
                    if (wallCollider != null && wallCollider.CheckPointCollision(proj.Position))
                    {
                        projComp.OnWallHit(proj.Position);
                        break;
                    }
                }
            }
        }

        private void NotifyCollision(GameObject obj, GameObject other)
        {
            foreach (var component in obj.GetAllComponents())
            {
                if (component is ICollisionHandler handler)
                {
                    handler.OnCollision(other);
                }
            }
        }

        private string GetPairKey(GameObject a, GameObject b)
        {
            // Create consistent key regardless of order
            int idA = a.GetHashCode();
            int idB = b.GetHashCode();
            return idA < idB ? $"{idA}_{idB}" : $"{idB}_{idA}";
        }
    }
}
