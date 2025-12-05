using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class MeshRendererComponent : Component
    {
        private Color Color { get; set; }
        private Vector3 Size { get; set; }

        public MeshRendererComponent(Color color, Vector3 size)
        {
            Color = color;
            Size = size;
        }

        public override void Render()
        {
            var health = Owner.GetComponent<HealthComponent>();
            Color drawColor = Color;

            if (health != null && health.HitFlashTime > 0)
                drawColor = Color.White;

            Raylib.DrawCubeV(Owner.Position + new Vector3(0, Size.Y / 2, 0), Size, drawColor);

            if (health != null)
                DrawHealthBar(health);
        }

        private void DrawHealthBar(HealthComponent health)
        {
            float percent = health.Current / health.Max;
            Vector3 barPosition = Owner.Position + new Vector3(0, 2f, 0);

            Raylib.DrawCubeV(barPosition, new Vector3(1f, 0.1f, 0.05f), Color.Red);

            if (percent > 0)
            {
                Raylib.DrawCubeV(
                    new Vector3(
                        barPosition.X - (1f - percent) * 0.5f,
                        barPosition.Y,
                        barPosition.Z
                    ),
                    new Vector3(percent, 0.12f, 0.06f),
                    Color.Green
                );
            }
        }
    }
}
