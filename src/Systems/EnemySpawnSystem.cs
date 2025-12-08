using System;
using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.Factories;
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

            // Utiliser le layout si disponible
            if (room.LayoutSpawners.Count > 0)
            {
                SpawnFromLayout(room, spawner, onProjectileSpawn, spawnedEnemies);
            }
            else
            {
                SpawnProcedural(room, spawner, count, onProjectileSpawn, spawnedEnemies);
            }

            // IMPORTANT: Activer tous les ennemis APRÈS qu'ils soient complètement initialisés
            foreach (var enemy in spawnedEnemies)
            {
                enemy.IsActive = true;
            }

            // Share enemy list for avoidance
            var enemies = GameWorld.Instance.GetAllEnemies();
            foreach (var enemy in enemies)
            {
                var ai = enemy.GetComponent<EnemyAIComponent>();
                if (ai != null)
                {
                    ai.RoomEnemies = enemies;
                }
            }
        }

        private void SpawnFromLayout(
            Room room,
            EnemyFactory spawner,
            Action<Vector3, Vector3, float, SoulType> onProjectileSpawn,
            List<GameObject> spawnedEnemies
        )
        {
            foreach (var s in room.LayoutSpawners)
            {
                Vector3 pos = room.WorldPosition + new Vector3(s.X, s.Y, s.Z);
                var enemy = spawner.SpawnEnemy(pos, (SoulType)s.Type);

                // IMPORTANT: Désactiver l'ennemi temporairement pour éviter qu'il ne tombe pendant l'initialisation
                enemy.IsActive = false;

                var ai = enemy.GetComponent<EnemyAIComponent>();
                if (ai != null)
                {
                    ai.OnShoot += onProjectileSpawn;
                    // IMPORTANT: Assigner les WallColliders immédiatement pour éviter que l'ennemi tombe
                    ai.WallColliders = room.WallColliders;
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
            List<GameObject> spawnedEnemies
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

                // 3. Spawner l'ennemi
                var enemy = spawner.SpawnEnemy(spawnPos.Value, soulType);

                // IMPORTANT: Désactiver l'ennemi temporairement pour éviter qu'il ne tombe pendant l'initialisation
                enemy.IsActive = false;

                // 4. Subscribe to projectile event et assigner WallColliders
                var ai = enemy.GetComponent<EnemyAIComponent>();
                if (ai != null)
                {
                    ai.OnShoot += onProjectileSpawn;
                    // IMPORTANT: Assigner les WallColliders immédiatement pour éviter que l'ennemi tombe
                    ai.WallColliders = room.WallColliders;
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
                    spawnPos = TrySpawnOnPlatform(room);
                }
                else
                {
                    spawnPos = TrySpawnOnFloor(room);
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
            var platform = room.InteriorObjects[rng.Next(room.InteriorObjects.Count)];
            var collider = platform.GetComponent<ColliderComponent>();
            if (collider != null)
            {
                var (min, max) = collider.GetBounds();
                float x = (float)(rng.NextDouble() * (max.X - min.X) + min.X);
                float z = (float)(rng.NextDouble() * (max.Z - min.Z) + min.Z);
                return new Vector3(x, max.Y, z);
            }

            // Fallback to floor
            return TrySpawnOnFloor(room);
        }

        private Vector3 TrySpawnOnFloor(Room room)
        {
            float range = GameConfig.RoomSize / 2f - 4f;
            float offsetX = (float)(rng.NextDouble() * 2 - 1) * range;
            float offsetZ = (float)(rng.NextDouble() * 2 - 1) * range;

            // Spawn légèrement au-dessus du sol (1.0f) pour donner le temps à la gravité de détecter le floor
            // Sinon les ennemis peuvent tomber à travers le sol avant le premier raycast
            return room.WorldPosition + new Vector3(offsetX, 1.0f, offsetZ);
        }

        private bool IsPositionValid(Vector3 spawnPos, Room room)
        {
            Vector3 checkHalfSize = new Vector3(0.5f, 1.0f, 0.5f);
            Vector3 checkCenter = spawnPos + new Vector3(0, checkHalfSize.Y, 0);

            Vector3 checkMin = checkCenter - checkHalfSize;
            Vector3 checkMax = checkCenter + checkHalfSize;

            // Lift slightly to avoid floor collision
            checkMin.Y += 0.2f;

            // Check wall collisions
            foreach (var wall in room.WallColliders)
            {
                var (wMin, wMax) = wall.GetBounds();
                if (CheckAABB(checkMin, checkMax, wMin, wMax))
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
                    if (CheckAABB(checkMin, checkMax, oMin, oMax))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool CheckAABB(Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB)
        {
            return (minA.X <= maxB.X && maxA.X >= minB.X)
                && (minA.Y <= maxB.Y && maxA.Y >= minB.Y)
                && (minA.Z <= maxB.Z && maxA.Z >= minB.Z);
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
