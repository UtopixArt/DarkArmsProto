using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components.Behaviors;
using DarkArmsProto.Core;

namespace DarkArmsProto.Components
{
    /// <summary>
    /// Refactored ProjectileComponent using Strategy Pattern.
    /// Behaviors are composable and extensible.
    /// Implements ICollisionHandler for automatic collision detection.
    /// </summary>
    public class ProjectileComponent : Component, ICollisionHandler
    {
        // Core properties
        public Vector3 Velocity { get; set; }
        public float Lifetime { get; set; } = 3.0f;
        public float Damage { get; set; } = 10f;
        public bool IsEnemyProjectile { get; set; } = false;

        // Strategy Pattern: Composable behaviors
        private List<IProjectileBehavior> behaviors = new List<IProjectileBehavior>();

        // Wall collision support
        public List<ColliderComponent>? WallColliders { get; set; }
        public System.Action<Vector3>? OnWallHitEvent;

        public ProjectileComponent() { }

        /// <summary>
        /// Add a behavior to this projectile (Strategy Pattern)
        /// </summary>
        public void AddBehavior(IProjectileBehavior behavior)
        {
            behaviors.Add(behavior);
        }

        /// <summary>
        /// Remove a specific behavior
        /// </summary>
        public void RemoveBehavior(IProjectileBehavior behavior)
        {
            behaviors.Remove(behavior);
        }

        /// <summary>
        /// Get all behaviors (for external systems to configure)
        /// </summary>
        public List<IProjectileBehavior> GetBehaviors() => behaviors;

        public override void Update(float deltaTime)
        {
            // Update all behaviors
            foreach (var behavior in behaviors)
            {
                behavior.Update(Owner, this, deltaTime);
            }

            // Calculate movement
            Vector3 moveAmount = Velocity * deltaTime;
            float moveDistance = moveAmount.Length();

            // Raycast check for walls to prevent tunneling
            if (WallColliders != null && moveDistance > 0.001f)
            {
                Vector3 moveDir = Vector3.Normalize(moveAmount);
                float closestHit = float.MaxValue;
                Vector3 hitPoint = Vector3.Zero;
                bool hitWall = false;

                foreach (var wall in WallColliders)
                {
                    if (
                        wall.Raycast(
                            Owner.Position,
                            moveDir,
                            moveDistance,
                            out float dist,
                            out Vector3 normal,
                            out Vector3 point
                        )
                    )
                    {
                        if (dist < closestHit)
                        {
                            closestHit = dist;
                            hitPoint = point;
                            hitWall = true;
                        }
                    }
                }

                if (hitWall)
                {
                    // Move to hit point
                    Owner.Position = hitPoint;

                    // Handle collision
                    bool shouldDestroy = OnWallHit(hitPoint);
                    if (shouldDestroy)
                    {
                        Owner.IsActive = false;
                        OnWallHitEvent?.Invoke(hitPoint);
                        return; // Stop updating
                    }
                }
            }

            // Apply velocity (if no hit or not destroyed)
            Owner.Position += moveAmount;

            // Update lifetime
            Lifetime -= deltaTime;
            if (Lifetime <= 0)
            {
                Owner.IsActive = false;
            }

            // Fallback: Check point collision if we somehow ended up inside a wall
            // (e.g. spawned inside, or raycast missed due to precision)
            if (WallColliders != null)
            {
                foreach (var wall in WallColliders)
                {
                    if (wall.CheckPointCollision(Owner.Position))
                    {
                        bool shouldDestroy = OnWallHit(Owner.Position);
                        if (shouldDestroy)
                        {
                            Owner.IsActive = false;
                            OnWallHitEvent?.Invoke(Owner.Position);
                            return;
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Called when projectile hits a target
        /// </summary>
        /// <returns>True if projectile should be destroyed</returns>
        public bool OnHit(GameObject target, Vector3 hitPosition)
        {
            bool shouldDestroy = false;

            // Ask each behavior if projectile should be destroyed
            foreach (var behavior in behaviors)
            {
                bool behaviorDestroy = behavior.OnHit(Owner, this, target, hitPosition);
                shouldDestroy = shouldDestroy || behaviorDestroy;
            }

            // If no behaviors said to destroy, default is to destroy
            if (behaviors.Count == 0)
                shouldDestroy = true;

            return shouldDestroy;
        }

        /// <summary>
        /// Called when projectile hits a wall
        /// </summary>
        /// <returns>True if projectile should be destroyed</returns>
        public bool OnWallHit(Vector3 hitPosition)
        {
            bool shouldDestroy = true; // Default: walls destroy projectiles

            // Ask each behavior
            foreach (var behavior in behaviors)
            {
                bool behaviorDestroy = behavior.OnWallHit(Owner, this, hitPosition);
                // If ANY behavior says don't destroy, we don't destroy
                if (!behaviorDestroy)
                    shouldDestroy = false;
            }

            return shouldDestroy;
        }

        /// <summary>
        /// ICollisionHandler implementation - called automatically by CollisionSystem
        /// </summary>
        public void OnCollision(GameObject other)
        {
            // Skip if projectile already dead
            if (!Owner.IsActive)
                return;

            // Player projectile hits enemy
            if (!IsEnemyProjectile && other.CompareTag("Enemy"))
            {
                HandleEnemyHit(other);
            }
            // Enemy projectile hits player
            else if (IsEnemyProjectile && other.CompareTag("Player"))
            {
                HandlePlayerHit(other);
            }
        }

        private void HandleEnemyHit(GameObject enemy)
        {
            // Apply damage
            var health = enemy.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.TakeDamage(Damage);

                // Add damage number
                Systems.DamageNumberManager.AddDamageNumber(enemy.Position, Damage);

                // Impact VFX
                var mesh = Owner.GetComponent<MeshRendererComponent>();
                Raylib_cs.Color color = mesh != null ? mesh.Color : Raylib_cs.Color.White;

                // Spawn blood
                VFX.VFXHelper.SpawnBlood(Owner.Position);

                // Spawn impact
                VFX.VFXHelper.SpawnImpact(Owner.Position, color, 10);

                // Check if enemy died - handled by EnemyDeathComponent now
            }

            // Ask behaviors if should destroy
            bool shouldDestroy = OnHit(enemy, Owner.Position);

            if (shouldDestroy)
            {
                Owner.IsActive = false;
            }
        }

        private void HandlePlayerHit(GameObject player)
        {
            // Apply damage to player
            var health = player.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.TakeDamage(Damage);

                // Impact VFX
                var mesh = Owner.GetComponent<MeshRendererComponent>();
                Raylib_cs.Color color = mesh != null ? mesh.Color : Raylib_cs.Color.Red;
                VFX.VFXHelper.SpawnImpact(Owner.Position, color, 5);
            }

            Owner.IsActive = false;
        }
    }
}
