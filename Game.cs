using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto
{
    public class Game
    {
        private struct DamageNumber
        {
            public Vector3 Position;
            public float Damage;
            public float Lifetime;
        }

        // Core systems
        private PlayerController player = null!;
        private WeaponSystem weaponSystem = null!;
        private EnemySpawner enemySpawner = null!;
        private SoulManager soulManager = null!;
        private RoomManager roomManager = null!;
        private List<GameObject> enemies;
        private List<Projectile> projectiles;
        private List<DamageNumber> damageNumbers;

        // Game state
        private int kills = 0;
        private int currentRoom = 1;

        public Game()
        {
            enemies = new List<GameObject>();
            projectiles = new List<Projectile>();
            damageNumbers = new List<DamageNumber>();
        }

        public void Initialize()
        {
            // Initialize room system first to get start position
            roomManager = new RoomManager();
            roomManager.GenerateDungeon();

            // Initialize player at start room position
            // Add height offset (1.6f) to avoid spawning in floor
            Vector3 startPos = roomManager.CurrentRoom.WorldPosition + new Vector3(0, 1.6f, 0);
            player = new PlayerController(startPos);

            // Initialize weapon system
            weaponSystem = new WeaponSystem(player);

            // Initialize soul manager
            soulManager = new SoulManager(weaponSystem);

            // Initialize enemy spawner
            enemySpawner = new EnemySpawner();

            // Initialize damage numbers
            damageNumbers = new List<DamageNumber>();

            // Initialize rooms with enemies
            roomManager.InitializeRooms(enemySpawner);

            // Get enemies from current room
            enemies = roomManager.GetCurrentRoomEnemies();
        }

        public void Update(float deltaTime)
        {
            // Update player
            player.Update(deltaTime, roomManager.CurrentRoom.WorldPosition);

            // Handle shooting
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                var newProjectiles = weaponSystem.Shoot(player.GetCamera());
                if (newProjectiles.Count > 0)
                {
                    projectiles.AddRange(newProjectiles);
                    player.AddScreenShake(GameConfig.ScreenShakeIntensity);
                }
            }

            // Update weapon system
            weaponSystem.Update(deltaTime);

            // Update projectiles
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                projectiles[i].Update(deltaTime, null!);

                // Check collision with enemies
                bool hit = false;
                for (int j = enemies.Count - 1; j >= 0; j--)
                {
                    var enemy = enemies[j];
                    if (Vector3.Distance(projectiles[i].Position, enemy.Position) < 1.0f)
                    {
                        var health = enemy.GetComponent<HealthComponent>();
                        if (health != null)
                        {
                            health.TakeDamage(projectiles[i].Damage);

                            damageNumbers.Add(
                                new DamageNumber
                                {
                                    Position = enemy.Position,
                                    Damage = projectiles[i].Damage,
                                    Lifetime = 1f,
                                }
                            );

                            // Apply special effects
                            if (projectiles[i].Lifesteal)
                            {
                                player.Heal(projectiles[i].Damage * 0.3f);
                            }

                            // Check if enemy died
                            if (health.IsDead)
                            {
                                var enemyComp = enemy.GetComponent<EnemyComponent>();
                                SoulType soulType =
                                    enemyComp != null ? enemyComp.Type : SoulType.Undead;

                                soulManager.SpawnSoul(enemy.Position, soulType);
                                for (int p = 0; p < 10; p++)
                                {
                                    var dir = new Vector3(
                                        (float)(Random.Shared.NextDouble() - 0.5),
                                        (float)Random.Shared.NextDouble(),
                                        (float)(Random.Shared.NextDouble() - 0.5)
                                    );
                                    // Tu peux ajouter un système de particles ici
                                }

                                enemies.RemoveAt(j);
                                kills++;

                                // Spawn new enemy
                                // enemies.Add(enemySpawner.SpawnEnemy());
                            }
                        }

                        if (!projectiles[i].Piercing)
                        {
                            hit = true;
                            break;
                        }
                    }
                }

                // Remove projectile if hit or out of bounds
                if (hit || projectiles[i].IsExpired())
                {
                    projectiles.RemoveAt(i);
                }
            }

            // Update room manager (handles enemies and transitions)
            roomManager.Update(deltaTime, player);
            enemies = roomManager.GetCurrentRoomEnemies();

            // Check if touching player
            foreach (var enemy in enemies)
            {
                if (
                    Vector3.Distance(enemy.Position, player.Position)
                    < GameConfig.EnemyCollisionRadius
                )
                {
                    player.TakeDamage(GameConfig.EnemyTouchDamagePerSecond * deltaTime);
                }
            }

            for (int i = damageNumbers.Count - 1; i >= 0; i--)
            {
                var dn = damageNumbers[i];
                dn.Lifetime -= deltaTime;
                dn.Position += new Vector3(0, deltaTime * 2, 0);
                damageNumbers[i] = dn;
                if (dn.Lifetime <= 0)
                    damageNumbers.RemoveAt(i);
            }

            // Update souls
            soulManager.Update(deltaTime, player.Position);
        }

        public void Render()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            // 3D rendering
            Raylib.BeginMode3D(player.GetCamera());

            // Draw rooms (floor, walls, doors)
            roomManager.Render();

            // Draw projectiles
            foreach (var projectile in projectiles)
            {
                projectile.Render();
            }

            // Draw souls
            soulManager.Render();

            Raylib.EndMode3D();

            foreach (var dn in damageNumbers)
            {
                var screenPos = Raylib.GetWorldToScreen(dn.Position, player.GetCamera());
                byte alpha = (byte)(dn.Lifetime * 255);
                Raylib.DrawText(
                    ((int)dn.Damage).ToString(),
                    (int)screenPos.X,
                    (int)screenPos.Y,
                    60,
                    new Color(Color.Yellow.R, Color.Yellow.G, Color.Yellow.B, alpha)
                );
            }

            // 2D UI
            RenderUI();

            Raylib.EndDrawing();
        }

        private void RenderUI()
        {
            // Stats panel
            Raylib.DrawRectangle(10, 10, 200, 100, new Color(0, 0, 0, 200));
            Raylib.DrawText($"HP: {(int)player.Health}/100", 20, 20, 20, Color.Green);
            Raylib.DrawText($"Kills: {kills}", 20, 45, 20, Color.White);
            Raylib.DrawText($"Room: {currentRoom}", 20, 70, 20, Color.White);

            // Weapon info
            weaponSystem.RenderUI();

            // Crosshair
            int centerX = Raylib.GetScreenWidth() / 2;
            int centerY = Raylib.GetScreenHeight() / 2;
            Raylib.DrawLine(centerX - 10, centerY, centerX + 10, centerY, Color.White);
            Raylib.DrawLine(centerX, centerY - 10, centerX, centerY + 10, Color.White);
        }

        public void Cleanup()
        {
            // Cleanup resources if needed
        }
    }
}
