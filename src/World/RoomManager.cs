using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.Factories;
using DarkArmsProto.Systems;
using DarkArmsProto.VFX;

namespace DarkArmsProto.World
{
    public class RoomManager
    {
        private Dictionary<Vector2, Room> rooms;
        private Room? currentRoom;
        private Random random;
        private LightManager? lightManager;
        private List<RoomLayout> roomTemplates = new List<RoomLayout>();
        private EnemySpawnSystem spawnSystem = new EnemySpawnSystem();

        public Room CurrentRoom => currentRoom!;
        public List<ColliderComponent> AllColliders;

        public Dictionary<Vector2, Room> GetAllRooms() => rooms;

        public RoomManager()
        {
            rooms = new Dictionary<Vector2, Room>();
            random = new Random();
            AllColliders = new List<ColliderComponent>();
            LoadTemplates();
        }

        private void LoadTemplates()
        {
            string path = Path.Combine("resources", "rooms");
            if (!Directory.Exists(path))
                return;

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var layout = JsonSerializer.Deserialize<RoomLayout>(json);
                    if (layout != null)
                    {
                        roomTemplates.Add(layout);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to load room template {file}: {e.Message}");
                }
            }
        }

        public void SetLightManager(LightManager lm)
        {
            this.lightManager = lm;
        }

        public void GenerateDungeon()
        {
            // Retry loop to ensure a good dungeon layout
            int attempts = 0;
            while (attempts < 10)
            {
                rooms.Clear();
                Vector2 startPos = Vector2.Zero;

                // Create starting room
                var startRoom = new Room(startPos, RoomType.Start);
                rooms[startPos] = startRoom;

                // Generate connected rooms - Increased to 20 rooms max
                GenerateRoomsRecursive(startRoom, 0, 20);
                SetRoomColliders(rooms);
                // Ensure we have enough rooms (e.g. at least 6)
                if (rooms.Count >= 6)
                    break;

                attempts++;
            }

            // Set current room
            if (rooms.ContainsKey(Vector2.Zero))
            {
                currentRoom = rooms[Vector2.Zero];
            }
            else
            {
                // Fallback
                var startRoom = new Room(Vector2.Zero, RoomType.Start);
                rooms[Vector2.Zero] = startRoom;
                currentRoom = startRoom;
            }

            currentRoom.OnEnter();

            // Connect rooms with doors
            ConnectRooms();
        }

