using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Core;

namespace DarkArmsProto.Components.Behaviors
{
    /// <summary>
    /// Homing behavior: Projectile tracks nearest enemy
    /// </summary>
    public class HomingBehavior : IProjectileBehavior
    {
        private float homingStrength;
        private float homingRange;
        private List<GameObject> enemies;

        public HomingBehavior(float strength = 0.1f, float range = 20f)
        {
            this.homingStrength = strength;
            this.homingRange = range;
            this.enemies = new List<GameObject>();
        }

        public void SetEnemies(List<GameObject> enemies)
        {
            this.enemies = enemies;
        }

        public void Update(GameObject projectile, ProjectileComponent component, float deltaTime)
        {
            if (enemies == null || enemies.Count == 0)
                return;

            // Find closest enemy
            GameObject? closestEnemy = null;
            float closestDist = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (!enemy.IsActive)
                    continue;

                float dist = Vector3.Distance(projectile.Position, enemy.Position);
                if (dist < closestDist && dist < homingRange)
                {
                    closestDist = dist;
                    closestEnemy = enemy;
                }
            }

            if (closestEnemy != null)
            {
                // Steer towards enemy
                Vector3 toEnemy = Vector3.Normalize(closestEnemy.Position - projectile.Position);
                Vector3 currentDir = Vector3.Normalize(component.Velocity);
                Vector3 newDir = Vector3.Normalize(
                    Vector3.Lerp(currentDir, toEnemy, homingStrength)
                );

                float speed = component.Velocity.Length();
                component.Velocity = newDir * speed;
            }
        }

        public bool OnHit(
            GameObject projectile,
            ProjectileComponent component,
            GameObject target,
            Vector3 hitPosition
        )
        {
            // Homing doesn't affect hit behavior
            return false; // Don't destroy (let other behaviors decide)
        }

        public bool OnWallHit(
            GameObject projectile,
            ProjectileComponent component,
            Vector3 hitPosition
        )
        {
            // Homing doesn't affect wall hit
            return false;
        }
    }
}
