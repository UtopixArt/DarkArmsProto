using System;
using System.Numerics;
using DarkArmsProto.Audio;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.Factories;
using DarkArmsProto.Systems;
using DarkArmsProto.VFX;
using DarkArmsProto.World;
using Raylib_cs;

namespace DarkArmsProto
{
    public class Game
    {
        // Core systems
        private GameObject player = null!;
        private EnemyFactory enemySpawner = null!;
        private SoulManager soulManager = null!;
        private RoomManager roomManager = null!;
        private ParticleManager particleManager = null!;
        private LightManager lightManager = null!;

        // New refactored systems
        private CombatSystem combatSystem = null!;

        // private ProjectileSystem projectileSystem = null!; // Removed
        private System.Collections.Generic.List<GameObject> projectiles =
            new System.Collections.Generic.List<GameObject>(); // Added
        private GameUI gameUI = null!;
        private MapEditor mapEditor = null!;

        // Game state
        private bool showColliderDebug = true;

        public static Camera3D GameCamera;
        public static Texture2D WhiteTexture;

        public void Initialize()
        {
            // Initialize audio system
            AudioManager.Instance.Initialize();
            AudioManager.Instance.SetMasterVolume(0.5f);

            // Create a 1x1 white texture for billboards
            Image img = Raylib.GenImageColor(1, 1, Color.White);
            WhiteTexture = Raylib.LoadTextureFromImage(img);
            Raylib.UnloadImage(img);

            // Initialize room system first to get start position
            roomManager = new RoomManager();
            roomManager.GenerateDungeon();
            player = CreatePlayer();

            // Initialize managers
            particleManager = new ParticleManager();
            lightManager = new LightManager();
            lightManager.Initialize();
            soulManager = new SoulManager(player.GetComponent<WeaponComponent>());
            soulManager.SetParticleManager(particleManager);
            enemySpawner = new EnemyFactory();

            // Initialize new systems
            combatSystem = new CombatSystem(
                player,
                soulManager,
                particleManager,
                lightManager,
                roomManager
            );
            // projectileSystem = new ProjectileSystem(player, particleManager, lightManager); // Removed

            // Wire up explosion event
            // projectileSystem.OnExplosion += combatSystem.TriggerExplosion; // Moved to local handler

            gameUI = new GameUI(player, roomManager);
            mapEditor = new MapEditor();
            mapEditor.SetLightManager(lightManager);

            roomManager.SetLightManager(lightManager);

            // Initialize rooms with enemies
            roomManager.InitializeRooms(
                enemySpawner,
                (pos, dir, dmg, type) =>
                {
                    SpawnEnemyProjectile(pos, dir, dmg, type);
                }
            );
        }

        private GameObject CreatePlayer()
        {
            // Initialize player at start room position
            Vector3 startPos = roomManager.CurrentRoom.WorldPosition + new Vector3(0, 1.6f, 0);
            player = new GameObject(startPos);

            var inputComp = new PlayerInputComponent();
            inputComp.RoomCenter = roomManager.CurrentRoom.WorldPosition;
            inputComp.WallColliders = roomManager.CurrentRoom.WallColliders;
            player.AddComponent(inputComp);

            var cameraComp = new CameraComponent();
            player.AddComponent(cameraComp);

            var healthComp = new HealthComponent();
            healthComp.MaxHealth = GameConfig.PlayerMaxHealth;
            healthComp.CurrentHealth = GameConfig.PlayerMaxHealth;
            player.AddComponent(healthComp);

            // Handle player damage feedback
            healthComp.OnDamageTaken += (amount) =>
            {
                // Play hit sound (maybe different for player?)
                AudioManager.Instance.PlaySound(SoundType.Hit, 0.5f);

                // Screen shake
                var shake = player.GetComponent<ScreenShakeComponent>();
                shake?.AddTrauma(0.5f);
            };

            var colliderComp = new ColliderComponent();
            colliderComp.Size = new Vector3(
                GameConfig.PlayerColliderWidth,
                GameConfig.PlayerColliderHeight,
                GameConfig.PlayerColliderDepth
            );
            player.AddComponent(colliderComp);

            var screenShake = new ScreenShakeComponent();
            player.AddComponent(screenShake);

            var weaponComp = new WeaponComponent();
            player.AddComponent(weaponComp);

            var weaponRender = new WeaponRenderComponent();
            player.AddComponent(weaponRender);

            var weaponUI = new WeaponUIComponent();
            player.AddComponent(weaponUI);

            return player;
        }

