using System.Numerics;
using DarkArmsProto.Core;

namespace DarkArmsProto.Components.Behaviors
{
    /// <summary>
    /// Strategy Pattern: Interface for projectile behaviors.
    /// Allows composing multiple behaviors on a single projectile.
    /// </summary>
    public interface IProjectileBehavior
    {
        /// <summary>
        /// Called every frame to update projectile behavior
        /// </summary>
        void Update(GameObject projectile, ProjectileComponent component, float deltaTime);

        /// <summary>
        /// Called when projectile hits a target
        /// </summary>
        /// <returns>True if projectile should be destroyed after hit</returns>
        bool OnHit(
            GameObject projectile,
            ProjectileComponent component,
            GameObject target,
            Vector3 hitPosition
        );

        /// <summary>
        /// Called when projectile hits a wall
        /// </summary>
        /// <returns>True if projectile should be destroyed after wall hit</returns>
        bool OnWallHit(GameObject projectile, ProjectileComponent component, Vector3 hitPosition);
    }
}
