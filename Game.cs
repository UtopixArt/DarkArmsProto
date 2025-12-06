using System;
using System.Numerics;
using DarkArmsProto.Audio;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
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
        private EnemySpawner enemySpawner = null!;
        private SoulManager soulManager = null!;
        private RoomManager roomManager = null!;
        private ParticleManager particleManager = null!;
        private LightManager lightManager = null!;

        // New refactored systems
        private CombatSystem combatSystem = null!;
        private ProjectileSystem projectileSystem = null!;
        private GameUI gameUI = null!;
        private MapEditor mapEditor = null!;
        private PhysicsSystem physicsSystem = null!;

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

            // Initialize physics system first
            physicsSystem = new PhysicsSystem
            {
                Gravity = new Vector3(0, -30f, 0), // Same as PlayerInputComponent gravity
            };

            // Create physics floor
            physicsSystem.CreateFloor();

            // Initialize room system and generate dungeon with physics
            roomManager = new RoomManager();
            roomManager.SetPhysicsSystem(physicsSystem);
            roomManager.GenerateDungeon();

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

            // Initialize managers
            particleManager = new ParticleManager();
            lightManager = new LightManager();
            lightManager.Initialize();
            soulManager = new SoulManager(weaponComp);
            soulManager.SetParticleManager(particleManager);
            enemySpawner = new EnemySpawner();
            enemySpawner.SetPhysicsSystem(physicsSystem);

            // Add BepuPhysics to player
            var physicsShape = new PhysicsShapeComponent();
            physicsShape.Initialize(physicsSystem);
            physicsShape.SetCapsule(0.4f, 1.6f); // radius, height
            player.AddComponent(physicsShape);

            var rigidbody = new RigidbodyComponent();
            rigidbody.Mass = 75f;
            rigidbody.IsKinematic = true; // Kinematic for precise FPS control
            rigidbody.LockRotationX = true;
            rigidbody.LockRotationY = true;
            rigidbody.LockRotationZ = true;
            rigidbody.Group = CollisionGroup.Player; // Set collision group
            rigidbody.Initialize(physicsSystem); // Initialize AFTER setting properties
            player.AddComponent(rigidbody); // Add to player FIRST so Owner is set
            rigidbody.CreateBody(physicsShape.GetShapeIndex(), physicsShape.GetEffectiveRadius());

            // Initialize new systems
            combatSystem = new CombatSystem(
                player,
                soulManager,
                particleManager,
                lightManager,
                roomManager
            );
            projectileSystem = new ProjectileSystem(player, particleManager, lightManager);
            projectileSystem.SetPhysicsSystem(physicsSystem);

            // Wire up explosion event
            projectileSystem.OnExplosion += combatSystem.TriggerExplosion;

            gameUI = new GameUI(player, roomManager);
            mapEditor = new MapEditor();
            mapEditor.SetLightManager(lightManager);

            roomManager.SetLightManager(lightManager);

            // Initialize rooms with enemies
            roomManager.InitializeRooms(
                enemySpawner,
                (pos, dir, dmg, type) =>
                {
                    projectileSystem.SpawnEnemyProjectile(pos, dir, dmg, type);
                }
            );
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

            // Update physics simulation
            physicsSystem.Update(deltaTime);

            // Update global camera reference
            var camComp = player.GetComponent<CameraComponent>();
            if (camComp != null)
            {
                GameCamera = camComp.Camera;
            }

            // Update enemies list for projectiles
            var enemies = roomManager.GetCurrentRoomEnemies();
            projectileSystem.SetEnemies(enemies);
            projectileSystem.SetWalls(roomManager.CurrentRoom.WallColliders);

            // Handle shooting via ProjectileSystem
            projectileSystem.HandleShooting();

            // Update projectiles
            projectileSystem.Update(deltaTime);

            // Process combat (projectile hits, enemy deaths, player damage)
            combatSystem.ProcessProjectileCollisions(
                projectileSystem.Projectiles,
                enemies,
                deltaTime
            );
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
            projectileSystem.Render();
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

                projectileSystem.RenderColliderDebug();
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

        public void Cleanup()
        {
            AudioManager.Instance.Cleanup();
            lightManager.Cleanup();
            physicsSystem.Dispose();
        }
    }
}
