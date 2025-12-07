using System;
using System.Numerics;
using DarkArmsProto.Core;
using DarkArmsProto.VFX;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class SoulComponent : Component
    {
        public SoulType Type { get; set; }
        public bool IsCollected { get; set; }
        public ParticleManager? ParticleManager { get; set; }

        private float time;
        private float floatSpeed = GameConfig.SoulFloatSpeed;
        private float magnetRadius = GameConfig.SoulMagnetRadius;
        private float collectRadius = GameConfig.SoulCollectRadius;

        private float particleTimer;
        private float particleInterval = 0.1f;

        public SoulComponent(SoulType type)
        {
            Type = type;
        }

        public override void Update(float deltaTime)
        {
            time += deltaTime;

            // Floating animation
            float baseY = 0.5f;
            float floatOffset = MathF.Sin(time * floatSpeed) * 0.2f;
            Owner.Position = new Vector3(Owner.Position.X, baseY + floatOffset, Owner.Position.Z);

            // Spawn particles
            particleTimer += deltaTime;
            if (particleTimer >= particleInterval)
            {
                particleTimer = 0;
                // Use soul color based on type
                Color soulColor = Type switch
                {
                    SoulType.Beast => new Color(255, 136, 0, 150),
                    SoulType.Undead => new Color(0, 255, 0, 150),
                    SoulType.Demon => new Color(255, 0, 0, 150),
                    _ => new Color(0, 255, 255, 150),
                };
                ParticleManager?.SpawnEffect("Soul", Owner.Position, soulColor);
            }
        }

        public bool CheckCollection(Vector3 playerPosition, float deltaTime)
        {
            float distance = Vector3.Distance(Owner.Position, playerPosition);

            if (distance < magnetRadius && !IsCollected)
            {
                // Move towards player
                Vector3 direction = Vector3.Normalize(playerPosition - Owner.Position);
                Owner.Position += direction * GameConfig.SoulMoveSpeed * deltaTime;

                if (distance < collectRadius)
                {
                    IsCollected = true;
                    return true;
                }
            }
            return false;
        }
    }
}
