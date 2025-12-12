using System;
using System.Numerics;
using Raylib_cs;

namespace DarkArmsProto.Helpers
{
    /// <summary>
    /// Static helper class for collision detection utilities.
    /// Centralizes collision logic for reuse across the codebase.
    /// </summary>
    public static class CollisionHelper
    {
        /// <summary>
        /// AABB (Axis-Aligned Bounding Box) collision check between two boxes
        /// </summary>
        /// <param name="minA">Minimum bounds of box A</param>
        /// <param name="maxA">Maximum bounds of box A</param>
        /// <param name="minB">Minimum bounds of box B</param>
        /// <param name="maxB">Maximum bounds of box B</param>
        /// <returns>True if boxes overlap</returns>
        public static bool CheckAABBCollision(Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB)
        {
            // Check overlap on all three axes
            bool overlapX = minA.X <= maxB.X && maxA.X >= minB.X;
            bool overlapY = minA.Y <= maxB.Y && maxA.Y >= minB.Y;
            bool overlapZ = minA.Z <= maxB.Z && maxA.Z >= minB.Z;

            return overlapX && overlapY && overlapZ;
        }

        /// <summary>
        /// Check if a point is inside an AABB
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <param name="min">Minimum bounds of box</param>
        /// <param name="max">Maximum bounds of box</param>
        /// <returns>True if point is inside the box</returns>
        public static bool CheckPointInAABB(Vector3 point, Vector3 min, Vector3 max)
        {
            return point.X >= min.X
                && point.X <= max.X
                && point.Y >= min.Y
                && point.Y <= max.Y
                && point.Z >= min.Z
                && point.Z <= max.Z;
        }

        /// <summary>
        /// Get distance from a point to an AABB (0 if inside)
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <param name="min">Minimum bounds of box</param>
        /// <param name="max">Maximum bounds of box</param>
        /// <returns>Distance to the box (0 if point is inside)</returns>
        public static float DistanceToAABB(Vector3 point, Vector3 min, Vector3 max)
        {
            // Closest point on box to the given point
            Vector3 closest = new Vector3(
                Math.Clamp(point.X, min.X, max.X),
                Math.Clamp(point.Y, min.Y, max.Y),
                Math.Clamp(point.Z, min.Z, max.Z)
            );

            return Vector3.Distance(point, closest);
        }

        /// <summary>
        /// Raycast against an AABB
        /// </summary>
        /// <param name="origin">Ray origin</param>
        /// <param name="direction">Ray direction (should be normalized)</param>
        /// <param name="maxDistance">Maximum ray distance</param>
        /// <param name="min">Minimum bounds of box</param>
        /// <param name="max">Maximum bounds of box</param>
        /// <param name="hitDistance">Distance to hit point</param>
        /// <param name="hitNormal">Normal of the hit surface</param>
        /// <param name="hitPoint">Point of intersection</param>
        /// <returns>True if ray hits the box</returns>
        public static bool RaycastAABB(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            Vector3 min,
            Vector3 max,
            out float hitDistance,
            out Vector3 hitNormal,
            out Vector3 hitPoint
        )
        {
            hitDistance = float.MaxValue;
            hitNormal = Vector3.Zero;
            hitPoint = Vector3.Zero;

            const float eps = 1e-6f;
            float tMin = 0f;
            float tMax = maxDistance;
            int hitAxis = -1;
            int hitSide = 0;

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

            // If hitAxis is still -1, the ray started inside or on the surface of the box
            // In this case, find which face we're exiting from based on the direction
            if (hitAxis == -1 && tMax > 0f)
            {
                // We're inside the box, find the exit face
                for (int axis = 0; axis < 3; axis++)
                {
                    float d = axis == 0 ? direction.X : axis == 1 ? direction.Y : direction.Z;
                    if (MathF.Abs(d) < eps) continue;

                    float o = axis == 0 ? origin.X : axis == 1 ? origin.Y : origin.Z;
                    float mn = axis == 0 ? min.X : axis == 1 ? min.Y : min.Z;
                    float mx = axis == 0 ? max.X : axis == 1 ? max.Y : max.Z;

                    float invD = 1f / d;
                    float t1 = (mn - o) * invD;
                    float t2 = (mx - o) * invD;

                    if (MathF.Abs(t2 - tMax) < eps) // Exiting through max face
                    {
                        hitAxis = axis;
                        hitSide = 1;
                        break;
                    }
                    else if (MathF.Abs(t1 - tMax) < eps) // Exiting through min face
                    {
                        hitAxis = axis;
                        hitSide = -1;
                        break;
                    }
                }
            }

            hitNormal = hitAxis switch
            {
                0 => hitSide < 0 ? -Vector3.UnitX : Vector3.UnitX,
                1 => hitSide < 0 ? -Vector3.UnitY : Vector3.UnitY,
                2 => hitSide < 0 ? -Vector3.UnitZ : Vector3.UnitZ,
                _ => Vector3.Zero,
            };
            return true;
        }

        /// <summary>
        /// Get the bounds of a box from center, size (half-extents), and offset
        /// </summary>
        /// <param name="position">Center position</param>
        /// <param name="size">Half-extents (size from center to edge)</param>
        /// <param name="offset">Offset from position</param>
        /// <returns>Min and max bounds</returns>
        public static (Vector3 min, Vector3 max) GetBounds(
            Vector3 position,
            Vector3 size,
            Vector3 offset
        )
        {
            Vector3 center = position + offset;
            Vector3 min = center - size;
            Vector3 max = center + size;
            return (min, max);
        }
    }
}
