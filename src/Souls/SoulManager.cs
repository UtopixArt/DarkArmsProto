using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Audio;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.VFX;
using Raylib_cs;

namespace DarkArmsProto
{
    public class SoulManager
    {
        private List<GameObject> souls;
        private WeaponComponent weaponComponent;
        private ParticleManager? particleManager;

        public SoulManager(WeaponComponent weaponComponent)
        {
            souls = new List<GameObject>();
            this.weaponComponent = weaponComponent;
        }

        public void SetParticleManager(ParticleManager pm)
        {
            particleManager = pm;
        }

        public void SpawnSoul(Vector3 position, SoulType type)
        {
            var go = new GameObject(position);

            var soulComp = new SoulComponent(type);
            soulComp.ParticleManager = particleManager;
            go.AddComponent(soulComp);

            Color color = type switch
            {
                SoulType.Beast => new Color(255, 136, 0, 200),
                SoulType.Undead => new Color(0, 255, 0, 200),
                SoulType.Demon => new Color(255, 0, 0, 200),
                _ => Color.White,
            };

            var meshComp = new MeshRendererComponent(color, new Vector3(0.3f));
            var color2 = new Color(color.R, color.G, color.B, (byte)100);
            var meshComp2 = new MeshRendererComponent(color2, new Vector3(0.4f));
            meshComp.MeshType = MeshType.Sphere;
            meshComp2.MeshType = MeshType.Sphere;
            go.AddComponent(meshComp);
            go.AddComponent(meshComp2);

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
                        // Play soul collect sound
                        AudioManager.Instance.PlaySound(SoundType.SoulCollect, 0.3f);

                        // Spawn collection particles
                        if (particleManager != null)
                        {
                            var meshComp = soul.GetComponent<MeshRendererComponent>();
                            Color soulColor = meshComp != null ? meshComp.Color : Color.White;
                            particleManager.SpawnSoulCollectEffect(soul.Position, soulColor);
                        }

                        // Feed soul to weapon
                        weaponComponent.FeedSoul(soulComp.Type);
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
