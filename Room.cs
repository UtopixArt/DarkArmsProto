using System.Collections.Generic;
using System.Numerics;

namespace DarkArmsProto
{
    public enum RoomType
    {
        Normal,
        Start,
        Boss,
        Treasure,
        Shop,
    }

    public enum Direction
    {
        North,
        South,
        East,
        West,
    }

    public class Room
    {
        public Vector2 GridPosition { get; private set; } // Position in grid (x, y)
        public Vector3 WorldPosition { get; private set; } // Actual 3D position
        public RoomType Type { get; private set; }
        public bool IsCleared { get; set; }
        public bool IsActive { get; set; }
        public bool IsVisited { get; set; }

        // Connections to other rooms
        public Dictionary<Direction, Room?> Connections { get; private set; }
        public Dictionary<Direction, Door> Doors { get; private set; }

        // Enemies in this room
        public List<Enemy> Enemies { get; private set; }
        public int InitialEnemyCount { get; private set; }

        private const float RoomWorldSize = 40f; // Size of each room in world units

        public Room(Vector2 gridPosition, RoomType type)
        {
            GridPosition = gridPosition;
            Type = type;
            IsCleared = type == RoomType.Start; // Starting room is already cleared
            IsActive = false;
            IsVisited = type == RoomType.Start;

            // Calculate world position from grid position
            WorldPosition = new Vector3(
                gridPosition.X * RoomWorldSize,
                0,
                gridPosition.Y * RoomWorldSize
            );

            Connections = new Dictionary<Direction, Room?>
            {
                { Direction.North, null },
                { Direction.South, null },
                { Direction.East, null },
                { Direction.West, null },
            };

            Doors = new Dictionary<Direction, Door>();
            Enemies = new List<Enemy>();
            InitialEnemyCount = 0;
        }

        public void AddConnection(Direction direction, Room otherRoom)
        {
            Connections[direction] = otherRoom;

            // Create door if it doesn't exist
            if (!Doors.ContainsKey(direction))
            {
                Vector3 doorPosition = GetDoorPosition(direction);
                Doors[direction] = new Door(doorPosition, direction, this);
            }
        }

        public bool HasConnection(Direction direction)
        {
            return Connections[direction] != null;
        }

        public Room? GetConnectedRoom(Direction direction)
        {
            return Connections[direction];
        }

        private Vector3 GetDoorPosition(Direction direction)
        {
            float halfSize = GameConfig.RoomSize / 2f;
            Vector3 offset = direction switch
            {
                Direction.North => new Vector3(0, 0, -halfSize),
                Direction.South => new Vector3(0, 0, halfSize),
                Direction.East => new Vector3(halfSize, 0, 0),
                Direction.West => new Vector3(-halfSize, 0, 0),
                _ => Vector3.Zero,
            };

            return WorldPosition + offset;
        }

        public void SpawnEnemies(EnemySpawner spawner, int count)
        {
            InitialEnemyCount = count;
            for (int i = 0; i < count; i++)
            {
                var enemy = spawner.SpawnEnemy();

                // Offset enemy position to be relative to room center
                var localPos = enemy.Position - new Vector3(0, enemy.Position.Y, 0);
                enemy.SetPosition(WorldPosition + localPos);

                Enemies.Add(enemy);
            }
        }

        public void UpdateEnemies(float deltaTime, Vector3 playerPosition)
        {
            // Remove dead enemies
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                if (Enemies[i].IsDead())
                {
                    Enemies.RemoveAt(i);
                }
            }

            // Update living enemies
            foreach (var enemy in Enemies)
            {
                enemy.Update(deltaTime, playerPosition);
            }

            // Check if room is cleared
            if (!IsCleared && Enemies.Count == 0 && InitialEnemyCount > 0)
            {
                IsCleared = true;
                UnlockDoors();
            }
        }

        public void LockDoors()
        {
            foreach (var door in Doors.Values)
            {
                door.Lock();
            }
        }

        public void UnlockDoors()
        {
            foreach (var door in Doors.Values)
            {
                door.Unlock();
            }
        }

        public void OnEnter()
        {
            IsActive = true;
            IsVisited = true;

            // Lock doors if room has enemies
            if (!IsCleared && Enemies.Count > 0)
            {
                LockDoors();
            }
        }

        public void OnExit()
        {
            IsActive = false;
        }

        public void RenderWalls()
        {
            float halfSize = GameConfig.RoomSize / 2f;
            float wallHeight = GameConfig.WallHeight;
            var wallColor = new Raylib_cs.Color(50, 50, 50, 255);
            var doorColor = new Raylib_cs.Color(80, 80, 80, 255);

            // Render each wall, with gaps for doors
            RenderWall(Direction.North, halfSize, wallHeight, wallColor, doorColor);
            RenderWall(Direction.South, halfSize, wallHeight, wallColor, doorColor);
            RenderWall(Direction.East, halfSize, wallHeight, wallColor, doorColor);
            RenderWall(Direction.West, halfSize, wallHeight, wallColor, doorColor);
        }