        public void Update(float deltaTime)
        {
            // Toggle Editor
            if (Raylib.IsKeyPressed(KeyboardKey.F1))
            {
                var editorCamComp = player.GetComponent<CameraComponent>();
                if (editorCamComp != null)
                {
                    mapEditor.Toggle(roomManager.CurrentRoom, editorCamComp.Camera);
                }
            }

            if (mapEditor.IsActive)
            {
                mapEditor.Update(deltaTime);
                GameCamera = mapEditor.GetCamera();
                return; // Skip game update
            }

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

            // Update enemies list for projectiles (handled in UpdateProjectiles now)
            var enemies = roomManager.GetCurrentRoomEnemies();
            // projectileSystem.SetEnemies(enemies);
            // projectileSystem.SetWalls(roomManager.CurrentRoom.WallColliders);

            // Handle shooting
            HandleShooting();

            // Update projectiles
            UpdateProjectiles(deltaTime, enemies);

            // Process combat (projectile hits, enemy deaths, player damage)
            combatSystem.ProcessProjectileCollisions(projectiles, enemies, deltaTime);
            combatSystem.ProcessEnemyPlayerCollisions(enemies, deltaTime);
            combatSystem.UpdateDamageNumbers(deltaTime);

            // Update room manager (handles enemies and transitions)
            roomManager.Update(deltaTime, player);

            // Update souls and particles
            soulManager.Update(deltaTime, player.Position);
            particleManager.Update(deltaTime);
            lightManager.Update(deltaTime);
        }

        public void Render()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            Camera3D renderCamera;
            bool isEditor = mapEditor.IsActive;

            if (isEditor)
            {
                renderCamera = mapEditor.GetCamera();
            }
            else
            {
                var cameraComp = player.GetComponent<CameraComponent>();
                if (cameraComp != null)
                {
                    renderCamera = cameraComp.Camera;
                    // Apply screen shake to camera
                    var screenShake = player.GetComponent<ScreenShakeComponent>();
                    if (screenShake != null)
                    {
                        renderCamera.Position += screenShake.ShakeOffset;
                        renderCamera.Target += screenShake.ShakeOffset;
                    }
                }
                else
                {
                    Raylib.EndDrawing();
                    return;
                }
            }

            // Update lighting shader with camera position
            lightManager.UpdateShader(renderCamera);

            // Update static GameCamera for components that need it (like billboards)
            GameCamera = renderCamera;

            // 3D rendering
            Raylib.BeginMode3D(renderCamera);

            // Render lit objects
            lightManager.SetShininess(6.0f);
            Raylib.BeginShaderMode(lightManager.LightingShader);
            roomManager.Render();
            Raylib.EndShaderMode();

            // Render unlit/emissive objects
            // projectileSystem.Render(); // Replaced by loop
            foreach (var proj in projectiles)
            {
                proj.Render();
            }

            soulManager.Render();
            particleManager.Render();
            lightManager.Render(); // Render dynamic lights (billboards)

            // Render Weapon (only if not in editor, or maybe yes?)
            if (!isEditor)
            {
                lightManager.SetShininess(6.0f); // Broader highlights for better visibility
                Raylib.BeginShaderMode(lightManager.LightingShader);
                var weaponRender = player.GetComponent<WeaponRenderComponent>();
                weaponRender?.Render();
                Raylib.EndShaderMode();
            }

            // Draw collider debug wireframes
            if (showColliderDebug || isEditor)
            {
                var playerCollider = player.GetComponent<ColliderComponent>();
                playerCollider?.Render();

                var enemies = roomManager.GetCurrentRoomEnemies();
                foreach (var enemy in enemies)
                {
                    enemy.GetComponent<ColliderComponent>()?.Render();
                }

                foreach (var proj in projectiles)
                {
                    proj.GetComponent<ColliderComponent>()?.Render();
                }
            }

            Raylib.EndMode3D();

            // Render damage numbers
            if (!isEditor)
            {
                var cameraComp = player.GetComponent<CameraComponent>();
                if (cameraComp != null)
                    gameUI.RenderDamageNumbers(combatSystem.DamageNumbers, cameraComp.Camera);
            }

