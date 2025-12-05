using System.Collections.Generic;
using System.Numerics;
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
        private List<Enemy> enemies;
        private List<Projectile> projectiles;
        private List<DamageNumber> damageNumbers;

        // Game state
        private int kills = 0;
        private int currentRoom = 1;

        public Game()
        {
            enemies = new List<Enemy>();
            projectiles = new List<Projectile>();
        }

        public void Initialize()
        {
            // Initialize player
            player = new PlayerController(new Vector3(0, 1.6f, 0));

            // Initialize weapon system
            weaponSystem = new WeaponSystem(player);

            // Initialize soul manager
            soulManager = new SoulManager(weaponSystem);

            // Initialize enemy spawner
            enemySpawner = new EnemySpawner();

            // Initialize damage numbers for display on hits
            damageNumbers = new List<DamageNumber>();

            // Spawn initial enemies
            for (int i = 0; i < 5; i++)
            {
                enemies.Add(enemySpawner.SpawnEnemy());
            }
        }

        public void Update(float deltaTime)
        {
            // Update player
            player.Update(deltaTime);

            // Handle shooting
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                var newProjectiles = weaponSystem.Shoot(player.GetCamera());
                if (newProjectiles.Count > 0)
                {
                    projectiles.AddRange(newProjectiles);
                    player.AddScreenShake(0.05f);
                }
            }

            // Update weapon system
            weaponSystem.Update(deltaTime);

            // Update projectiles
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                projectiles[i].Update(deltaTime, enemies);

                // Check collision with enemies
                bool hit = false;
                for (int j = enemies.Count - 1; j >= 0; j--)
                {
                    if (projectiles[i].CheckCollision(enemies[j]))
                    {
                        enemies[j].TakeDamage(projectiles[i].Damage);

                        damageNumbers.Add(
                            new DamageNumber
                            {
                                Position = enemies[j].Position,
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
                        if (enemies[j].IsDead())
                        {
                            soulManager.SpawnSoul(enemies[j].Position, enemies[j].Type);
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
                            enemies.Add(enemySpawner.SpawnEnemy());
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

            // Update enemies
            foreach (var enemy in enemies)
            {
                enemy.Update(deltaTime, player.Position);

                // Check if touching player
                if (Vector3.Distance(enemy.Position, player.Position) < 1.5f)
                {
                    player.TakeDamage(10 * deltaTime);
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

            // Draw floor
            Raylib.DrawPlane(new Vector3(0, 0, 0), new Vector2(40, 40), new Color(30, 30, 30, 255));

            // Draw walls
            DrawWalls();

            // Draw enemies
            foreach (var enemy in enemies)
            {
                enemy.Render();
            }

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

        private void DrawWalls()
        {
            float roomSize = 20f;
            float wallHeight = 5f;
            Color wallColor = new Color(50, 50, 50, 255);

            // North wall
            Raylib.DrawCubeV(
                new Vector3(0, wallHeight / 2, -roomSize / 2),
                new Vector3(roomSize, wallHeight, 0.5f),
                wallColor
            );
            // South wall
            Raylib.DrawCubeV(
                new Vector3(0, wallHeight / 2, roomSize / 2),
                new Vector3(roomSize, wallHeight, 0.5f),
                wallColor
            );
            // West wall
            Raylib.DrawCubeV(
                new Vector3(-roomSize / 2, wallHeight / 2, 0),
                new Vector3(0.5f, wallHeight, roomSize),
                wallColor
            );
            // East wall
            Raylib.DrawCubeV(
                new Vector3(roomSize / 2, wallHeight / 2, 0),
                new Vector3(0.5f, wallHeight, roomSize),
                wallColor
            );
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
