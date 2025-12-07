using System.Numerics;
using DarkArmsProto.Core;
using DarkArmsProto.VFX;
using Raylib_cs;

namespace DarkArmsProto.Components.Behaviors
{
    public class TrailBehavior : IProjectileBehavior
    {
        private float spawnTimer;
        private float spawnInterval = 0.02f;
        private Color color;

        public TrailBehavior(Color color)
        {
            this.color = color;
        }

        public void Update(GameObject projectile, ProjectileComponent component, float deltaTime)
        {
            spawnTimer += deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0;
                VFXHelper.SpawnTrail(projectile.Position, color);
            }
        }

        public bool OnHit(
            GameObject projectile,
            ProjectileComponent component,
            GameObject target,
            Vector3 hitPosition
        )
        {
            return false;
        }

        public bool OnWallHit(
            GameObject projectile,
            ProjectileComponent component,
            Vector3 hitPosition
        )
        {
            return true;
        }
    }
}
