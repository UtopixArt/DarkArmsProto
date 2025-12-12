using System;
using System.Diagnostics;
using System.Numerics;
using DarkArmsProto.Audio;
using DarkArmsProto.Builders;
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

        // Refactored systems
        private CombatSystem combatSystem = null!;
        private CollisionSystem collisionSystem = null!;
        private ProjectileManager projectileManager = null!;
        private RenderSystem renderSystem = null!;
        private GameUI gameUI = null!;
        private MapEditor mapEditor = null!;

        public static Camera3D GameCamera;
        public static Texture2D WhiteTexture;

        public void Initialize()
        {
            // Initialize audio system
            AudioManager.Instance.Initialize();
            AudioManager.Instance.SetMasterVolume(0.5f);

            // Clear GameWorld and damage numbers for fresh start
            GameWorld.Instance.Clear();
            Systems.DamageNumberManager.Clear();

            // Create a 1x1 white texture for billboards
            Image img = Raylib.GenImageColor(1, 1, Color.White);
            WhiteTexture = Raylib.LoadTextureFromImage(img);
            Raylib.UnloadImage(img);

            // Initialize room system first to get start position
            roomManager = new RoomManager();
            roomManager.GenerateDungeon();

            // Create player using factory
            player = Factories.PlayerFactory.Create(
                roomManager.CurrentRoom.WorldPosition + new Vector3(0, 1.6f, 0),
                roomManager.CurrentRoom
            );

            // Register player to GameWorld
            GameWorld.Instance.Register(player, "Player");

            // Initialize managers
            particleManager = new ParticleManager();

            // Use Builder for LightManager
            lightManager = new LightManagerBuilder().WithAmbientLight(0.05f, 0.05f, 0.05f).Build();

            soulManager = new SoulManager(player.GetComponent<WeaponComponent>());
            soulManager.SetParticleManager(particleManager);
            enemySpawner = new EnemyFactory();

            // Initialize VFX Helper
            VFX.VFXHelper.Initialize(particleManager, lightManager);

            // Make SoulManager globally accessible for EnemyDeathComponent
            Components.EnemyDeathComponent.GlobalSoulManager = soulManager;

            // Initialize new systems
            combatSystem = new CombatSystem(player, roomManager);
            collisionSystem = new CollisionSystem();

            projectileManager = new ProjectileManager();
            projectileManager.SetWalls(roomManager.AllColliders);

            gameUI = new GameUI(player, roomManager);
            mapEditor = new MapEditor();
            mapEditor.SetLightManager(lightManager);
            mapEditor.SetParticleManager(particleManager);

            roomManager.SetLightManager(lightManager);
            roomManager.SetProjectileManager(projectileManager);

            // Inject shooting dependencies into PlayerInputComponent
            var playerInput = player.GetComponent<PlayerInputComponent>();
            if (playerInput != null)
            {
                playerInput.ProjectileManager = projectileManager;
                playerInput.ExplosionCallback = combatSystem.TriggerExplosion;
            }

            // Initialize new refactored systems
            renderSystem = new RenderSystem(
                player,
                roomManager,
                lightManager,
                particleManager,
                soulManager,
                projectileManager,
                gameUI,
                mapEditor,
                combatSystem
            );

            // Initialize rooms with enemies
            roomManager.InitializeRooms(
                enemySpawner,
                (pos, dir, dmg, type) =>
                {
                    SpawnEnemyProjectile(pos, dir, dmg, type);
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

            // Toggle collider debug with F3 (Always available)
            if (Raylib.IsKeyPressed(KeyboardKey.F3))
            {
                renderSystem.ShowColliderDebug = !renderSystem.ShowColliderDebug;
            }

            // Toggle NavMesh debug with F4 (Always available)
            if (Raylib.IsKeyPressed(KeyboardKey.F4))
            {
                renderSystem.ShowNavMesh = !renderSystem.ShowNavMesh;
            }

            if (mapEditor.IsActive)
            {
                mapEditor.Update(deltaTime);
                GameCamera = mapEditor.GetCamera();
                return; // Skip game update
            }

            // GameWorld met à jour TOUS les GameObjects (player, enemies, etc.)
            // PlayerInputComponent handles both movement and shooting in its Update()
            GameWorld.Instance.Update(deltaTime);

            // Update Projectile System
            projectileManager.Update(deltaTime);

            // Process collisions (automatic via CollisionSystem)
            collisionSystem.Update(deltaTime);

            // Update damage numbers (static manager)
            Systems.DamageNumberManager.Update(deltaTime);

            // Update room manager (gère l'IA des ennemis et les transitions)
            roomManager.Update(deltaTime, player);

            // Update souls and particles
            soulManager.Update(deltaTime, player.Position);
            particleManager.Update(deltaTime);
            lightManager.Update(deltaTime);
        }

        public void Render()
        {
            renderSystem.Render();
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

            // Muzzle flash VFX
            var mesh = projectile.GetComponent<MeshRendererComponent>();
            if (mesh != null)
            {
                VFX.VFXHelper.SpawnMuzzleFlash(position, mesh.Color);
            }

            // Register to GameWorld as enemy projectile
            projectileManager.SpawnProjectile(projectile, true);

            // Sound
            AudioManager.Instance.PlaySound(SoundType.Shoot, 0.2f);
        }

        public void Cleanup()
        {
            AudioManager.Instance.Cleanup();
            lightManager.Cleanup();
            renderSystem.Cleanup();
        }
    }
}
