using System;
using System.Numerics;

namespace DarkArmsProto
{
    public class EnemySpawner
    {
        private Random random;
        private float spawnRadius = 12f;

        public EnemySpawner()
        {
            random = new Random();
        }

        public Enemy SpawnEnemy()
        {
            // Random position around the room
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float distance = 8f + (float)(random.NextDouble() * 5f);

            Vector3 position = new Vector3(
                MathF.Cos(angle) * distance,
                0.75f,
                MathF.Sin(angle) * distance
            );

            // Random enemy type
            SoulType type = (SoulType)random.Next(0, 3);

            return new Enemy(position, type);
        }
    }
}
