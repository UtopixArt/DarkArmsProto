using System;
using System.Numerics;
using DarkArmsProto.Audio;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.VFX;
using Raylib_cs;

namespace DarkArmsProto.Systems
{
    /// <summary>
    /// Handles all player input (shooting, movement is in PlayerInputComponent).
    /// Centralizes input logic that was scattered in Game.cs.
    /// </summary>
    public class InputSystem
    {
        private GameObject player;
        private ProjectileSystem projectileSystem;
        private Action<Vector3, float, float> explosionCallback;

        public InputSystem(
            GameObject player,
            ProjectileSystem projectileSystem,
            Action<Vector3, float, float> explosionCallback
        )
        {
            this.player = player;
            this.projectileSystem = projectileSystem;
            this.explosionCallback = explosionCallback;
        }

        /// <summary>
        /// Handle all player input
        /// </summary>
        public void HandleInput(float deltaTime)
        {
            HandleShooting();
        }

        private void HandleShooting()
        {
            if (!Raylib.IsMouseButtonDown(MouseButton.Left))
                return;

            var cameraComp = player.GetComponent<CameraComponent>();
            var weaponComp = player.GetComponent<WeaponComponent>();

            if (cameraComp == null || weaponComp == null)
                return;

            var newProjectiles = weaponComp.TryShoot(cameraComp.Camera, explosionCallback);

            if (newProjectiles.Count == 0)
                return;

            // Setup projectile wall hit events
            foreach (var proj in newProjectiles)
            {
                var projComp = proj.GetComponent<ProjectileComponent>();
                if (projComp != null)
                {
                    projComp.OnWallHitEvent += (pos) =>
                    {
                        var mesh = proj.GetComponent<MeshRendererComponent>();
                        Color color = mesh != null ? mesh.Color : Color.Yellow;
                        VFXHelper.SpawnImpact(pos, color, 5);
                    };
                }
            }

            // Register projectiles to GameWorld
            projectileSystem.SpawnProjectiles(newProjectiles, false);

            // Shoot feedback
            AudioManager.Instance.PlaySound(SoundType.Shoot, 0.3f);

            // Screen shake
            var screenShake = player.GetComponent<ScreenShakeComponent>();
            if (screenShake != null)
            {
                screenShake.AddTrauma(GameConfig.ScreenShakeOnShoot);
            }

            // Muzzle flash VFX
            SpawnMuzzleFlash(newProjectiles[0], cameraComp);
        }

        private void SpawnMuzzleFlash(GameObject firstProjectile, CameraComponent cameraComp)
        {
            var projMesh = firstProjectile.GetComponent<MeshRendererComponent>();
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
                    + Vector3.Normalize(cameraComp.Camera.Target - cameraComp.Camera.Position)
                        * 0.5f;
            }

            VFXHelper.SpawnMuzzleFlash(muzzlePos, muzzleColor);
        }
    }
}
