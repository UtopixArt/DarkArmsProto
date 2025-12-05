using System;
using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components; // Nécessaire pour HealthComponent et ChaseAIComponent
using DarkArmsProto.Core;
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

            CreateWallColliders();
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

        public void SpawnEnemies(EnemySpawner spawner, int count)
        {
            InitialEnemyCount = count;
            Random rng = new Random();

            for (int i = 0; i < count; i++)
            {
                // 1. Calculer une position aléatoire dans la salle
                float range = GameConfig.RoomSize / 2f - 4f; // Marge de sécurité par rapport aux murs
                float offsetX = (float)(rng.NextDouble() * 2 - 1) * range;
                float offsetZ = (float)(rng.NextDouble() * 2 - 1) * range;
                Vector3 spawnPos = WorldPosition + new Vector3(offsetX, 0, offsetZ);

                // 2. Choisir un type d'ennemi
                SoulType soulType;
                if (Type == RoomType.Boss)
                {
                    soulType = SoulType.Demon;
                }
                else
                {
                    double roll = rng.NextDouble();
                    if (roll < 0.15) // 15% Beast
                        soulType = SoulType.Beast;
                    else if (roll < 0.60) // 45% Demon
                        soulType = SoulType.Demon;
                    else // 40% Undead
                        soulType = SoulType.Undead;
                }

                // 3. Spawner l'ennemi (GameObject)
                var enemy = spawner.SpawnEnemy(spawnPos, soulType);
                Enemies.Add(enemy);
            }
        }

        public void UpdateEnemies(float deltaTime, Vector3 playerPosition)
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
                var ai = enemy.GetComponent<ChaseAIComponent>();
                if (ai != null)
                {
                    ai.TargetPosition = playerPosition;
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

            Raylib_cs.Raylib.DrawPlane(
                WorldPosition,
                new Vector2(GameConfig.RoomSize, GameConfig.RoomSize),
                floorColor
            );
        }

        public void RenderDoors()
        {
            foreach (var door in Doors.Values)
                door.Render();
        }
    }
}
