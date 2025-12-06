using System;
using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class SoulComponent : Component
    {
        public SoulType Type { get; set; }
        public bool IsCollected { get; set; }

        private float time;
        private float floatSpeed = GameConfig.SoulFloatSpeed;
        private float magnetRadius = GameConfig.SoulMagnetRadius;
        private float collectRadius = GameConfig.SoulCollectRadius;

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
