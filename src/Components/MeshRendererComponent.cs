using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public enum MeshType
    {
        Cube,
        Sphere,
    }

    public class MeshRendererComponent : Component
    {
        public MeshType MeshType { get; set; } = MeshType.Cube;
        public Color Color { get; set; } = Color.White;
        public Vector3 Scale { get; set; } = Vector3.One;

        public MeshRendererComponent() { }

        public MeshRendererComponent(Color color, Vector3 scale)
        {
            Color = color;
            Scale = scale;
        }

        public override void Render()
        {
            // Hit flash effect
            Color drawColor = Color;
            var health = Owner.GetComponent<HealthComponent>();
            if (health != null && health.HitFlashTime > 0)
            {
                drawColor = Color.White;
            }

            if (MeshType == MeshType.Cube)
            {
                Raylib.DrawCube(Owner.Position, Scale.X, Scale.Y, Scale.Z, drawColor);
                Raylib.DrawCubeWires(Owner.Position, Scale.X, Scale.Y, Scale.Z, Color.Black);
            }
            else if (MeshType == MeshType.Sphere)
            {
                Raylib.DrawSphere(Owner.Position, Scale.X, drawColor);
            }
        }
    }
}
