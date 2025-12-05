using System.Numerics;
using Raylib_cs;

namespace DarkArmsProto
{
    public class Projectile
    {
        public Vector3 Position { get; private set; }
        public float Damage { get; private set; }
        public bool Piercing { get; private set; }
        public bool Lifesteal { get; private set; }
        public bool Homing { get; private set; }

        private Vector3 direction;
        private float speed;
        private float lifetime;
        private float maxLifetime = 5f;
        private Color color;
        private float size;

        public Projectile(
            Vector3 position,
            Vector3 direction,
            float damage,
            float speed,
            Color color,
            float size,
            bool piercing,
            bool lifesteal,
            bool homing
        )
        {
            Position = position;
            this.direction = Vector3.Normalize(direction);
            Damage = damage;
            this.speed = speed;
            this.color = color;
            this.size = size;
            Piercing = piercing;
            Lifesteal = lifesteal;
            Homing = homing;
            lifetime = 0f;
        }

        public void Update(float deltaTime, List<Enemy> enemies)
        {
            // Homing behavior
            if (Homing && enemies.Count > 0)
            {
                Enemy? closestEnemy = null;
                float closestDist = float.MaxValue;

                foreach (var enemy in enemies)
                {
                    float dist = Vector3.Distance(Position, enemy.Position);
                    if (dist < closestDist && dist < 15f)
                    {
                        closestDist = dist;
                        closestEnemy = enemy;
                    }
                }

                if (closestEnemy != null)
                {
                    Vector3 toEnemy = closestEnemy.Position - Position;
                    toEnemy = Vector3.Normalize(toEnemy);

                    // Lerp direction toward enemy
                    direction = Vector3.Lerp(direction, toEnemy, 0.1f);
                    direction = Vector3.Normalize(direction);
                }
            }

            Position += direction * speed * deltaTime;
            lifetime += deltaTime;
        }

        public bool CheckCollision(Enemy enemy)
        {
            float distance = Vector3.Distance(Position, enemy.Position);
            return distance < (size + 0.5f);
        }

        public bool IsExpired()
        {
            return lifetime > maxLifetime;
        }

        public void Render()
        {
            Raylib.DrawSphere(Position, size, color);

            // Add glow effect
            Raylib.DrawSphere(
                Position,
                size * 1.2f,
                new Color((byte)color.R, (byte)color.G, (byte)color.B, (byte)100)
            );
        }
    }
}
