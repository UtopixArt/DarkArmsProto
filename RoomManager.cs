using System;
using System.Collections.Generic;
using System.Numerics;

namespace DarkArmsProto
{
    public class RoomManager
    {
        private Dictionary<Vector2, Room> rooms;
        private Room? currentRoom;
        private Random random;

        public Room CurrentRoom => currentRoom!;

        public RoomManager()
        {
            rooms = new Dictionary<Vector2, Room>();
            random = new Random();
        }

        public void GenerateDungeon()
        {
            // Simple 3x3 grid for now
            // Layout:
            // [ ][ ][ ]
            // [ ][S][ ]
            // [ ][ ][ ]

            Vector2 startPos = new Vector2(1, 1); // Center

            // Create starting room
            var startRoom = new Room(startPos, RoomType.Start);
            rooms[startPos] = startRoom;
            currentRoom = startRoom;
            currentRoom.OnEnter();

            // Generate connected rooms
            GenerateRoomsRecursive(startRoom, 0, 5); // Max 5 rooms for now

            // Connect rooms with doors
            ConnectRooms();
        }

        private void GenerateRoomsRecursive(Room parentRoom, int depth, int maxRooms)
        {
            if (rooms.Count >= maxRooms || depth > 3)
                return;

            Vector2 parentPos = parentRoom.GridPosition;
            Direction[] directions =
            {
                Direction.North,
                Direction.South,
                Direction.East,
                Direction.West,
            };

            // Shuffle directions for variety
            Shuffle(directions);

            foreach (var dir in directions)
            {
                if (rooms.Count >= maxRooms)
                    break;

                Vector2 offset = GetDirectionOffset(dir);
                Vector2 newPos = parentPos + offset;

                // Check if room already exists
                if (rooms.ContainsKey(newPos))
                    continue;

                // 60% chance to create a room
                if (random.NextDouble() > 0.6)
                    continue;

                // Determine room type
                RoomType roomType = RoomType.Normal;
                if (rooms.Count == maxRooms - 1)
                {
                    roomType = RoomType.Boss; // Last room is boss
                }
                else if (random.NextDouble() < 0.2)
                {
                    roomType = random.NextDouble() < 0.5 ? RoomType.Treasure : RoomType.Shop;
                }

                // Create new room
                var newRoom = new Room(newPos, roomType);
                rooms[newPos] = newRoom;

                // Recurse
                GenerateRoomsRecursive(newRoom, depth + 1, maxRooms);
            }
        }

        private void ConnectRooms()
        {
            foreach (var room in rooms.Values)
            {
                Vector2 pos = room.GridPosition;
                Direction[] directions =
                {
                    Direction.North,
                    Direction.South,
                    Direction.East,
                    Direction.West,
                };

                foreach (var dir in directions)
                {
                    Vector2 offset = GetDirectionOffset(dir);
                    Vector2 neighborPos = pos + offset;

                    if (rooms.ContainsKey(neighborPos))
                    {
                        room.AddConnection(dir, rooms[neighborPos]);
                    }
                }
            }
        }

        private Vector2 GetDirectionOffset(Direction direction)
        {
            return direction switch
            {
                Direction.North => new Vector2(0, -1),
                Direction.South => new Vector2(0, 1),
                Direction.East => new Vector2(1, 0),
                Direction.West => new Vector2(-1, 0),
                _ => Vector2.Zero,
            };
        }

        private void Shuffle<T>(T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }

        public void SpawnEnemiesInRoom(Room room, EnemySpawner spawner)
        {
            if (
                room.Type == RoomType.Start
                || room.Type == RoomType.Treasure
                || room.Type == RoomType.Shop
            )
                return;

            int enemyCount = room.Type == RoomType.Boss ? 1 : random.Next(3, 7);
            room.SpawnEnemies(spawner, enemyCount);
        }

        public void Update(float deltaTime, Vector3 playerPosition)
        {
            if (currentRoom == null)
                return;

            // Update current room enemies
            currentRoom.UpdateEnemies(deltaTime, playerPosition);

            // Check for room transitions
            CheckRoomTransition(playerPosition);
        }

        private void CheckRoomTransition(Vector3 playerPosition)
        {
            if (currentRoom == null)
                return;

            foreach (var door in currentRoom.Doors.Values)
            {
                if (door.CanPass(playerPosition))
                {
                    var nextRoom = door.GetDestinationRoom();
                    if (nextRoom != null)
                    {
                        TransitionToRoom(nextRoom);
                        break;
                    }
                }
            }
        }

        private void TransitionToRoom(Room newRoom)
        {
            if (currentRoom == newRoom)
                return;

            // Exit current room
            currentRoom?.OnExit();

            // Enter new room
            currentRoom = newRoom;
            currentRoom.OnEnter();

            Console.WriteLine($"Entered {currentRoom.Type} room at {currentRoom.GridPosition}");
        }

        public Vector3 GetRoomCenterOffset(Vector3 playerPosition)
        {
            if (currentRoom == null)
                return Vector3.Zero;

            return currentRoom.WorldPosition - new Vector3(playerPosition.X, 0, playerPosition.Z);
        }

        public void Render()
        {
            if (currentRoom == null)
                return;

            // Render current room
            currentRoom.RenderFloor();
            currentRoom.RenderWalls();
            currentRoom.RenderDoors();

            // Render current room enemies
            foreach (var enemy in currentRoom.Enemies)
            {
                enemy.Render();
            }

            // Optional: Render adjacent rooms (for seamless feel)
            foreach (var connection in currentRoom.Connections.Values)
            {
                if (connection != null && connection.IsVisited)
                {
                    connection.RenderFloor();
                    connection.RenderWalls();
                }
            }
        }

        public List<Enemy> GetCurrentRoomEnemies()
        {
            return currentRoom?.Enemies ?? new List<Enemy>();
        }

        public void InitializeRooms(EnemySpawner spawner)
        {
            foreach (var room in rooms.Values)
            {
                if (room.Type != RoomType.Start)
                {
                    SpawnEnemiesInRoom(room, spawner);
                }
            }
        }
    }
}
