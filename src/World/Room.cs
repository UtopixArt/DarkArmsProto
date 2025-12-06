using System;
using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components; // Nécessaire pour HealthComponent et ChaseAIComponent
using DarkArmsProto.Core;
using DarkArmsProto.Factories;
using DarkArmsProto.VFX;
using Raylib_cs;

namespace DarkArmsProto.World
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
        public Vector2 GridPosition { get; private set; }
        public Vector3 WorldPosition { get; private set; }
        public RoomType Type { get; private set; }
        public bool IsCleared { get; set; }
        public bool IsActive { get; set; }
        public bool IsVisited { get; set; }

        public Dictionary<Direction, Room?> Connections { get; private set; }
        public Dictionary<Direction, Door> Doors { get; private set; }

        public List<GameObject> Enemies { get; private set; }
        public int InitialEnemyCount { get; private set; }

        // Wall colliders for physics
        public List<ColliderComponent> WallColliders { get; private set; }

        // Interior objects and lights
        public List<GameObject> InteriorObjects { get; private set; }
        public List<DynamicLight> RoomLights { get; private set; }

        private const float RoomWorldSize = 40f;

        public Room(Vector2 gridPosition, RoomType type)
        {
            GridPosition = gridPosition;
            Type = type;
            IsCleared = type == RoomType.Start;
            IsActive = false;
            IsVisited = type == RoomType.Start;

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
            Enemies = new List<GameObject>();
            InitialEnemyCount = 0;
            WallColliders = new List<ColliderComponent>();
            InteriorObjects = new List<GameObject>();
            RoomLights = new List<DynamicLight>();

            CreateWallColliders();
            GenerateInterior();
        }

        private void CreateWallColliders()
        {
            float halfSize = GameConfig.RoomSize / 2f;
            float wallHeight = GameConfig.WallHeight;
            float wallThickness = 0.5f;

            // North wall
            var northWall = new GameObject(
                WorldPosition + new Vector3(0, wallHeight / 2f, -halfSize)
            );
            var northCollider = new ColliderComponent
            {
                Size = new Vector3(halfSize, wallHeight / 2f, wallThickness / 2f),
                ShowDebug = false,
            };
            northWall.AddComponent(northCollider);
            WallColliders.Add(northCollider);

            // South wall
            var southWall = new GameObject(
                WorldPosition + new Vector3(0, wallHeight / 2f, halfSize)
            );
            var southCollider = new ColliderComponent
            {
                Size = new Vector3(halfSize, wallHeight / 2f, wallThickness / 2f),
                ShowDebug = false,
            };
            southWall.AddComponent(southCollider);
            WallColliders.Add(southCollider);

            // East wall
            var eastWall = new GameObject(
                WorldPosition + new Vector3(halfSize, wallHeight / 2f, 0)
            );
            var eastCollider = new ColliderComponent
            {
                Size = new Vector3(wallThickness / 2f, wallHeight / 2f, halfSize),
                ShowDebug = false,
            };
            eastWall.AddComponent(eastCollider);
            WallColliders.Add(eastCollider);

            // West wall
            var westWall = new GameObject(
                WorldPosition + new Vector3(-halfSize, wallHeight / 2f, 0)
            );
            var westCollider = new ColliderComponent
            {
                Size = new Vector3(wallThickness / 2f, wallHeight / 2f, halfSize),
                ShowDebug = false,
            };
            westWall.AddComponent(westCollider);
            WallColliders.Add(westCollider);

            // Floor Collider (Prevents enemies from falling/clipping through floor)
            var floorObj = new GameObject(WorldPosition + new Vector3(0, -0.5f, 0));
            var floorCollider = new ColliderComponent
            {
                Size = new Vector3(halfSize, 0.5f, halfSize),
                ShowDebug = false,
            };
            floorObj.AddComponent(floorCollider);
            WallColliders.Add(floorCollider);

            // Ceiling Collider (Prevents flying enemies from flying too high)
            var ceilingObj = new GameObject(WorldPosition + new Vector3(0, wallHeight + 0.5f, 0));
            var ceilingCollider = new ColliderComponent
            {
                Size = new Vector3(halfSize, 0.5f, halfSize),
                ShowDebug = false,
            };
            ceilingObj.AddComponent(ceilingCollider);
            WallColliders.Add(ceilingCollider);
        }

        public void AddConnection(Direction direction, Room otherRoom)
        {
            Connections[direction] = otherRoom;
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

        public List<SpawnerData> LayoutSpawners { get; private set; } = new List<SpawnerData>();

        public void ApplyLayout(RoomLayout layout)
        {
            InteriorObjects.Clear();
            WallColliders.Clear();
            RoomLights.Clear();

            // Re-create outer walls
            CreateWallColliders();

            // Apply Platforms
            foreach (var p in layout.Platforms)
            {
                AddPlatform(WorldPosition + new Vector3(p.X, p.Y, p.Z), new Vector3(p.W, p.H, p.D));
            }

            // Apply Lights
            foreach (var l in layout.Lights)
            {
                var color = new Color(l.R, l.G, l.B, (byte)255);
                var light = new DynamicLight
                {
                    Position = WorldPosition + new Vector3(l.X, l.Y, l.Z),
                    Color = color,
                    Intensity = l.Intensity,
                    Radius = 2.0f,
                };
                RoomLights.Add(light);
            }

            LayoutSpawners = layout.Spawners;
        }

        public void SpawnEnemies(
            EnemyFactory spawner,
            int count,
            Action<Vector3, Vector3, float, SoulType> onProjectileSpawn
        )
        {
            InitialEnemyCount = count;
            Random rng = new Random();

            if (LayoutSpawners.Count > 0)
            {
                InitialEnemyCount = LayoutSpawners.Count;
                foreach (var s in LayoutSpawners)
                {
                    Vector3 pos = WorldPosition + new Vector3(s.X, s.Y, s.Z);
                    var enemy = spawner.SpawnEnemy(pos, (SoulType)s.Type);

                    var ai = enemy.GetComponent<EnemyAIComponent>();
                    if (ai != null)
                    {
                        ai.OnShoot += onProjectileSpawn;
                    }

                    Enemies.Add(enemy);
                }
                return;
            }

            for (int i = 0; i < count; i++)
            {
                // 1. Choose a spawn surface (Floor or Platform)
                Vector3 spawnPos;

                // 50% chance to spawn on a platform if available
                if (InteriorObjects.Count > 0 && rng.NextDouble() > 0.5)
                {
                    var platform = InteriorObjects[rng.Next(InteriorObjects.Count)];
                    var collider = platform.GetComponent<ColliderComponent>();
                    if (collider != null)
                    {
                        // Spawn on top of the platform
                        var (min, max) = collider.GetBounds();
                        float x = (float)(rng.NextDouble() * (max.X - min.X) + min.X);
                        float z = (float)(rng.NextDouble() * (max.Z - min.Z) + min.Z);
                        spawnPos = new Vector3(x, max.Y, z); // Exact top surface
                    }
                    else
                    {
                        // Fallback to floor
                        float range = GameConfig.RoomSize / 2f - 4f;
                        float offsetX = (float)(rng.NextDouble() * 2 - 1) * range;
                        float offsetZ = (float)(rng.NextDouble() * 2 - 1) * range;
                        spawnPos = WorldPosition + new Vector3(offsetX, 0, offsetZ);
                    }
                }
                else
                {
                    // Spawn on floor
                    float range = GameConfig.RoomSize / 2f - 4f;
                    float offsetX = (float)(rng.NextDouble() * 2 - 1) * range;
                    float offsetZ = (float)(rng.NextDouble() * 2 - 1) * range;
                    spawnPos = WorldPosition + new Vector3(offsetX, 0, offsetZ);
                }

                // 2. Choisir un type d'ennemi
                SoulType soulType;
                if (Type == RoomType.Boss)
                {
                    soulType = SoulType.Demon;
                }
                else
                {
                    double roll = rng.NextDouble();
                    if (roll < 0.55) // 50% Beast
                        soulType = SoulType.Beast;
                    else if (roll < 0.70) // 45% Demon
                        soulType = SoulType.Demon;
                    else // 40% Undead
                        soulType = SoulType.Undead;
                }

                // 3. Spawner l'ennemi (GameObject)
                var enemy = spawner.SpawnEnemy(spawnPos, soulType);

                // 4. Subscribe to projectile event
                var ai = enemy.GetComponent<EnemyAIComponent>();
                if (ai != null)
                {
                    ai.OnShoot += onProjectileSpawn;
                }

                Enemies.Add(enemy);
            }
        }

        public void UpdateEnemies(float deltaTime, GameObject player)
        {
            // Nettoyage des ennemis morts
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                var health = Enemies[i].GetComponent<HealthComponent>();
                // On vérifie si le composant existe et si l'ennemi est mort
                if (health != null && health.IsDead)
                {
                    Enemies.RemoveAt(i);
                }
            }

            // Mise à jour des ennemis vivants
            foreach (var enemy in Enemies)
            {
                // Mise à jour de la cible pour l'IA
                var ai = enemy.GetComponent<EnemyAIComponent>();
                if (ai != null)
                {
                    ai.SetTarget(player);
                    ai.WallColliders = WallColliders;
                }

                // Update générique du GameObject
                enemy.Update(deltaTime);
            }

            // Vérification de la fin de salle
            if (!IsCleared && Enemies.Count == 0 && InitialEnemyCount > 0)
            {
                IsCleared = true;
                UnlockDoors();
            }
        }

        public void LockDoors()
        {
            foreach (var door in Doors.Values)
                door.Lock();
        }

        public void UnlockDoors()
        {
            foreach (var door in Doors.Values)
                door.Unlock();
        }

        public void OnEnter()
        {
            IsActive = true;
            IsVisited = true;
            if (!IsCleared && Enemies.Count > 0)
                LockDoors();
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
            // float doorWidth = 4f;
            Vector3 wallPos = WorldPosition + new Vector3(0, wallHeight / 2f, 0);

            // Logique de rendu des murs (inchangée pour la brièveté, mais nécessaire)
            // Je remets le code simplifié pour que ça compile, assurez-vous de garder votre logique de rendu existante si elle est plus complexe

            Vector3 size = Vector3.Zero;
            Vector3 pos = wallPos;

            switch (direction)
            {
                case Direction.North:
                    pos.Z -= halfSize;
                    size = new Vector3(GameConfig.RoomSize, wallHeight, 0.5f);
                    break;
                case Direction.South:
                    pos.Z += halfSize;
                    size = new Vector3(GameConfig.RoomSize, wallHeight, 0.5f);
                    break;
                case Direction.East:
                    pos.X += halfSize;
                    size = new Vector3(0.5f, wallHeight, GameConfig.RoomSize);
                    break;
                case Direction.West:
                    pos.X -= halfSize;
                    size = new Vector3(0.5f, wallHeight, GameConfig.RoomSize);
                    break;
            }

            if (!hasDoor)
            {
                Raylib_cs.Raylib.DrawCubeV(pos, size, wallColor);
            }
            else
            {
                // Dessiner deux segments pour laisser un trou pour la porte
                // (Simplification ici, reprenez votre code de rendu de mur détaillé si besoin)
                // Pour l'instant je dessine juste des piliers pour marquer la porte
                Raylib_cs.Raylib.DrawCubeV(
                    pos,
                    size * new Vector3(1, 0.3f, 1) + new Vector3(0, wallHeight / 2, 0),
                    wallColor
                ); // Linteau
            }
        }

        public void RenderFloor()
        {
            var floorColor = new Raylib_cs.Color(30, 30, 30, 255);
            if (Type == RoomType.Boss)
                floorColor = new Raylib_cs.Color(50, 20, 20, 255);
            else if (Type == RoomType.Treasure)
                floorColor = new Raylib_cs.Color(50, 50, 20, 255);
            else if (Type == RoomType.Shop)
                floorColor = new Raylib_cs.Color(20, 50, 20, 255);

            // Floor
            Raylib_cs.Raylib.DrawPlane(
                WorldPosition,
                new Vector2(GameConfig.RoomSize, GameConfig.RoomSize),
                floorColor
            );

            // Ceiling (Roof)
            // Draw a plane at WallHeight, but we need to rotate it to face down or just draw a cube
            // Drawing a thin cube for the ceiling is easier
            Raylib_cs.Raylib.DrawCube(
                WorldPosition + new Vector3(0, GameConfig.WallHeight, 0),
                GameConfig.RoomSize,
                0.1f,
                GameConfig.RoomSize,
                new Raylib_cs.Color(20, 20, 20, 255) // Dark ceiling
            );
        }

        private void GenerateInterior()
        {
            if (Type == RoomType.Start)
                return;

            Random rng = new Random(GridPosition.GetHashCode());
            int layoutType = rng.Next(0, 4); // 0: Random, 1: Catwalks, 2: Split Level, 3: Arena

            switch (layoutType)
            {
                case 0:
                    GenerateRandomBlocks(rng);
                    break;
                case 1:
                    GenerateCatwalks(rng);
                    break;
                case 2:
                    GenerateSplitLevel(rng);
                    break;
                case 3:
                    GenerateArena(rng);
                    break;
            }

            // Add Ambient Lights
            GenerateLights(rng);
        }

        private void AddPlatform(Vector3 position, Vector3 size)
        {
            var block = new GameObject(position);
            var collider = new ColliderComponent { Size = size / 2f, ShowDebug = false };
            block.AddComponent(collider);
            InteriorObjects.Add(block);
            WallColliders.Add(collider);
        }

        private void GenerateRandomBlocks(Random rng)
        {
            int numBlocks = rng.Next(3, 8);
            for (int i = 0; i < numBlocks; i++)
            {
                float w = rng.Next(2, 6);
                float h = rng.Next(1, 4);
                float d = rng.Next(2, 6);
                Vector3 size = new Vector3(w, h, d);

                float x = (float)(rng.NextDouble() * 20 - 10);
                float z = (float)(rng.NextDouble() * 20 - 10);
                Vector3 pos = WorldPosition + new Vector3(x, h / 2f, z);

                AddPlatform(pos, size);
            }
        }

        private void GenerateCatwalks(Random rng)
        {
            // Create a perimeter walkway at height 4
            float height = 4f;
            float width = 3f;
            float range = 10f;

            // North Walkway
            AddPlatform(
                WorldPosition + new Vector3(0, height, -range),
                new Vector3(range * 2, 0.5f, width)
            );
            // South Walkway
            AddPlatform(
                WorldPosition + new Vector3(0, height, range),
                new Vector3(range * 2, 0.5f, width)
            );
            // East Walkway
            AddPlatform(
                WorldPosition + new Vector3(range, height, 0),
                new Vector3(width, 0.5f, range * 2)
            );
            // West Walkway
            AddPlatform(
                WorldPosition + new Vector3(-range, height, 0),
                new Vector3(width, 0.5f, range * 2)
            );

            // Stairs to access
            GenerateStairs(WorldPosition + new Vector3(-5, 0, -5), height, Direction.North);
            GenerateStairs(WorldPosition + new Vector3(5, 0, 5), height, Direction.South);
        }

        private void GenerateSplitLevel(Random rng)
        {
            // Half the room is raised
            float height = 4f;

            // Large platform on West side
            AddPlatform(WorldPosition + new Vector3(-8, height, 0), new Vector3(10, 0.5f, 20));

            // Stairs
            GenerateStairs(WorldPosition + new Vector3(-2, 0, 0), height, Direction.West);

            // Some cover on top
            AddPlatform(WorldPosition + new Vector3(-10, height + 1, 5), new Vector3(2, 2, 2));
            AddPlatform(WorldPosition + new Vector3(-10, height + 1, -5), new Vector3(2, 2, 2));
        }

        private void GenerateArena(Random rng)
        {
            // Central platform
            float height = 3f;
            AddPlatform(WorldPosition + new Vector3(0, height, 0), new Vector3(10, 0.5f, 10));

            // Bridges to center
            AddPlatform(WorldPosition + new Vector3(0, height, -8), new Vector3(2, 0.5f, 6)); // North bridge
            AddPlatform(WorldPosition + new Vector3(0, height, 8), new Vector3(2, 0.5f, 6)); // South bridge

            // Pillars in corners
            AddPlatform(WorldPosition + new Vector3(-10, 2, -10), new Vector3(2, 4, 2));
            AddPlatform(WorldPosition + new Vector3(10, 2, -10), new Vector3(2, 4, 2));
            AddPlatform(WorldPosition + new Vector3(-10, 2, 10), new Vector3(2, 4, 2));
            AddPlatform(WorldPosition + new Vector3(10, 2, 10), new Vector3(2, 4, 2));
        }

        private void GenerateStairs(Vector3 startPos, float targetHeight, Direction dir)
        {
            int steps = 5;
            float stepHeight = targetHeight / steps;
            float stepDepth = 1.5f;
            float stepWidth = 3f;

            Vector3 offset = dir switch
            {
                Direction.North => new Vector3(0, 0, -stepDepth),
                Direction.South => new Vector3(0, 0, stepDepth),
                Direction.East => new Vector3(stepDepth, 0, 0),
                Direction.West => new Vector3(-stepDepth, 0, 0),
                _ => Vector3.Zero,
            };

            for (int i = 0; i < steps; i++)
            {
                Vector3 pos = startPos + offset * i + new Vector3(0, (i + 1) * stepHeight / 2f, 0);
                // Adjust Y to be center of block
                // Actually, if we want stairs to climb, we place blocks.
                // Block center Y = (i+1)*stepHeight / 2. Height = (i+1)*stepHeight.
                // This makes a solid ramp.
                // Let's make floating steps instead for style.

                Vector3 stepPos =
                    startPos + offset * i + new Vector3(0, i * stepHeight + stepHeight / 2, 0);
                Vector3 size =
                    dir == Direction.North || dir == Direction.South
                        ? new Vector3(stepWidth, stepHeight, stepDepth)
                        : new Vector3(stepDepth, stepHeight, stepWidth);

                AddPlatform(stepPos, size);
            }
        }

        private void GenerateLights(Random rng)
        {
            int numLights = rng.Next(0, 3); // Fewer lights (0 to 2)
            for (int i = 0; i < numLights; i++)
            {
                float x = (float)(rng.NextDouble() * 24 - 12);
                float z = (float)(rng.NextDouble() * 24 - 12);
                Vector3 pos = WorldPosition + new Vector3(x, 4, z);

                Color color = new Color(
                    rng.Next(50, 150), // Darker colors
                    rng.Next(20, 80),
                    rng.Next(20, 50),
                    255
                );

                RoomLights.Add(
                    new DynamicLight
                    {
                        Position = pos,
                        Color = color,
                        Intensity = 1.0f, // Reduced intensity (was 2.0)
                        Radius = 10.0f, // Reduced radius (was 15.0)
                        IsStatic = true,
                        Flicker = rng.NextDouble() > 0.7, // Occasional flicker
                    }
                );
            }
        }

        public void RenderInterior()
        {
            foreach (var obj in InteriorObjects)
            {
                var collider = obj.GetComponent<ColliderComponent>();
                if (collider != null)
                {
                    Vector3 size = collider.Size * 2;
                    Raylib.DrawCube(obj.Position, size.X, size.Y, size.Z, Color.DarkGray);
                    Raylib.DrawCubeWires(obj.Position, size.X, size.Y, size.Z, Color.Gray);
                }
            }
        }

        public void RenderDoors()
        {
            foreach (var door in Doors.Values)
                door.Render();
        }
    }
}
