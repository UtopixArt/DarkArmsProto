using System.Numerics;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.Data;
using Raylib_cs;

namespace DarkArmsProto.Factories
{
    public class EnemyFactory
    {
        public EnemyFactory()
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

            var builder = new Builders.EnemyBuilder()
                .AtPosition(position)
                .OfType(type)
                .WithHealth(enemyData.Health)
                .WithSpeed(enemyData.Speed)
                .WithDamage(enemyData.Damage)
                .WithAttackRange(enemyData.AttackRange)
                .WithDetectionRange(enemyData.DetectionRange)
                .WithAttackCooldown(enemyData.AttackCooldown)
                .WithSprite(enemyData.SpritePath, enemyData.SpriteSize)
                .WithColliderSize(enemyData.GetColliderSize());

            if (enemyData.IsFlying)
                builder.AsFlying();

            if (enemyData.IsRanged)
                builder.AsRanged();

            return builder.Build();
        }
    }
}