            // Editor Render (Gizmos & UI)
            if (isEditor)
            {
                mapEditor.Render();
            }
            else
            {
                // 2D UI
                gameUI.RenderUI(combatSystem.Kills, showColliderDebug);
                gameUI.RenderEnemyHealthBars(roomManager.GetCurrentRoomEnemies());
            }

            Raylib.EndDrawing();
        }

        private void HandleShooting()
        {
            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                var cameraComp = player.GetComponent<CameraComponent>();
                var weaponComp = player.GetComponent<WeaponComponent>();

                if (cameraComp != null && weaponComp != null)
                {
                    var newProjectiles = weaponComp.TryShoot(
                        cameraComp.Camera,
                        combatSystem.TriggerExplosion
                    );
                    if (newProjectiles.Count > 0)
                    {
                        foreach (var proj in newProjectiles)
                        {
                            // Setup projectile events
                            var projComp = proj.GetComponent<ProjectileComponent>();
                            if (projComp != null)
                            {
                                projComp.OnWallHitEvent += (pos) =>
                                {
                                    var mesh = proj.GetComponent<MeshRendererComponent>();
                                    Color color = mesh != null ? mesh.Color : Color.Yellow;
                                    particleManager.SpawnImpact(pos, color, 5);
                                    lightManager.AddImpactLight(pos, color);
                                    AudioManager.Instance.PlaySound(SoundType.Hit, 0.1f);
                                };
                            }
                            projectiles.Add(proj);
                        }

                        // Play shoot sound
                        AudioManager.Instance.PlaySound(SoundType.Shoot, 0.3f);

                        // Add screen shake on shoot
                        var screenShake = player.GetComponent<ScreenShakeComponent>();
                        if (screenShake != null)
                        {
                            screenShake.AddTrauma(GameConfig.ScreenShakeOnShoot);
                        }

                        // Muzzle flash particles at barrel
                        var projMesh = newProjectiles[0].GetComponent<MeshRendererComponent>();
                        Color muzzleColor = projMesh != null ? projMesh.Color : Color.Yellow;

                        Vector3 muzzlePos;
                        var weaponRender = player.GetComponent<WeaponRenderComponent>();
                        if (weaponRender != null)
                        {
                            muzzlePos = weaponRender.GetMuzzlePosition();
                        }
                        else
                        {
                            muzzlePos =
                                cameraComp.Camera.Position
                                + Vector3.Normalize(
                                    cameraComp.Camera.Target - cameraComp.Camera.Position
                                ) * 0.5f;
                        }

                        particleManager.SpawnImpact(muzzlePos, muzzleColor, 2);
                        lightManager.AddMuzzleFlash(muzzlePos, muzzleColor);
                    }
                }
            }
        }

        private void SpawnEnemyProjectile(
            Vector3 position,
            Vector3 direction,
            float damage,
            SoulType type
        )
        {
            var projectile = Factories.ProjectileFactory.CreateEnemyProjectile(
                position,
                direction,
                damage,
                type
            );

            // Light
            var mesh = projectile.GetComponent<MeshRendererComponent>();
            if (mesh != null)
            {
                lightManager.AddMuzzleFlash(position, mesh.Color);
            }

            projectiles.Add(projectile);

            // Sound
            AudioManager.Instance.PlaySound(SoundType.Shoot, 0.2f);
        }

        private void UpdateProjectiles(float deltaTime, List<GameObject> enemies)
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var proj = projectiles[i];
                var projComp = proj.GetComponent<ProjectileComponent>();

                if (projComp == null || !proj.IsActive)
                {
                    projectiles.RemoveAt(i);
                    continue;
                }

                // Update dependencies
                projComp.WallColliders = roomManager.CurrentRoom.WallColliders;

                // Update homing behavior if present
                foreach (var behavior in projComp.GetBehaviors())
                {
                    if (behavior is DarkArmsProto.Components.Behaviors.HomingBehavior homing)
                    {
                        homing.SetEnemies(enemies);
                    }
                }

                proj.Update(deltaTime);

                if (!proj.IsActive)
                {
                    projectiles.RemoveAt(i);
                }
            }
        }

        public void Cleanup()
        {
            AudioManager.Instance.Cleanup();
            lightManager.Cleanup();
        }
    }
}
