using System;
using System.Numerics;
using DarkArmsProto.Core;
using DarkArmsProto.Helpers;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class ColliderComponent : Component
    {
        // Box collider using half-extents (size from center to edge)
        public Vector3 Size { get; set; } = new Vector3(0.5f, 0.5f, 0.5f);
        public Vector3 Offset { get; set; } = Vector3.Zero;
        public bool IsTrigger { get; set; } = false;
        public bool ShowDebug { get; set; } = false; // Show debug wireframe

        // Legacy radius support for backward compatibility
        public float Radius
        {
            get => Size.X;
            set => Size = new Vector3(value, value, value);
        }

        // Get the world-space bounds of this collider
        public (Vector3 min, Vector3 max) GetBounds()
        {
            return CollisionHelper.GetBounds(Owner.Position, Size, Offset);
        }

        // AABB (Axis-Aligned Bounding Box) collision check
        public bool CheckCollision(ColliderComponent other)
        {
            if (other == null)
                return false;

            var (minA, maxA) = GetBounds();
            var (minB, maxB) = other.GetBounds();

            return CollisionHelper.CheckAABBCollision(minA, maxA, minB, maxB);
        }

        // Check collision with a point (useful for projectiles)
        public bool CheckPointCollision(Vector3 point)
        {
            var (min, max) = GetBounds();
            return CollisionHelper.CheckPointInAABB(point, min, max);
        }

        // Get distance from point to box (0 if inside)
        public float DistanceToPoint(Vector3 point)
        {
            var (min, max) = GetBounds();
            return CollisionHelper.DistanceToAABB(point, min, max);
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

        public bool Raycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            out float hitDistance,
            out Vector3 hitNormal,
            out Vector3 hitPoint
        )
        {
            var (min, max) = GetBounds();
            return CollisionHelper.RaycastAABB(
                origin,
                direction,
                maxDistance,
                min,
                max,
                out hitDistance,
                out hitNormal,
                out hitPoint
            );
        }
    }
}
