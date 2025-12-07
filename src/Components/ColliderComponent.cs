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

        public bool Raycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            out float hitDistance,
            out Vector3 hitNormal,
            out Vector3 hitPoint
        )
        {
            hitDistance = float.MaxValue;
            hitNormal = Vector3.Zero;
            hitPoint = Vector3.Zero;

            var (min, max) = GetBounds();
            const float eps = 1e-6f;
            float tMin = 0f,
                tMax = maxDistance;
            int hitAxis = -1,
                hitSide = 0;

            for (int axis = 0; axis < 3; axis++)
            {
                float o =
                    axis == 0 ? origin.X
                    : axis == 1 ? origin.Y
                    : origin.Z;
                float d =
                    axis == 0 ? direction.X
                    : axis == 1 ? direction.Y
                    : direction.Z;
                float mn =
                    axis == 0 ? min.X
                    : axis == 1 ? min.Y
                    : min.Z;
                float mx =
                    axis == 0 ? max.X
                    : axis == 1 ? max.Y
                    : max.Z;

                if (MathF.Abs(d) < eps)
                {
                    if (o < mn || o > mx)
                        return false;
                    continue;
                }

                float invD = 1f / d;
                float t1 = (mn - o) * invD;
                float t2 = (mx - o) * invD;
                int side = -1; // Default to hitting Min face (Normal -1)

                if (t1 > t2)
                {
                    (t1, t2) = (t2, t1);
                    side = 1; // Hitting Max face (Normal +1)
                }

                float prev = tMin;
                tMin = MathF.Max(tMin, t1);
                tMax = MathF.Min(tMax, t2);
                if (tMax < tMin)
                    return false;
                if (tMin != prev)
                {
                    hitAxis = axis;
                    hitSide = side;
                }
            }

            float tHit = tMin >= 0f ? tMin : tMax;
            if (tHit < 0f || tHit > maxDistance)
                return false;

            hitDistance = tHit;
            hitPoint = origin + direction * tHit;
            hitNormal = hitAxis switch
            {
                0 => hitSide < 0 ? -Vector3.UnitX : Vector3.UnitX,
                1 => hitSide < 0 ? -Vector3.UnitY : Vector3.UnitY,
                2 => hitSide < 0 ? -Vector3.UnitZ : Vector3.UnitZ,
                _ => Vector3.Zero,
            };
            return true;
        }
    }
}