        private void GenerateRoomsRecursive(Room parentRoom, int depth, int maxRooms)
        {
            // Increased depth limit to 10 to allow longer paths
            if (rooms.Count >= maxRooms || depth > 10)
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

                // 10% chance to skip (was 25%) - makes it denser
                if (random.NextDouble() > 0.9)
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

                // Apply template if available
                if (roomType == RoomType.Normal && roomTemplates.Count > 0)
                {
                    // 50% chance to use a template
                    if (random.NextDouble() < 0.5)
                    {
                        var template = roomTemplates[random.Next(roomTemplates.Count)];
                        newRoom.ApplyLayout(template);
                    }
                }

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

        public void SpawnEnemiesInRoom(
            Room room,
            EnemyFactory spawner,
            Action<Vector3, Vector3, float, SoulType> onProjectileSpawn
        )
        {
            if (
                room.Type == RoomType.Start
                || room.Type == RoomType.Treasure
                || room.Type == RoomType.Shop
            )
                return;

            int enemyCount =
                room.Type == RoomType.Boss
                    ? GameConfig.BossRoomEnemyCount
                    : random.Next(GameConfig.MinEnemiesPerRoom, GameConfig.MaxEnemiesPerRoom + 1);

            // Utiliser le nouveau système de spawn
            spawnSystem.SpawnEnemiesInRoom(room, spawner, enemyCount, onProjectileSpawn);

            // Initialiser le compteur d'ennemis de la room
            room.InitializeEnemyCount(
                room.LayoutSpawners.Count > 0 ? room.LayoutSpawners.Count : enemyCount
            );
        }

        public void Update(float deltaTime, GameObject player)
        {
            if (currentRoom == null)
                return;

            // Update enemy AI (target, wall colliders)
            //currentRoom.UpdateEnemyAI(player);

            // Check for room transitions
            CheckRoomTransition(player);
        }

        private void CheckRoomTransition(GameObject player)
        {
            if (currentRoom == null)
                return;

            foreach (var door in currentRoom.Doors.Values)
            {
                if (door.CanPass(player.Position))
                {
                    var nextRoom = door.GetDestinationRoom();
                    if (nextRoom != null)
                    {
                        TransitionToRoom(nextRoom, door.Direction, player);
                        break;
                    }
                }
            }
        }

        private void TransitionToRoom(Room newRoom, Direction entryDirection, GameObject player)
        {
            if (currentRoom == newRoom)
                return;

            // Exit current room
            currentRoom?.OnExit();

            // Enter new room
            currentRoom = newRoom;
            currentRoom.OnEnter();

            // Update Lights
            if (lightManager != null)
            {
                lightManager.ClearStaticLights();
                foreach (var light in currentRoom.RoomLights)
                {
                    lightManager.AddStaticLight(
                        light.Position,
                        light.Color,
                        light.Intensity,
                        light.Radius,
                        light.Flicker
                    );
                }
            }

            // Determine arrival door (opposite of the one we entered)
            Direction arrivalDirection = entryDirection switch
            {
                Direction.North => Direction.South,
                Direction.South => Direction.North,
                Direction.East => Direction.West,
                Direction.West => Direction.East,
                _ => Direction.North,
            };

            // Teleport player to the arrival door of the new room
            if (newRoom.Doors.TryGetValue(arrivalDirection, out Door? arrivalDoor))
            {
                // Calculate push direction (same as movement direction)
                Vector3 moveDir = entryDirection switch
                {
                    Direction.North => new Vector3(0, 0, -1),
                    Direction.South => new Vector3(0, 0, 1),
                    Direction.East => new Vector3(1, 0, 0),
                    Direction.West => new Vector3(-1, 0, 0),
                    _ => Vector3.Zero,
                };

                // Teleport to door position + push into room
                float pushDistance = 4.0f; // Push safely away from door trigger
                Vector3 targetPos = arrivalDoor.Position + moveDir * pushDistance;
                targetPos.Y = player.Position.Y; // Keep player height
                player.Position = targetPos;
            }
            else
            {
                // Fallback: teleport to center if door not found
                Vector3 targetPos = newRoom.WorldPosition;
                targetPos.Y = player.Position.Y; // Keep player height
                player.Position = targetPos;
            }

            Console.WriteLine($"Entered {currentRoom.Type} room at {currentRoom.GridPosition}");

            // Update player room center for boundary checks
            var inputComp = player.GetComponent<PlayerInputComponent>();
            if (inputComp != null)
            {
                inputComp.RoomCenter = currentRoom.WorldPosition;
                inputComp.WallColliders = currentRoom.WallColliders;
            }
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
            currentRoom.RenderInterior();
            currentRoom.RenderDoors();

            // Get all enemies from GameWorld
            var enemies = GameWorld.Instance.GetAllEnemies();

            // Sort enemies by distance to camera (Back to Front) for proper transparency
            Vector3 camPos = Game.GameCamera.Position;
            enemies.Sort(
                (a, b) =>
                {
                    float distA = Vector3.DistanceSquared(a.Position, camPos);
                    float distB = Vector3.DistanceSquared(b.Position, camPos);
                    return distB.CompareTo(distA); // Descending order (far to near)
                }
            );

            // Render current room enemies
            foreach (var enemy in enemies)
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

        public List<GameObject> GetCurrentRoomEnemies()
        {
            return GameWorld.Instance.GetAllEnemies();
        }

        public void InitializeRooms(
            EnemyFactory spawner,
            Action<Vector3, Vector3, float, SoulType> onProjectileSpawn
        )
        {
            foreach (var room in rooms.Values)
            {
                if (room.Type != RoomType.Start)
                {
                    SpawnEnemiesInRoom(room, spawner, onProjectileSpawn);
                }
            }
        }

        private void SetRoomColliders(Dictionary<Vector2, Room> rooms)
        {
            AllColliders.Clear();

            foreach (var room in rooms.Values)
            {
                // AllColliders.AddRange(room.WallColliders);
                GameWorld.Instance.RegisterMultipleColliders(room.WallGameObjects, "Wall");
            }
        }
    }
}
