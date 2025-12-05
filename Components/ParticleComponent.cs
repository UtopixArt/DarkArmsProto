using System;
using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class ParticleComponent : Component
    {
        public Vector3 Velocity { get; set; }
        public Color Color { get; set; }
        public float Size { get; set; } = 0.2f;
        public float Lifetime { get; set; } = 1f;
        public float Gravity { get; set; } = -5f;
        private float currentLifetime;
        private float initialLifetime;

        public ParticleComponent(Vector3 velocity, Color color, float lifetime = 1f)
        {
            Velocity = velocity;
            Color = color;
            Lifetime = lifetime;
            currentLifetime = lifetime;
            initialLifetime = lifetime;
        }

        public override void Update(float deltaTime)
        {
            // Apply velocity
            Owner.Position += Velocity * deltaTime;

            // Apply gravity
            Velocity += new Vector3(0, Gravity * deltaTime, 0);

            // Apply drag (air resistance)
            Velocity *= 0.95f;

            // Update lifetime
            currentLifetime -= deltaTime;
            if (currentLifetime <= 0)
            {
                Owner.IsActive = false;
            }
        }

        public override void Render()
        {
            // Fade out based on lifetime
            float lifeRatio = currentLifetime / initialLifetime;
            float currentSize = Size * lifeRatio; // Shrink over time

            Color fadeColor = new Color(Color.R, Color.G, Color.B, (byte)(Color.A * lifeRatio));

            // Use billboard for cleaner look
            Raylib.DrawBillboard(
                Game.GameCamera,
                Game.WhiteTexture,
                Owner.Position,
                currentSize,
                fadeColor
            );
        }
    }
}
