using System;
using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.Factories;
using DarkArmsProto.Helpers;
using DarkArmsProto.World;

namespace DarkArmsProto.Systems
{
    /// <summary>
    /// Système responsable du spawn des ennemis dans les rooms
    /// Extrait de Room pour séparer les responsabilités
    /// </summary>
    public class EnemySpawnSystem
    {
        private Random rng = new Random();

        /// <summary>
        /// Spawn des ennemis dans une room selon le layout ou de manière procédurale
        /// </summary>
        public void SpawnEnemiesInRoom(
            Room room,
            EnemyFactory spawner,
            int count,
            Action<Vector3, Vector3, float, SoulType> onProjectileSpawn
        )
        {
            List<GameObject> spawnedEnemies = new List<GameObject>();

            // Collect all colliders (walls + interior objects) to pass to enemies
            List<ColliderComponent> allColliders = new List<ColliderComponent>(room.WallColliders);
            foreach (var obj in room.InteriorObjects)
            {
                var col = obj.GetComponent<ColliderComponent>();
                if (col != null)
                {
                    allColliders.Add(col);
                }
            }

            // Utiliser le layout si disponible
            if (room.LayoutSpawners.Count > 0)
            {
                SpawnFromLayout(room, spawner, onProjectileSpawn, spawnedEnemies, allColliders);
            }
            else
            {
                SpawnProcedural(room, spawner, count, onProjectileSpawn, spawnedEnemies, allColliders);
            }

            // Share enemy list for avoidance and assign NavMesh
            // IMPORTANT: Only assign NavMesh to enemies spawned in THIS room (spawnedEnemies)
            // NOT to all enemies in the game world!
            var allEnemies = GameWorld.Instance.GetAllEnemies();

            foreach (var enemy in spawnedEnemies)
            {
                var ai = enemy.GetComponent<EnemyAIComponent>();
                if (ai != null)
                {
                    ai.RoomEnemies = allEnemies; // Share list of all enemies for avoidance
                    ai.NavMesh = room.NavMesh; // Assign THIS room's NavMesh
                }
            }
        }

        private void SpawnFromLayout(
            Room room,
            EnemyFactory spawner,
            Action<Vector3, Vector3, float, SoulType> onProjectileSpawn,
            List<GameObject> spawnedEnemies,
            List<ColliderComponent> allColliders
        )
        {
            foreach (var s in room.LayoutSpawners)
            {
                Vector3 pos = room.WorldPosition + new Vector3(s.X, s.Y, s.Z);
                var enemy = spawner.SpawnEnemy(pos, (SoulType)s.Type, allColliders);

                var ai = enemy.GetComponent<EnemyAIComponent>();
                if (ai != null)
                {
                    ai.OnShoot += onProjectileSpawn;
                }

                // Enregistrer l'ennemi dans GameWorld
                GameWorld.Instance.Register(enemy, "Enemy");

                // S'abonner à l'event OnDeath pour notifier la room
                SubscribeToEnemyDeath(enemy, room);

                // Ajouter à la liste des ennemis spawnés
                spawnedEnemies.Add(enemy);
            }
        }

        private void SpawnProcedural(
            Room room,
            EnemyFactory spawner,
            int count,
            Action<Vector3, Vector3, float, SoulType> onProjectileSpawn,
            List<GameObject> spawnedEnemies,
            List<ColliderComponent> allColliders
        )
        {
            for (int i = 0; i < count; i++)
            {
                // 1. Trouver une position de spawn valide
                Vector3? spawnPos = FindValidSpawnPosition(room);
                if (!spawnPos.HasValue)
                    continue; // Skip this enemy if no valid pos found

                // 2. Choisir un type d'ennemi
                SoulType soulType = ChooseEnemyType(room.Type);

                // 3. Spawner l'ennemi avec les WallColliders
                var enemy = spawner.SpawnEnemy(spawnPos.Value, soulType, allColliders);

                // 4. Subscribe to projectile event
                var ai = enemy.GetComponent<EnemyAIComponent>();
                if (ai != null)
                {
                    ai.OnShoot += onProjectileSpawn;
                }

                // Enregistrer l'ennemi dans GameWorld
                GameWorld.Instance.Register(enemy, "Enemy");

                // S'abonner à l'event OnDeath pour notifier la room
                SubscribeToEnemyDeath(enemy, room);

                // Ajouter à la liste des ennemis spawnés
                spawnedEnemies.Add(enemy);
            }
        }

        private void SubscribeToEnemyDeath(GameObject enemy, Room room)
        {
            var health = enemy.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.OnDeath += () => room.OnEnemyDeath(enemy);
            }
        }

        private Vector3? FindValidSpawnPosition(Room room)
        {
            int attempts = 0;
            while (attempts < 20)
            {
                attempts++;

                Vector3 spawnPos;

                // 50% chance to spawn on a platform if available
                if (room.InteriorObjects.Count > 0 && rng.NextDouble() > 0.5)
                {
                    spawnPos = TrySpawnOnFloor(room);
                }
                else
                {
                    spawnPos = TrySpawnOnPlatform(room);
                }

                // Check if position is clear of obstacles
                if (IsPositionValid(spawnPos, room))
                {
                    return spawnPos;
                }
            }

            return null; // No valid position found
        }

