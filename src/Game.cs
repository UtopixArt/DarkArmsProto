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

        // Refactored systems
        private CombatSystem combatSystem = null!;
        private CollisionSystem collisionSystem = null!;
        private ProjectileSystem projectileSystem = null!;
        private InputSystem inputSystem = null!;
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
            lightManager = new LightManager();
            lightManager.Initialize();
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
            projectileSystem = new ProjectileSystem();

            gameUI = new GameUI(player, roomManager);
            mapEditor = new MapEditor();
            mapEditor.SetLightManager(lightManager);

            roomManager.SetLightManager(lightManager);

            // Initialize new refactored systems
            inputSystem = new InputSystem(player, projectileSystem, combatSystem.TriggerExplosion);
            renderSystem = new RenderSystem(
                player,
                roomManager,
                lightManager,
                particleManager,
                soulManager,
                projectileSystem,
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

            if (mapEditor.IsActive)
            {
                mapEditor.Update(deltaTime);
                GameCamera = mapEditor.GetCamera();
                return; // Skip game update
            }

            // Toggle collider debug with F3
            if (Raylib.IsKeyPressed(KeyboardKey.F3))
            {
                renderSystem.ShowColliderDebug = !renderSystem.ShowColliderDebug;
            }

            // Update player
            player.Update(deltaTime);

            // Update global camera reference
            var camComp = player.GetComponent<CameraComponent>();
            if (camComp != null)
            {
                GameCamera = camComp.Camera;
            }

            // Update walls for projectiles
            projectileSystem.SetWalls(roomManager.CurrentRoom.WallColliders);

            // Handle player input (shooting, etc.)
            inputSystem.HandleInput(deltaTime);

            // Update projectiles
            projectileSystem.Update(deltaTime);

            // Process collisions (automatic via CollisionSystem)
            collisionSystem.Update(deltaTime);

            // Update damage numbers (static manager)
            Systems.DamageNumberManager.Update(deltaTime);

            // Update room manager (handles enemies and transitions)
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
            projectileSystem.SpawnProjectile(projectile, true);

            // Sound
            AudioManager.Instance.PlaySound(SoundType.Shoot, 0.2f);
        }

        public void Cleanup()
        {
            AudioManager.Instance.Cleanup();
            lightManager.Cleanup();
        }
    }
}
