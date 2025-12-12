using System.Numerics;
using DarkArmsProto.Components.Behaviors;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    /// <summary>
    /// Renders a laser beam as a line from start to end position.
    /// Gets positions from LaserBehavior.
    /// </summary>
    public class LaserRenderComponent : Component
    {
        public Color Color { get; set; } = new Color(0, 255, 255, 255);
        public float Thickness { get; set; } = 0.05f;
        public float GlowIntensity { get; set; } = 1.5f;

        private LaserBehavior? laserBehavior = null;

        public override void Start()
        {
            // Find the LaserBehavior attached to this projectile
            var projectileComp = Owner.GetComponent<ProjectileComponent>();
            if (projectileComp != null)
            {
                foreach (var behavior in projectileComp.GetBehaviors())
                {
                    if (behavior is LaserBehavior laser)
                    {
                        laserBehavior = laser;
                        break;
                    }
                }
            }
        }

        public override void Render()
        {
            if (laserBehavior == null) return;

            // Get all bounce points
            var bouncePoints = laserBehavior.GetBouncePoints();
            if (bouncePoints.Count < 2) return;

            // Draw all segments
            for (int i = 0; i < bouncePoints.Count - 1; i++)
            {
                Vector3 start = bouncePoints[i];
                Vector3 end = bouncePoints[i + 1];

                // Calculate direction and distance
                Vector3 direction = end - start;
                float distance = direction.Length();
                if (distance < 0.001f) continue;

                direction = Vector3.Normalize(direction);

                DrawLaserSegment(start, end, direction, distance);
            }

            // Draw impact sphere at final endpoint
            Vector3 finalEnd = bouncePoints[bouncePoints.Count - 1];
            Color core = new Color(
                (byte)MathF.Min(255, Color.R + 100),
                (byte)MathF.Min(255, Color.G + 50),
                (byte)Color.B,
                (byte)255
            );
            Color outerGlow = new Color(
                (byte)(Color.R * 0.6f),
                (byte)(Color.G * 0.8f),
                (byte)(Color.B),
                (byte)(Color.A * 0.3f)
            );
            Raylib.DrawSphere(finalEnd, Thickness * 3, core);
            Raylib.DrawSphere(finalEnd, Thickness * 5, outerGlow);

            // TODO: Add dynamic light at endpoint (need LightManager reference)
            // VFX.LightManager.AddLight(end, Color, GlowIntensity, radius, lifetime, flicker);
        }

        /// <summary>
        /// Draw a single laser segment with 3 layers
        /// </summary>
        private void DrawLaserSegment(Vector3 start, Vector3 end, Vector3 direction, float distance)
        {
            // Draw outer glow cylinder (cyan bleu transparent)
            Color outerGlow = new Color(
                (byte)(Color.R * 0.6f),
                (byte)(Color.G * 0.8f),
                (byte)(Color.B),
                (byte)(Color.A * 0.3f)
            );
            DrawLaserCylinder(start, end, direction, distance, Thickness * 2.5f, outerGlow);

            // Draw middle glow (cyan moyen)
            Color middleGlow = new Color(
                (byte)(Color.R * 0.7f),
                (byte)(Color.G * 0.9f),
                Color.B,
                (byte)(Color.A * 0.6f)
            );
            DrawLaserCylinder(start, end, direction, distance, Thickness * 1.5f, middleGlow);

            // Draw inner core cylinder (blanc-cyan clair solide)
            Color core = new Color(
                (byte)MathF.Min(255, Color.R + 100),
                (byte)MathF.Min(255, Color.G + 50),
                (byte)Color.B,
                (byte)255
            );
            DrawLaserCylinder(start, end, direction, distance, Thickness, core);
        }

        /// <summary>
        /// Draw a cylindrical laser beam between two points
        /// </summary>
        private void DrawLaserCylinder(Vector3 start, Vector3 end, Vector3 direction, float length, float radius, Color color)
        {
            // Create rotation matrix to align cylinder with laser direction
            Vector3 up = MathF.Abs(Vector3.Dot(direction, Vector3.UnitY)) > 0.99f
                ? Vector3.UnitX
                : Vector3.UnitY;
            Vector3 right = Vector3.Normalize(Vector3.Cross(up, direction));
            Vector3 forward = Vector3.Cross(direction, right);

            // Draw cylinder as segments
            int segments = 8;
            float angleStep = MathF.PI * 2 / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;

                // Calculate vertices
                Vector3 offset1 = (MathF.Cos(angle1) * right + MathF.Sin(angle1) * forward) * radius;
                Vector3 offset2 = (MathF.Cos(angle2) * right + MathF.Sin(angle2) * forward) * radius;

                Vector3 v1 = start + offset1;
                Vector3 v2 = start + offset2;
                Vector3 v3 = end + offset2;
                Vector3 v4 = end + offset1;

                // Draw quad as two triangles
                Raylib.DrawTriangle3D(v1, v2, v3, color);
                Raylib.DrawTriangle3D(v1, v3, v4, color);
            }

            // Draw end caps
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;

                Vector3 offset1 = (MathF.Cos(angle1) * right + MathF.Sin(angle1) * forward) * radius;
                Vector3 offset2 = (MathF.Cos(angle2) * right + MathF.Sin(angle2) * forward) * radius;

                // Start cap
                Raylib.DrawTriangle3D(start, start + offset1, start + offset2, color);

                // End cap
                Raylib.DrawTriangle3D(end, end + offset2, end + offset1, color);
            }
        }
    }
}
