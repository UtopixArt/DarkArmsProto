using System.Numerics;
using DarkArmsProto.Core;

namespace DarkArmsProto.Components
{
    public class ChaseAIComponent : Component
    {
        public float Speed { get; set; }
        public Vector3 TargetPosition { get; set; }

        public ChaseAIComponent(float speed)
        {
            Speed = speed;
        }

        public override void Update(float deltaTime)
        {
            Vector3 direction = TargetPosition - Owner.Position;
            direction.Y = 0;

            if (direction.LengthSquared() > 0.01f)
            {
                Owner.Position += Vector3.Normalize(direction) * Speed * deltaTime;
            }
        }
    }
}
