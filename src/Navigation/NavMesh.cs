using System;
using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components;

namespace DarkArmsProto.Navigation
{
    /// <summary>
    /// Simple grid-based NavMesh for enemy navigation.
    /// Divides the room into cells and marks walkable/unwalkable areas.
    /// </summary>
    public class NavMesh
    {
        public float CellSize { get; private set; }
        public Vector3 Origin { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private bool[,] walkable; // true = walkable, false = blocked
        private Vector3[,] cellCenters; // Pre-computed cell center positions

        public NavMesh(Vector3 origin, float width, float height, float cellSize = 1.0f)
        {
            this.Origin = origin;
            this.CellSize = cellSize;
            this.Width = (int)(width / cellSize);
            this.Height = (int)(height / cellSize);

            walkable = new bool[Width, Height];
            cellCenters = new Vector3[Width, Height];

            // Initialize all cells as walkable
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Height; z++)
                {
                    walkable[x, z] = true;
                    cellCenters[x, z] = GetCellCenter(x, z);
                }
            }
        }

        /// <summary>
        /// Build the navmesh by checking collisions with walls
        /// </summary>
        public void Build(List<ColliderComponent> wallColliders)
        {
            if (wallColliders == null)
                return;

            // For each cell, check if it's blocked by a wall
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Height; z++)
                {
                    Vector3 cellCenter = cellCenters[x, z];
                    bool blocked = IsCellBlocked(cellCenter, wallColliders);
                    walkable[x, z] = !blocked;
                }
            }
        }

        private bool IsCellBlocked(Vector3 position, List<ColliderComponent> wallColliders)
        {
            // Check if this cell overlaps with any wall collider
            // Use entity collision size for more realistic pathfinding
            // Average enemy size is about 1.0-1.35 units, so use 0.7 units as radius
            float checkRadius = 0.7f;

            // First, find the floor height at this position
            float floorY = position.Y;
            foreach (var wall in wallColliders)
            {
                if (wall == null)
                    continue;

                var (minWall, maxWall) = wall.GetBounds();

                // Check if it's a floor (horizontal surface)
                float wallHeight = maxWall.Y - minWall.Y;
                bool isHorizontal = wallHeight < 1.0f;

                if (isHorizontal)
                {
                    // Check if this floor is in our XZ position
                    bool overlapX =
                        position.X + checkRadius > minWall.X && position.X - checkRadius < maxWall.X;
                    bool overlapZ =
                        position.Z + checkRadius > minWall.Z && position.Z - checkRadius < maxWall.Z;

                    if (overlapX && overlapZ)
                    {
                        // This is a floor under/at our position
                        floorY = Math.Max(floorY, maxWall.Y);
                    }
                }
            }

            // Now check for obstacles at walking height above the floor
            float entityHeight = 2.0f; // Typical enemy height
            float minY = floorY; // Walking surface
            float maxY = floorY + entityHeight; // Head height

            foreach (var wall in wallColliders)
            {
                if (wall == null)
                    continue;

                var (minWall, maxWall) = wall.GetBounds();

                // Skip floors (already handled above)
                float wallHeight = maxWall.Y - minWall.Y;
                if (wallHeight < 1.0f)
                    continue;

                // Skip obstacles that are completely below the walking surface
                if (maxWall.Y < minY + 0.5f)
                    continue; // Below floor

                // Skip obstacles that are completely above entity head height
                if (minWall.Y > maxY)
                    continue; // Above head

                // Check AABB overlap in XZ plane
                bool overlapX =
                    position.X + checkRadius > minWall.X && position.X - checkRadius < maxWall.X;
                bool overlapZ =
                    position.Z + checkRadius > minWall.Z && position.Z - checkRadius < maxWall.Z;

                if (overlapX && overlapZ)
                {
                    return true; // Cell is blocked by a wall/obstacle
                }
            }

            return false;
        }

        /// <summary>
        /// Get a random walkable position in the navmesh
        /// </summary>
        public bool GetRandomWalkablePosition(Random random, out Vector3 position)
        {
            position = Vector3.Zero;

            // Find all walkable cells
            List<(int x, int z)> walkableCells = new List<(int, int)>();
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Height; z++)
                {
                    if (walkable[x, z])
                    {
                        walkableCells.Add((x, z));
                    }
                }
            }

            if (walkableCells.Count == 0)
                return false;

            // Pick a random walkable cell
            var (cellX, cellZ) = walkableCells[random.Next(walkableCells.Count)];
            position = cellCenters[cellX, cellZ];
            return true;
        }

        /// <summary>
        /// Get a random walkable position near a given position
        /// </summary>
        public bool GetRandomWalkablePositionNear(
            Vector3 origin,
            float maxDistance,
            Random random,
            out Vector3 position
        )
        {
            position = Vector3.Zero;

            // Convert origin to grid coordinates
            (int originX, int originZ) = WorldToGrid(origin);

            // Calculate search radius in cells
            int radiusCells = (int)(maxDistance / CellSize);

            // Find walkable cells within radius
            List<(int x, int z)> walkableCells = new List<(int, int)>();
            for (int dx = -radiusCells; dx <= radiusCells; dx++)
            {
                for (int dz = -radiusCells; dz <= radiusCells; dz++)
                {
                    int x = originX + dx;
                    int z = originZ + dz;

                    if (!IsValidCell(x, z))
                        continue;

                    if (!walkable[x, z])
                        continue;

                    // Check distance
                    Vector3 cellPos = cellCenters[x, z];
                    float dist = Vector3.Distance(origin, cellPos);
                    if (dist <= maxDistance)
                    {
                        walkableCells.Add((x, z));
                    }
                }
            }

            if (walkableCells.Count == 0)
                return false;

            // Pick a random walkable cell
            var (cellX, cellZ) = walkableCells[random.Next(walkableCells.Count)];
            position = cellCenters[cellX, cellZ];
            return true;
        }

        /// <summary>
        /// Check if a position is walkable
        /// </summary>
        public bool IsWalkable(Vector3 position)
        {
            (int x, int z) = WorldToGrid(position);
            if (!IsValidCell(x, z))
                return false;
            return walkable[x, z];
        }

        /// <summary>
        /// Get next position toward target using simple pathfinding
        /// Returns the current position if no valid path exists
        /// </summary>
        public Vector3 GetNextPositionToward(Vector3 from, Vector3 target)
        {
            (int fromX, int fromZ) = WorldToGrid(from);
            (int targetX, int targetZ) = WorldToGrid(target);

            if (!IsValidCell(fromX, fromZ))
                return from;

            // If target is invalid or we're already at target, just return current position
            if (!IsValidCell(targetX, targetZ) || (fromX == targetX && fromZ == targetZ))
                return from;

            // Calculate direction vector
            int dx = Math.Sign(targetX - fromX);
            int dz = Math.Sign(targetZ - fromZ);

            // List of moves to try, prioritized by distance to target
            List<(int x, int z, float score)> moves = new List<(int, int, float)>();

            // Try all 8 directions
            int[] dxOptions = { dx, dx, 0, 0, -dx, -dx, dx, -dx };
            int[] dzOptions = { dz, 0, dz, -dz, dz, 0, -dz, 0 };

            for (int i = 0; i < 8; i++)
            {
                int nextX = fromX + dxOptions[i];
                int nextZ = fromZ + dzOptions[i];

                // Skip invalid or blocked cells
                if (!IsValidCell(nextX, nextZ) || !walkable[nextX, nextZ])
                    continue;

                // Calculate score (lower is better) = distance to target
                float distToTarget = MathF.Sqrt(
                    (targetX - nextX) * (targetX - nextX) + (targetZ - nextZ) * (targetZ - nextZ)
                );

                moves.Add((nextX, nextZ, distToTarget));
            }

            // Sort by score (closest to target first)
            moves.Sort((a, b) => a.score.CompareTo(b.score));

            // Return best move if available
            if (moves.Count > 0)
            {
                var best = moves[0];
                return cellCenters[best.x, best.z];
            }

            // No valid moves - stay in place
            return from;
        }

        private (int x, int z) WorldToGrid(Vector3 worldPos)
        {
            int x = (int)((worldPos.X - Origin.X) / CellSize);
            int z = (int)((worldPos.Z - Origin.Z) / CellSize);
            return (x, z);
        }

        private Vector3 GetCellCenter(int x, int z)
        {
            return new Vector3(
                Origin.X + (x + 0.5f) * CellSize,
                Origin.Y,
                Origin.Z + (z + 0.5f) * CellSize
            );
        }

        private bool IsValidCell(int x, int z)
        {
            return x >= 0 && x < Width && z >= 0 && z < Height;
        }

        /// <summary>
        /// Debug: Render the navmesh
        /// </summary>
        public void Render()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Height; z++)
                {
                    Vector3 center = cellCenters[x, z];
                    Raylib_cs.Color color = walkable[x, z]
                        ? new Raylib_cs.Color(0, 255, 0, 50)
                        : new Raylib_cs.Color(255, 0, 0, 50);

                    // Draw a small cube at cell center
                    Raylib_cs.Raylib.DrawCube(center, CellSize * 0.8f, 0.1f, CellSize * 0.8f, color);
                }
            }
        }
    }
}
