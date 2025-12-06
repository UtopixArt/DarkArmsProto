using System;
using System.Numerics;
using DarkArmsProto.Core;

namespace DarkArmsProto.Components.Behaviors
{
    /// <summary>
    /// Explosive behavior: Projectile creates explosion on impact
    /// </summary>
    public class ExplosiveBehavior : IProjectileBehavior
    {
        private float explosionRadius;
        private float explosionDamage;
        private Action<Vector3, float, float>? onExplode; // Callback: pos, radius, damage

        public ExplosiveBehavior(
            float radius = 3.0f,
            float damageMultiplier = 1.0f,
            Action<Vector3, float, float>? explosionCallback = null
        )
        {
            this.explosionRadius = radius;
            this.explosionDamage = damageMultiplier;
            this.onExplode = explosionCallback;
        }

        public void Update(GameObject projectile, ProjectileComponent component, float deltaTime)
        {
            // Explosive doesn't need update logic
        }

        public bool OnHit(
            GameObject projectile,
            ProjectileComponent component,
            GameObject target,
            Vector3 hitPosition
        )
        {
            TriggerExplosion(hitPosition, component.Damage);
            return true; // Destroy projectile on hit
        }

        public bool OnWallHit(
            GameObject projectile,
            ProjectileComponent component,
            Vector3 hitPosition
        )
        {
            TriggerExplosion(hitPosition, component.Damage);
            return true; // Destroy projectile on wall hit
        }

        private void TriggerExplosion(Vector3 position, float baseDamage)
        {
            float totalDamage = baseDamage * explosionDamage;
            onExplode?.Invoke(position, explosionRadius, totalDamage);
        }
    }
}
