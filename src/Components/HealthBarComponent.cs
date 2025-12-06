using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class HealthBarComponent : Component
    {
        public Vector3 Offset { get; set; } = new Vector3(0, 3.5f, 0);
        public Vector2 Size { get; set; } = new Vector2(1.5f, 0.1f);
        public Color BackgroundColor { get; set; } = Color.Gray;
        public Color ForegroundColor { get; set; } = Color.Red;

        public HealthBarComponent(Vector3 offset, Vector2 size)
        {
            Offset = offset;
            Size = size;
        }

        public override void Render()
        {
            // Do nothing in 3D render pass
        }

        public void DrawUI()
        {
            var health = Owner.GetComponent<HealthComponent>();
            if (health == null)
                return;

            // Calculate percentage
            float percent = health.CurrentHealth / health.MaxHealth;
            if (percent < 0)
                percent = 0;
            if (percent > 1)
                percent = 1;

            Vector3 pos = Owner.Position + Offset;

            // Get screen position
            Vector2 screenPos = Raylib.GetWorldToScreen(pos, Game.GameCamera);

            // Check if in front of camera
            Vector3 toEnemy = Vector3.Normalize(pos - Game.GameCamera.Position);
            Vector3 camForward = Vector3.Normalize(
                Game.GameCamera.Target - Game.GameCamera.Position
            );
            if (Vector3.Dot(toEnemy, camForward) < 0)
                return;

            float width = 80f;
            float height = 10f;

            // Center the bar
            float startX = screenPos.X - width / 2;
            float startY = screenPos.Y;

            // Background
            Raylib.DrawRectangle(
                (int)startX,
                (int)startY,
                (int)width,
                (int)height,
                new Color(0, 0, 0, 200)
            );

            // Foreground
            Raylib.DrawRectangle(
                (int)startX,
                (int)startY,
                (int)(width * percent),
                (int)height,
                ForegroundColor
            );
        }
    }
}
