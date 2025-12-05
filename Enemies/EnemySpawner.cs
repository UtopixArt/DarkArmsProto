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
            GameObject go = new GameObject();
            go.Position = position;

            // Stats selon le type
            float hp = (type == SoulType.Beast) ? 150 : 100;
            float speed = (type == SoulType.Beast) ? 6 : 4;
            Color color = (type == SoulType.Beast) ? new Color(255, 136, 0, 255) : Color.Green;
            if (type == SoulType.Demon)
            {
                hp = 200;
                speed = 5;
                color = Color.Red;
            }

            // Ajout des composants
            go.AddComponent(new HealthComponent(hp));
            go.AddComponent(new ChaseAIComponent(speed));
            go.AddComponent(new MeshRendererComponent(color, new Vector3(1, 1.5f, 1)));
            go.AddComponent(new EnemyComponent(type)); // Pour savoir quelle âme donner

            return go;
        }
    }
}
