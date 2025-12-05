using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto
{
    public class SoulManager
    {
        private List<GameObject> souls;
        private WeaponSystem weaponSystem;

        public SoulManager(WeaponSystem weaponSystem)
        {
            souls = new List<GameObject>();
            this.weaponSystem = weaponSystem;
        }

        public void SpawnSoul(Vector3 position, SoulType type)
        {
            var go = new GameObject(position);

            var soulComp = new SoulComponent(type);
            go.AddComponent(soulComp);

            Color color = type switch
            {
                SoulType.Beast => new Color(255, 136, 0, 255),
                SoulType.Undead => new Color(0, 255, 0, 255),
                SoulType.Demon => new Color(255, 0, 0, 255),
                _ => Color.White,
            };

            var meshComp = new MeshRendererComponent(color, new Vector3(0.3f));
            meshComp.MeshType = MeshType.Sphere;
            go.AddComponent(meshComp);

            souls.Add(go);
        }

        public void Update(float deltaTime, Vector3 playerPosition)
        {
            for (int i = souls.Count - 1; i >= 0; i--)
            {
                var soul = souls[i];
                soul.Update(deltaTime);

                var soulComp = soul.GetComponent<SoulComponent>();
                if (soulComp != null)
                {
                    if (soulComp.CheckCollection(playerPosition, deltaTime))
                    {
                        // Feed soul to weapon
                        weaponSystem.FeedSoul(soulComp.Type);
                        souls.RemoveAt(i);
                    }
                }
            }
        }

        public void Render()
        {
            foreach (var soul in souls)
            {
                soul.Render();
            }
        }
    }
}
