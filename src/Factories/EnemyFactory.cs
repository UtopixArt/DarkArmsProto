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

        public GameObject SpawnEnemy(
            Vector3 position,
            SoulType type,
            List<ColliderComponent>? wallColliders = null
        )
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
                .WithSprite(enemyData.SpritePath, enemyData.SpriteSize, enemyData.GetSpriteOffset())
                .WithColliderSize(enemyData.GetColliderSize())
                .WithColliderOffset(enemyData.GetColliderOffset());

            if (wallColliders != null)
            {
                builder.WithWallColliders(wallColliders);
            }

            if (enemyData.IsFlying)
                builder.AsFlying();

            if (enemyData.IsRanged)
                builder.AsRanged();

            var enemy = builder.Build();

            // NOTE: L'enregistrement dans GameWorld est maintenant géré par EnemySpawnSystem
            // pour avoir un meilleur contrôle sur l'initialisation (éviter que les ennemis tombent)

            return enemy;
        }
    }
}