        private void RenderWall(
            Direction direction,
            float halfSize,
            float wallHeight,
            Raylib_cs.Color wallColor,
            Raylib_cs.Color doorColor
        )
        {
            bool hasDoor = HasConnection(direction);
            float doorWidth = 4f;

            Vector3 wallPos = WorldPosition + new Vector3(0, wallHeight / 2f, 0);

            switch (direction)
            {
                case Direction.North:
                    wallPos.Z -= halfSize;
                    if (hasDoor)
                    {
                        // Left segment
                        Raylib_cs.Raylib.DrawCubeV(
                            wallPos + new Vector3(-(halfSize - doorWidth / 2f) / 2f, 0, 0),
                            new Vector3(halfSize - doorWidth / 2f, wallHeight, 0.5f),
                            wallColor
                        );
                        // Right segment
                        Raylib_cs.Raylib.DrawCubeV(
                            wallPos + new Vector3((halfSize - doorWidth / 2f) / 2f, 0, 0),
                            new Vector3(halfSize - doorWidth / 2f, wallHeight, 0.5f),
                            wallColor
                        );
                    }
                    else
                    {
                        Raylib_cs.Raylib.DrawCubeV(
                            wallPos,
                            new Vector3(GameConfig.RoomSize, wallHeight, 0.5f),
                            wallColor
                        );
                    }
                    break;

                case Direction.South:
                    wallPos.Z += halfSize;
                    if (hasDoor)
                    {
                        Raylib_cs.Raylib.DrawCubeV(
                            wallPos + new Vector3(-(halfSize - doorWidth / 2f) / 2f, 0, 0),
                            new Vector3(halfSize - doorWidth / 2f, wallHeight, 0.5f),
                            wallColor
                        );
                        Raylib_cs.Raylib.DrawCubeV(
                            wallPos + new Vector3((halfSize - doorWidth / 2f) / 2f, 0, 0),
                            new Vector3(halfSize - doorWidth / 2f, wallHeight, 0.5f),
                            wallColor
                        );
                    }
                    else
                    {
                        Raylib_cs.Raylib.DrawCubeV(
                            wallPos,
                            new Vector3(GameConfig.RoomSize, wallHeight, 0.5f),
                            wallColor
                        );
                    }
                    break;

                case Direction.East:
                    wallPos.X += halfSize;
                    if (hasDoor)
                    {
                        Raylib_cs.Raylib.DrawCubeV(
                            wallPos + new Vector3(0, 0, -(halfSize - doorWidth / 2f) / 2f),
                            new Vector3(0.5f, wallHeight, halfSize - doorWidth / 2f),
                            wallColor
                        );
                        Raylib_cs.Raylib.DrawCubeV(
                            wallPos + new Vector3(0, 0, (halfSize - doorWidth / 2f) / 2f),
                            new Vector3(0.5f, wallHeight, halfSize - doorWidth / 2f),
                            wallColor
                        );
                    }
                    else
                    {
                        Raylib_cs.Raylib.DrawCubeV(
                            wallPos,
                            new Vector3(0.5f, wallHeight, GameConfig.RoomSize),
                            wallColor
                        );
                    }
                    break;

                case Direction.West:
                    wallPos.X -= halfSize;
                    if (hasDoor)
                    {
                        Raylib_cs.Raylib.DrawCubeV(
                            wallPos + new Vector3(0, 0, -(halfSize - doorWidth / 2f) / 2f),
                            new Vector3(0.5f, wallHeight, halfSize - doorWidth / 2f),
                            wallColor
                        );
                        Raylib_cs.Raylib.DrawCubeV(
                            wallPos + new Vector3(0, 0, (halfSize - doorWidth / 2f) / 2f),
                            new Vector3(0.5f, wallHeight, halfSize - doorWidth / 2f),
                            wallColor
                        );
                    }
                    else
                    {
                        Raylib_cs.Raylib.DrawCubeV(
                            wallPos,
                            new Vector3(0.5f, wallHeight, GameConfig.RoomSize),
                            wallColor
                        );
                    }
                    break;
            }
        }

        public void RenderFloor()
        {
            var floorColor = new Raylib_cs.Color(30, 30, 30, 255);

            // Different color for room types
            if (Type == RoomType.Boss)
                floorColor = new Raylib_cs.Color(50, 20, 20, 255);
            else if (Type == RoomType.Treasure)
                floorColor = new Raylib_cs.Color(50, 50, 20, 255);
            else if (Type == RoomType.Shop)
                floorColor = new Raylib_cs.Color(20, 50, 20, 255);

            Raylib_cs.Raylib.DrawPlane(
                WorldPosition,
                new Vector2(GameConfig.RoomSize, GameConfig.RoomSize),
                floorColor
            );
        }

        public void RenderDoors()
        {
            foreach (var door in Doors.Values)
            {
                door.Render();
            }
        }
    }
}
