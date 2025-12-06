using System.Numerics;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.Data;
using Raylib_cs;

namespace DarkArmsProto
{
    public class EnemySpawner
    {
        public EnemySpawner()
        {
            // Load enemy database
            EnemyDatabase.Load();
        }

        public GameObject SpawnEnemy(Vector3 position, SoulType type)
        {
            // Get enemy data from database
            var enemyData = EnemyDatabase.Get(type);

            if (enemyData == null)
            {
                Console.WriteLine($"[EnemySpawner] ERROR: No data found for {type}");
                return new GameObject(position);
            }

            // Adjust spawn position based on sprite size
            float spriteSize = enemyData.SpriteSize;
            Vector3 spawnPos = position + new Vector3(0, spriteSize / 2.0f, 0);

            // Flying enemies start higher
            if (enemyData.IsFlying)
                spawnPos.Y += 1.5f;

            var go = new GameObject(spawnPos);

            // Add components using data from JSON
            go.AddComponent(new HealthComponent(enemyData.Health));
            go.AddComponent(
                new EnemyAIComponent(type, enemyData.Speed)
                {
                    AttackRange = enemyData.AttackRange,
                    DetectionRange = enemyData.DetectionRange,
                    AttackCooldown = enemyData.AttackCooldown,
                    Damage = enemyData.Damage,
                    IsFlying = enemyData.IsFlying,
                    IsRanged = enemyData.IsRanged,
                }
            );

            go.AddComponent(
                new SpriteRendererComponent(enemyData.SpritePath, enemyData.SpriteSize, Color.White)
            );

            go.AddComponent(new EnemyComponent(type));
            go.AddComponent(
                new HealthBarComponent(
                    new Vector3(0, enemyData.IsFlying ? 1.5f : GameConfig.EnemyHealthBarOffsetY, 0),
                    new Vector2(GameConfig.EnemyHealthBarWidth, GameConfig.EnemyHealthBarHeight)
                )
            );

            // Add collider
            var collider = new ColliderComponent { Size = enemyData.GetColliderSize() };
            go.AddComponent(collider);

            return go;
        }
    }
}
