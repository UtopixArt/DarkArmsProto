using System.Numerics;
using DarkArmsProto.Core;

namespace DarkArmsProto.Components.Behaviors
{
    /// <summary>
    /// Piercing behavior: Projectile passes through enemies without being destroyed
    /// </summary>
    public class PiercingBehavior : IProjectileBehavior
    {
        private int maxPierces;
        private int currentPierces;

        public PiercingBehavior(int maxPierces = -1) // -1 = infinite
        {
            this.maxPierces = maxPierces;
            this.currentPierces = 0;
        }

        public void Update(GameObject projectile, ProjectileComponent component, float deltaTime)
        {
            // Piercing doesn't need update logic
        }

        public bool OnHit(
            GameObject projectile,
            ProjectileComponent component,
            GameObject target,
            Vector3 hitPosition
        )
        {
            currentPierces++;

            // Destroy if max pierces reached
            if (maxPierces > 0 && currentPierces >= maxPierces)
            {
                return true; // Destroy projectile
            }

            return false; // Don't destroy, continue piercing
        }

        public bool OnWallHit(
            GameObject projectile,
            ProjectileComponent component,
            Vector3 hitPosition
        )
        {
            // Piercing projectiles are destroyed by walls
            return true;
        }
    }
}
