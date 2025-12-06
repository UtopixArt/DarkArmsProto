using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.VFX;
using DarkArmsProto.World;
using Raylib_cs;

namespace DarkArmsProto.Systems
{
    /// <summary>
    /// Centralized rendering system. Handles all 3D and 2D rendering.
    /// Extracted from Game.cs to reduce complexity.
    /// </summary>
    public class RenderSystem
    {
        private GameObject player;
        private RoomManager roomManager;
        private LightManager lightManager;
        private ParticleManager particleManager;
        private SoulManager soulManager;
        private ProjectileSystem projectileSystem;
        private GameUI gameUI;
        private MapEditor mapEditor;
        private CombatSystem combatSystem;
        private bool showColliderDebug;

        public bool ShowColliderDebug
        {
            get => showColliderDebug;
            set => showColliderDebug = value;
        }

        public RenderSystem(
            GameObject player,
            RoomManager roomManager,
            LightManager lightManager,
            ParticleManager particleManager,
            SoulManager soulManager,
            ProjectileSystem projectileSystem,
            GameUI gameUI,
            MapEditor mapEditor,
            CombatSystem combatSystem
        )
        {
            this.player = player;
            this.roomManager = roomManager;
            this.lightManager = lightManager;
            this.particleManager = particleManager;
            this.soulManager = soulManager;
            this.projectileSystem = projectileSystem;
            this.gameUI = gameUI;
            this.mapEditor = mapEditor;
            this.combatSystem = combatSystem;
            this.showColliderDebug = true;
        }

        /// <summary>
        /// Main render method - handles everything
        /// </summary>
        public void Render()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            bool isEditor = mapEditor.IsActive;
            Camera3D renderCamera = GetRenderCamera(isEditor);

            // Update lighting and global camera
            lightManager.UpdateShader(renderCamera);
            Game.GameCamera = renderCamera;

            // 3D rendering
            Render3D(renderCamera, isEditor);

            // 2D UI rendering
            Render2D(renderCamera, isEditor);

            Raylib.EndDrawing();
        }

        private Camera3D GetRenderCamera(bool isEditor)
        {
            if (isEditor)
            {
                return mapEditor.GetCamera();
            }

            var cameraComp = player.GetComponent<CameraComponent>();
            if (cameraComp == null)
            {
                // Return a default camera if player camera not found
                return new Camera3D();
            }

            Camera3D camera = cameraComp.Camera;

            // Apply screen shake
            var screenShake = player.GetComponent<ScreenShakeComponent>();
            if (screenShake != null)
            {
                camera.Position += screenShake.ShakeOffset;
                camera.Target += screenShake.ShakeOffset;
            }

            return camera;
        }

        private void Render3D(Camera3D camera, bool isEditor)
        {
            Raylib.BeginMode3D(camera);

            // Render lit objects (rooms/walls)
            lightManager.SetShininess(6.0f);
            Raylib.BeginShaderMode(lightManager.LightingShader);
            roomManager.Render();
            Raylib.EndShaderMode();

            // Render unlit/emissive objects
            projectileSystem.Render();
            soulManager.Render();
            particleManager.Render();
            lightManager.Render(); // Dynamic lights (billboards)

            // Render weapon (only if not in editor)
            if (!isEditor)
            {
                RenderWeapon();
            }

            // Render debug colliders
            if (showColliderDebug || isEditor)
            {
                RenderDebugColliders();
            }

            Raylib.EndMode3D();
        }

        private void RenderWeapon()
        {
            lightManager.SetShininess(6.0f);
            Raylib.BeginShaderMode(lightManager.LightingShader);

            var weaponRender = player.GetComponent<WeaponRenderComponent>();
            weaponRender?.Render();

            Raylib.EndShaderMode();
        }

        private void RenderDebugColliders()
        {
            // Player collider
            var playerCollider = player.GetComponent<ColliderComponent>();
            playerCollider?.Render();

            // Enemy colliders
            var enemies = roomManager.GetCurrentRoomEnemies();
            foreach (var enemy in enemies)
            {
                enemy.GetComponent<ColliderComponent>()?.Render();
            }

            // Projectile colliders
            var playerProjectiles = GameWorld.Instance.FindAllWithTag("Projectile");
            var enemyProjectiles = GameWorld.Instance.FindAllWithTag("EnemyProjectile");

            foreach (var proj in playerProjectiles)
            {
                proj.GetComponent<ColliderComponent>()?.Render();
            }

            foreach (var proj in enemyProjectiles)
            {
                proj.GetComponent<ColliderComponent>()?.Render();
            }
        }

        private void Render2D(Camera3D camera, bool isEditor)
        {
            if (isEditor)
            {
                mapEditor.Render();
            }
            else
            {
                // Render damage numbers
                var cameraComp = player.GetComponent<CameraComponent>();
                if (cameraComp != null)
                {
                    gameUI.RenderDamageNumbers(DamageNumberManager.GetAll(), cameraComp.Camera);
                }

                // Render UI
                gameUI.RenderUI(combatSystem.Kills, showColliderDebug);
                gameUI.RenderEnemyHealthBars(roomManager.GetCurrentRoomEnemies());
            }
        }
    }
}
