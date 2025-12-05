using System;
using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.World;
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
        private GameObject player = null!;
        private WeaponSystem weaponSystem = null!;
        private EnemySpawner enemySpawner = null!;
        private SoulManager soulManager = null!;
        private RoomManager roomManager = null!;
        private List<GameObject> enemies;
        private List<GameObject> projectiles;
        private List<DamageNumber> damageNumbers;

        // Game state
        private int kills = 0;
        private int currentRoom = 1;
        private bool showColliderDebug = true;

        public static Camera3D GameCamera;
        public static Texture2D WhiteTexture;

        public Game()
        {
            enemies = new List<GameObject>();
            projectiles = new List<GameObject>();
            damageNumbers = new List<DamageNumber>();
        }

        public void Initialize()
        {
            // Create a 1x1 white texture for billboards
            Image img = Raylib.GenImageColor(1, 1, Color.White);
            WhiteTexture = Raylib.LoadTextureFromImage(img);
            Raylib.UnloadImage(img);

            // Initialize room system first to get start position
            roomManager = new RoomManager();
            roomManager.GenerateDungeon();

            // Initialize player at start room position
            // Add height offset (1.6f) to avoid spawning in floor
            Vector3 startPos = roomManager.CurrentRoom.WorldPosition + new Vector3(0, 1.6f, 0);

            player = new GameObject(startPos);

            var inputComp = new PlayerInputComponent();
            inputComp.RoomCenter = roomManager.CurrentRoom.WorldPosition;
            player.AddComponent(inputComp);

            var cameraComp = new CameraComponent();
            player.AddComponent(cameraComp);

            var healthComp = new HealthComponent();
            healthComp.MaxHealth = 100;
            healthComp.CurrentHealth = 100;
            player.AddComponent(healthComp);

            var colliderComp = new ColliderComponent();
            colliderComp.Size = new Vector3(0.4f, 0.8f, 0.4f); // Box collider for player (slightly smaller than visual)
            player.AddComponent(colliderComp);

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
            // Toggle collider debug with F3
            if (Raylib.IsKeyPressed(KeyboardKey.F3))
            {
                showColliderDebug = !showColliderDebug;
            }

            // Update player
            player.Update(deltaTime);

            // Update global camera reference
            var camComp = player.GetComponent<CameraComponent>();
            if (camComp != null)
            {
                GameCamera = camComp.Camera;
            }

            // Update static enemies list for projectiles
            ProjectileComponent.Enemies = enemies;

            // Handle shooting
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                var cameraComp = player.GetComponent<CameraComponent>();
                if (cameraComp != null)
                {
                    var newProjectiles = weaponSystem.Shoot(cameraComp.Camera);
                    if (newProjectiles.Count > 0)
                    {
                        projectiles.AddRange(newProjectiles);
                        // Screen shake logic would go here if we had a ScreenShakeComponent
                    }
                }
            }

            // Update weapon system
            weaponSystem.Update(deltaTime);

            // Update projectiles
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var proj = projectiles[i];
                proj.Update(deltaTime);

                var projComp = proj.GetComponent<ProjectileComponent>();
                if (projComp == null || !proj.IsActive)
                {
                    projectiles.RemoveAt(i);
                    continue;
                }

                // Check collision with enemies using box colliders
                bool hit = false;
                for (int j = enemies.Count - 1; j >= 0; j--)
                {
                    var enemy = enemies[j];
                    var enemyCollider = enemy.GetComponent<ColliderComponent>();

                    // Check collision using box collider if available, otherwise fallback to distance
                    bool collision = false;
                    if (enemyCollider != null)
                    {
                        collision = enemyCollider.CheckPointCollision(proj.Position);
                    }
                    else
                    {
                        collision = Vector3.Distance(proj.Position, enemy.Position) < 1.0f;
                    }

                    if (collision)
                    {
                        var health = enemy.GetComponent<HealthComponent>();
                        if (health != null)
                        {
                            health.TakeDamage(projComp.Damage);

                            damageNumbers.Add(
                                new DamageNumber
                                {
                                    Position = enemy.Position,
                                    Damage = projComp.Damage,
                                    Lifetime = 1f,
                                }
                            );

                            // Apply special effects
                            if (projComp.Lifesteal)
                            {
                                var playerHealth = player.GetComponent<HealthComponent>();
                                if (playerHealth != null)
                                {
                                    playerHealth.Heal(projComp.Damage * 0.3f);
                                }
                            }

                            // Check if enemy died
                            if (health.IsDead)
                            {
                                var enemyComp = enemy.GetComponent<EnemyComponent>();
                                SoulType soulType =
                                    enemyComp != null ? enemyComp.Type : SoulType.Undead;

                                soulManager.SpawnSoul(enemy.Position, soulType);

                                enemies.RemoveAt(j);
                                kills++;
                            }
                        }

                        if (!projComp.Piercing)
                        {
                            hit = true;
                            break;
                        }
                    }
                }

                // Remove projectile if hit
                if (hit)
                {
                    projectiles.RemoveAt(i);
                }
            }

            // Update room manager (handles enemies and transitions)
            roomManager.Update(deltaTime, player);
            enemies = roomManager.GetCurrentRoomEnemies();

            // Check if touching player using box colliders
            var playerCollider = player.GetComponent<ColliderComponent>();
            foreach (var enemy in enemies)
            {
                bool collision = false;

                // Use box colliders if both have them
                var enemyCollider = enemy.GetComponent<ColliderComponent>();
                if (playerCollider != null && enemyCollider != null)
                {
                    collision = playerCollider.CheckCollision(enemyCollider);
                }
                else
                {
                    // Fallback to distance check
                    collision = Vector3.Distance(enemy.Position, player.Position)
                        < GameConfig.EnemyCollisionRadius;
                }

                if (collision)
                {
                    var playerHealth = player.GetComponent<HealthComponent>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(GameConfig.EnemyTouchDamagePerSecond * deltaTime);
                    }
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

            var cameraComp = player.GetComponent<CameraComponent>();
            if (cameraComp != null)
            {
                // 3D rendering
                Raylib.BeginMode3D(cameraComp.Camera);

                // Draw rooms (floor, walls, doors)
                roomManager.Render();

                // Draw projectiles
                foreach (var projectile in projectiles)
                {
                    projectile.Render();
                }

                // Draw souls
                soulManager.Render();

                // Draw collider debug wireframes
                if (showColliderDebug)
                {
                    // Player collider
                    var playerCollider = player.GetComponent<ColliderComponent>();
                    if (playerCollider != null)
                    {
                        playerCollider.Render();
                    }

                    // Enemy colliders
                    foreach (var enemy in enemies)
                    {
                        var enemyCollider = enemy.GetComponent<ColliderComponent>();
                        if (enemyCollider != null)
                        {
                            enemyCollider.Render();
                        }
                    }

                    // Projectile colliders
                    foreach (var projectile in projectiles)
                    {
                        var projCollider = projectile.GetComponent<ColliderComponent>();
                        if (projCollider != null)
                        {
                            projCollider.Render();
                        }
                    }
                }

                Raylib.EndMode3D();

                foreach (var dn in damageNumbers)
                {
                    var screenPos = Raylib.GetWorldToScreen(dn.Position, cameraComp.Camera);
                    byte alpha = (byte)(dn.Lifetime * 255);
                    Raylib.DrawText(
                        ((int)dn.Damage).ToString(),
                        (int)screenPos.X,
                        (int)screenPos.Y,
                        60,
                        new Color(Color.Yellow.R, Color.Yellow.G, Color.Yellow.B, alpha)
                    );
                }
            }

            // 2D UI
            RenderUI();

            // Draw health bars (after 3D mode)
            foreach (var enemy in enemies)
            {
                var healthBar = enemy.GetComponent<HealthBarComponent>();
                if (healthBar != null)
                {
                    healthBar.DrawUI();
                }
            }

            Raylib.EndDrawing();
        }

        private void RenderUI()
        {
            var healthComp = player.GetComponent<HealthComponent>();
            float currentHealth = healthComp != null ? healthComp.CurrentHealth : 0;

            // Stats panel
            Raylib.DrawRectangle(10, 10, 200, 100, new Color(0, 0, 0, 200));
            Raylib.DrawText($"HP: {(int)currentHealth}/100", 20, 20, 20, Color.Green);
            Raylib.DrawText($"Kills: {kills}", 20, 45, 20, Color.White);
            Raylib.DrawText($"Room: {currentRoom}", 20, 70, 20, Color.White);

            // Weapon info
            weaponSystem.RenderUI();

            // Crosshair
            int centerX = Raylib.GetScreenWidth() / 2;
            int centerY = Raylib.GetScreenHeight() / 2;
            Raylib.DrawLine(centerX - 10, centerY, centerX + 10, centerY, Color.White);
            Raylib.DrawLine(centerX, centerY - 10, centerX, centerY + 10, Color.White);

            // Debug indicator
            if (showColliderDebug)
            {
                Raylib.DrawText(
                    "[F3] Colliders: ON",
                    Raylib.GetScreenWidth() - 170,
                    10,
                    16,
                    Color.Green
                );
            }
            else
            {
                Raylib.DrawText(
                    "[F3] Colliders: OFF",
                    Raylib.GetScreenWidth() - 170,
                    10,
                    16,
                    Color.Gray
                );
            }
        }

        public void Cleanup()
        {
            // Cleanup resources if needed
        }
    }
}
