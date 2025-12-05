using System;
using System.Numerics;
using DarkArmsProto.Core;

namespace DarkArmsProto.Components
{
    public class ScreenShakeComponent : Component
    {
        private float trauma = 0f;
        private float maxOffset = 1.5f; // Augmenté de 0.3 à 1.5
        private float maxAngle = 0.15f; // Augmenté de 0.05 à 0.15
        private Random random = new Random();

        public Vector3 ShakeOffset { get; private set; }
        public float ShakeAngle { get; private set; }

        public void AddTrauma(float amount)
        {
            trauma = Math.Min(trauma + amount, 1f);
        }

        public override void Update(float deltaTime)
        {
            if (trauma > 0)
            {
                // Decrease trauma over time (plus lent pour durer plus longtemps)
                trauma = Math.Max(trauma - deltaTime * 1.5f, 0);

                // Shake amount is squared for smoother falloff
                float shake = trauma * trauma;

                // Generate random shake
                ShakeOffset = new Vector3(
                    maxOffset * shake * (float)(random.NextDouble() * 2 - 1),
                    maxOffset * shake * (float)(random.NextDouble() * 2 - 1),
                    maxOffset * shake * (float)(random.NextDouble() * 2 - 1)
                );

                ShakeAngle = maxAngle * shake * (float)(random.NextDouble() * 2 - 1);
            }
            else
            {
                ShakeOffset = Vector3.Zero;
                ShakeAngle = 0f;
            }
        }
    }
}
