using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components.Behaviors;
using DarkArmsProto.Core;

namespace DarkArmsProto.Components
{
    /// <summary>
    /// Refactored ProjectileComponent using Strategy Pattern.
    /// Behaviors are composable and extensible.
    /// </summary>
    public class ProjectileComponent : Component
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

            // Apply velocity
            Owner.Position += Velocity * deltaTime;

            // Update lifetime
            Lifetime -= deltaTime;
            if (Lifetime <= 0)
            {
                Owner.IsActive = false;
            }

            // Check wall collisions
            if (WallColliders != null)
            {
                foreach (var wall in WallColliders)
                {
                    if (wall.CheckPointCollision(Owner.Position))
                    {
                        // Delegate to behaviors
                        bool shouldDestroy = OnWallHit(Owner.Position);

                        if (shouldDestroy)
                        {
                            Owner.IsActive = false;
                            OnWallHitEvent?.Invoke(Owner.Position);
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
    }
}