        private Vector3 TrySpawnOnPlatform(Room room)
        {
            // Find the main floor Y first for reference
            float mainFloorY = room.WorldPosition.Y;
            foreach (var wall in room.WallColliders)
            {
                var (min, max) = wall.GetBounds();
                if (max.Y > mainFloorY && Math.Abs(max.Y - min.Y) < 2.0f)
                {
                    mainFloorY = Math.Max(mainFloorY, max.Y);
                }
            }

            // Try to find a valid platform (not a ceiling)
            int attempts = 0;
            while (attempts < 10)
            {
                attempts++;
                var platform = room.InteriorObjects[rng.Next(room.InteriorObjects.Count)];
                var collider = platform.GetComponent<ColliderComponent>();
                if (collider != null)
                {
                    var (min, max) = collider.GetBounds();

                    // Check if this is a horizontal platform (not too tall, likely a floor not a ceiling)
                    float height = max.Y - min.Y;
                    bool isHorizontalPlatform = height < 2.0f; // Platforms/floors are typically very thin

                    // Check if it's a floor-like surface (within reasonable height above main floor)
                    // Platforms should be between main floor and 10 units above it (avoid ceiling)
                    bool isFloorLevel = max.Y >= mainFloorY - 1.0f && max.Y <= mainFloorY + 10.0f;

                    if (isHorizontalPlatform && isFloorLevel)
                    {
                        float x = (float)(rng.NextDouble() * (max.X - min.X) + min.X);
                        float z = (float)(rng.NextDouble() * (max.Z - min.Z) + min.Z);
                        // Spawn slightly above platform, rigidbody will snap to ground
                        return new Vector3(x, max.Y + 1.0f, z);
                    }
                }
            }

            // Fallback to floor if no valid platform found
            return TrySpawnOnFloor(room);
        }

        private Vector3 TrySpawnOnFloor(Room room)
        {
            float range = GameConfig.RoomSize / 2f - 4f;
            float offsetX = (float)(rng.NextDouble() * 2 - 1) * range;
            float offsetZ = (float)(rng.NextDouble() * 2 - 1) * range;

            // Find the floor Y position by checking the highest floor collider
            float floorY = room.WorldPosition.Y;
            foreach (var wall in room.WallColliders)
            {
                var (min, max) = wall.GetBounds();
                // Check if this is a floor (horizontal surface with small height)
                if (max.Y > floorY && Math.Abs(max.Y - min.Y) < 2.0f)
                {
                    floorY = Math.Max(floorY, max.Y);
                }
            }

            // Spawn slightly above floor, rigidbody will snap to ground
            return new Vector3(
                room.WorldPosition.X + offsetX,
                floorY + 1.0f,
                room.WorldPosition.Z + offsetZ
            );
        }

        private bool IsPositionValid(Vector3 spawnPos, Room room)
        {
            // Use the largest enemy collider size (Beast: 1.35x1.55x1.35) to ensure all enemy types fit
            Vector3 checkHalfSize = new Vector3(1.35f, 1.55f, 1.35f);
            Vector3 checkCenter = spawnPos + new Vector3(0, checkHalfSize.Y, 0);

            var (checkMin, checkMax) = CollisionHelper.GetBounds(
                checkCenter,
                checkHalfSize,
                Vector3.Zero
            );

            // Lift slightly to avoid floor collision
            checkMin.Y += 0.2f;

            // Check wall collisions
            foreach (var wall in room.WallColliders)
            {
                var (wMin, wMax) = wall.GetBounds();
                if (CollisionHelper.CheckAABBCollision(checkMin, checkMax, wMin, wMax))
                {
                    return false;
                }
            }

            // Check interior object collisions
            foreach (var obj in room.InteriorObjects)
            {
                var objCol = obj.GetComponent<ColliderComponent>();
                if (objCol != null)
                {
                    var (oMin, oMax) = objCol.GetBounds();
                    if (CollisionHelper.CheckAABBCollision(checkMin, checkMax, oMin, oMax))
                    {
                        return false;
                    }
                }
            }

            // Check if position is walkable using NavMesh
            if (room.NavMesh != null && !room.NavMesh.IsWalkable(spawnPos))
            {
                return false;
            }

            return true;
        }

        private SoulType ChooseEnemyType(RoomType roomType)
        {
            if (roomType == RoomType.Boss)
            {
                return SoulType.Demon;
            }

            double roll = rng.NextDouble();
            if (roll < 0.33) // 33% Beast
                return SoulType.Beast;
            else if (roll < 0.70) // 37% Demon
                return SoulType.Demon;
            else // 30% Undead
                return SoulType.Undead;
        }
    }
}
