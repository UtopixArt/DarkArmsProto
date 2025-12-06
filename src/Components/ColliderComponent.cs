using System;
using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class ColliderComponent : Component
    {
        // Box collider using half-extents (size from center to edge)
        public Vector3 Size { get; set; } = new Vector3(0.5f, 0.5f, 0.5f);
        public Vector3 Offset { get; set; } = Vector3.Zero;
        public bool IsTrigger { get; set; } = false;
        public bool ShowDebug { get; set; } = true; // Show debug wireframe

        // Legacy radius support for backward compatibility
        public float Radius
        {
            get => Size.X;
            set => Size = new Vector3(value, value, value);
        }

        // Get the world-space bounds of this collider
        public (Vector3 min, Vector3 max) GetBounds()
        {
            Vector3 center = Owner.Position + Offset;
            Vector3 min = center - Size;
            Vector3 max = center + Size;
            return (min, max);
        }

        // AABB (Axis-Aligned Bounding Box) collision check
        public bool CheckCollision(ColliderComponent other)
        {
            if (other == null)
                return false;

            var (minA, maxA) = GetBounds();
            var (minB, maxB) = other.GetBounds();

            // Check overlap on all three axes
            bool overlapX = minA.X <= maxB.X && maxA.X >= minB.X;
            bool overlapY = minA.Y <= maxB.Y && maxA.Y >= minB.Y;
            bool overlapZ = minA.Z <= maxB.Z && maxA.Z >= minB.Z;

            return overlapX && overlapY && overlapZ;
        }

        // Check collision with a point (useful for projectiles)
        public bool CheckPointCollision(Vector3 point)
        {
            var (min, max) = GetBounds();
            return point.X >= min.X
                && point.X <= max.X
                && point.Y >= min.Y
                && point.Y <= max.Y
                && point.Z >= min.Z
                && point.Z <= max.Z;
        }

        // Get distance from point to box (0 if inside)
        public float DistanceToPoint(Vector3 point)
        {
            var (min, max) = GetBounds();
            Vector3 center = Owner.Position + Offset;

            // Closest point on box to the given point
            Vector3 closest = new Vector3(
                Math.Clamp(point.X, min.X, max.X),
                Math.Clamp(point.Y, min.Y, max.Y),
                Math.Clamp(point.Z, min.Z, max.Z)
            );

            return Vector3.Distance(point, closest);
        }

        public override void Render()
        {
            if (!ShowDebug)
                return;

            var (min, max) = GetBounds();
            Vector3 center = Owner.Position + Offset;
            Vector3 fullSize = Size * 2; // Convert half-extents to full size

            // Draw wireframe box
            Color debugColor = IsTrigger ? new Color(0, 255, 0, 150) : new Color(255, 0, 0, 150);
            Raylib.DrawCubeWiresV(center, fullSize, debugColor);
        }
    }
}
