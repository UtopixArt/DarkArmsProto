using Raylib_cs;
using System;
using System.Numerics;

namespace DarkArmsProto
{
    public class Soul
    {
        public Vector3 Position { get; private set; }
        public SoulType Type { get; private set; }
        public bool IsCollected { get; set; }

        private Color color;
        private float time;
        private float floatSpeed = 2f;
        private float rotationSpeed = 90f;
        private float magnetRadius = 3f;
        private float collectRadius = 1.5f;

        public Soul(Vector3 position, SoulType type)
        {
            Position = position;
            Position = new Vector3(Position.X, 0.5f, Position.Z);
            Type = type;
            IsCollected = false;
            time = 0f;

            color = type switch
            {
                SoulType.Beast => new Color(255, 136, 0, 255),
                SoulType.Undead => new Color(0, 255, 0, 255),
                SoulType.Demon => new Color(255, 0, 0, 255),
                _ => Color.White
            };
        }

        public void Update(float deltaTime, Vector3 playerPosition)
        {
            time += deltaTime;

            // Floating animation
            float baseY = 0.5f;
            float floatOffset = MathF.Sin(time * floatSpeed) * 0.2f;
            Position = new Vector3(Position.X, baseY + floatOffset, Position.Z);

            // Check if player is close enough to collect
            float distance = Vector3.Distance(Position, playerPosition);

            if (distance < magnetRadius && !IsCollected)
            {
                // Move toward player
                Vector3 direction = playerPosition - Position;
                direction = Vector3.Normalize(direction);
                Position += direction * 10f * deltaTime;
            }

            if (distance < collectRadius)
            {
                IsCollected = true;
            }
        }

        public void Render()
        {
            if (IsCollected) return;

            // Draw soul orb
            Raylib.DrawSphere(Position, 0.3f, color);

            // Glow effect
            Raylib.DrawSphere(Position, 0.4f, new Color((byte)color.R, (byte)color.G, (byte)color.B, (byte)100));

            // Outer pulse
            float pulseSize = 0.5f + MathF.Sin(time * 3f) * 0.1f;
            Raylib.DrawSphere(Position, pulseSize, new Color((byte)color.R, (byte)color.G, (byte)color.B, (byte)50));
        }
    }
}