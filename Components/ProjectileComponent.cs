using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class ProjectileComponent : Component
    {
        public Vector3 Velocity { get; set; }
        public float Lifetime { get; set; } = 3.0f;
        public float Damage { get; set; } = 10f;
        public bool Piercing { get; set; }
        public bool Lifesteal { get; set; }
        public bool Homing { get; set; }
        public float HomingStrength { get; set; } = 0.1f;
        public float HomingRange { get; set; } = 20f;
        public bool IsEnemyProjectile { get; set; } = false;

        public static List<GameObject> Enemies { get; set; } = new List<GameObject>();

        public override void Update(float deltaTime)
        {
            if (Homing && Enemies != null)
            {
                GameObject? closestEnemy = null;
                float closestDist = float.MaxValue;

                foreach (var enemy in Enemies)
                {
                    if (!enemy.IsActive)
                        continue;

                    float dist = Vector3.Distance(Owner.Position, enemy.Position);
                    if (dist < closestDist && dist < HomingRange)
                    {
                        closestDist = dist;
                        closestEnemy = enemy;
                    }
                }

                if (closestEnemy != null)
                {
                    Vector3 toEnemy = Vector3.Normalize(closestEnemy.Position - Owner.Position);
                    Vector3 currentDir = Vector3.Normalize(Velocity);
                    Vector3 newDir = Vector3.Normalize(
                        Vector3.Lerp(currentDir, toEnemy, HomingStrength)
                    );
                    float speed = Velocity.Length();
                    Velocity = newDir * speed;
                }
            }

            Owner.Position += Velocity * deltaTime;
            Lifetime -= deltaTime;
            if (Lifetime <= 0)
            {
                Owner.IsActive = false;
            }
        }
    }
}
