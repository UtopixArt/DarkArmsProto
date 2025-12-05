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

            // Ajout des composants
            go.AddComponent(new HealthComponent(hp));
            go.AddComponent(new ChaseAIComponent(speed));
            go.AddComponent(
                new MeshRendererComponent(
                    color,
                    new Vector3(
                        GameConfig.EnemyMeshWidth,
                        GameConfig.EnemyMeshHeight,
                        GameConfig.EnemyMeshDepth
                    )
                )
            );
            go.AddComponent(new EnemyComponent(type)); // Pour savoir quelle âme donner
            go.AddComponent(
                new HealthBarComponent(
                    new Vector3(0, GameConfig.EnemyHealthBarOffsetY, 0),
                    new Vector2(GameConfig.EnemyHealthBarWidth, GameConfig.EnemyHealthBarHeight)
                )
            );

            // Add box collider matching the mesh size
            var collider = new ColliderComponent();
            collider.Size = new Vector3(
                GameConfig.EnemyColliderWidth,
                GameConfig.EnemyColliderHeight,
                GameConfig.EnemyColliderDepth
            );
            go.AddComponent(collider);

            return go;
        }
    }
}
