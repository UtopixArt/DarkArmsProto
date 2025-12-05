using System.Numerics;
using Raylib_cs;

namespace DarkArmsProto
{
    public class Enemy
    {
        public Vector3 Position { get; private set; }
        public SoulType Type { get; private set; }

        private float health;
        private float maxHealth;
        private float hitFlashTime = 0f;
        private float speed;
        private Color color;
        private Vector3 size;

        public Enemy(Vector3 position, SoulType type)
        {
            Position = position;
            Type = type;

            // Set stats based on type
            switch (type)
            {
                case SoulType.Beast:
                    maxHealth = GameConfig.BeastEnemyHealth;
                    speed = GameConfig.BeastEnemySpeed;
                    color = new Color(255, 136, 0, 255);
                    break;
                case SoulType.Undead:
                    maxHealth = GameConfig.UndeadEnemyHealth;
                    speed = GameConfig.UndeadEnemySpeed;
                    color = new Color(0, 255, 0, 255);
                    break;
                case SoulType.Demon:
                    maxHealth = GameConfig.DemonEnemyHealth;
                    speed = GameConfig.DemonEnemySpeed;
                    color = new Color(255, 0, 0, 255);
                    break;
            }

            health = maxHealth;
            size = new Vector3(1f, 1.5f, 1f);
        }

        public void SetPosition(Vector3 newPosition)
        {
            Position = newPosition;
        }

        public void Update(float deltaTime, Vector3 playerPosition)
        {
            if (hitFlashTime > 0)
                hitFlashTime -= deltaTime;
            // Move toward player
            Vector3 direction = playerPosition - Position;
            direction.Y = 0; // Move only on XZ plane

            if (direction != Vector3.Zero)
            {
                direction = Vector3.Normalize(direction);
                Position += direction * speed * deltaTime;
            }
        }

        public void TakeDamage(float damage)
        {
            health -= damage;
            hitFlashTime = GameConfig.HitFlashDuration;
        }

        public bool IsDead()
        {
            return health <= 0;
        }

        public void Render()
        {
            // Main body
            Color renderColor = hitFlashTime > 0 ? Color.White : color;
            Raylib.DrawCubeV(Position + new Vector3(0, 0.75f, 0), size, renderColor);

            // Health bar
            float healthPercent = health / maxHealth;
            Vector3 barPos = Position + new Vector3(0, 2f, 0);

            Raylib.DrawCubeV(barPos, new Vector3(1f, 0.1f, 0.05f), Color.Red);
            Raylib.DrawCubeV(
                new Vector3(barPos.X - (1f - healthPercent) * 0.5f, barPos.Y, barPos.Z),
                new Vector3(healthPercent, 0.12f, 0.06f),
                Color.Green
            );

            // Glow effect based on health
            float emissive = 0.3f + (1f - healthPercent) * 0.7f;
            Color glowColor = new Color(color.R, color.G, color.B, (byte)(100 * emissive));
            Raylib.DrawCubeV(Position + new Vector3(0, 0.75f, 0), size * 1.1f, glowColor);
        }
    }
}
