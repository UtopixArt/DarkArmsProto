using System.Numerics;
using DarkArmsProto.Components; // AJOUTER CECI
using DarkArmsProto.Core; // AJOUTER CECI
using Raylib_cs;

namespace DarkArmsProto
{
    public class EnemySpawner
    {
        public GameObject SpawnEnemy(Vector3 position, SoulType type)
        {
            GameObject go = new GameObject(position);

            // Stats selon le type (using GameConfig)
            float hp = type switch
            {
                SoulType.Beast => GameConfig.BeastEnemyHealth,
                SoulType.Undead => GameConfig.UndeadEnemyHealth,
                SoulType.Demon => GameConfig.DemonEnemyHealth,
                _ => GameConfig.UndeadEnemyHealth,
            };

            float speed = type switch
            {
                SoulType.Beast => GameConfig.BeastEnemySpeed,
                SoulType.Undead => GameConfig.UndeadEnemySpeed,
                SoulType.Demon => GameConfig.DemonEnemySpeed,
                _ => GameConfig.UndeadEnemySpeed,
            };

            Color color = type switch
            {
                SoulType.Beast => new Color(255, 136, 0, 255),
                SoulType.Undead => new Color(0, 255, 0, 255),
                SoulType.Demon => new Color(255, 0, 0, 255),
                _ => Color.White,
            };

            // Determine mesh size based on type
            Vector3 meshSize;
            Vector3 colliderSize;

            if (type == SoulType.Demon)
            {
                // Small cube for flying demons
                meshSize = new Vector3(
                    GameConfig.DemonMeshSize,
                    GameConfig.DemonMeshSize,
                    GameConfig.DemonMeshSize
                );
                colliderSize = new Vector3(
                    GameConfig.DemonColliderSize,
                    GameConfig.DemonColliderSize,
                    GameConfig.DemonColliderSize
                );
            }
            else
            {
                // Standard tall enemy
                meshSize = new Vector3(
                    GameConfig.EnemyMeshWidth,
                    GameConfig.EnemyMeshHeight,
                    GameConfig.EnemyMeshDepth
                );
                colliderSize = new Vector3(
                    GameConfig.EnemyColliderWidth,
                    GameConfig.EnemyColliderHeight,
                    GameConfig.EnemyColliderDepth
                );
            }

            // Ajout des composants
            go.AddComponent(new HealthComponent(hp));
            go.AddComponent(new EnemyAIComponent(type, speed));
            go.AddComponent(new MeshRendererComponent(color, meshSize));
            go.AddComponent(new EnemyComponent(type)); // Pour savoir quelle âme donner
            go.AddComponent(
                new HealthBarComponent(
                    new Vector3(
                        0,
                        type == SoulType.Demon ? 1.5f : GameConfig.EnemyHealthBarOffsetY,
                        0
                    ),
                    new Vector2(GameConfig.EnemyHealthBarWidth, GameConfig.EnemyHealthBarHeight)
                )
            );

            // Add box collider matching the mesh size
            var collider = new ColliderComponent();
            collider.Size = colliderSize;
            go.AddComponent(collider);

            return go;
        }
    }
}
