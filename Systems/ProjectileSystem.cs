using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Audio;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.VFX;
using Raylib_cs;

namespace DarkArmsProto.Systems
{
    public class ProjectileSystem
    {
        private List<GameObject> projectiles;
        private GameObject player;
        private ParticleManager particleManager;
        private LightManager lightManager;
        private List<GameObject> enemies;
        private List<ColliderComponent> walls;

        public List<GameObject> Projectiles => projectiles;

        public ProjectileSystem(
            GameObject player,
            ParticleManager particleManager,
            LightManager lightManager
        )
        {
            this.projectiles = new List<GameObject>();
            this.player = player;
            this.particleManager = particleManager;
            this.lightManager = lightManager;
            this.enemies = new List<GameObject>();
            this.walls = new List<ColliderComponent>();
        }

        /// <summary>
        /// Update the enemies list reference for homing projectiles
        /// </summary>
        public void SetEnemies(List<GameObject> enemies)
        {
            this.enemies = enemies;
            ProjectileComponent.Enemies = enemies;
        }

        public void SetWalls(List<ColliderComponent> walls)
        {
            this.walls = walls;
        }

        /// <summary>
        /// Handle shooting input and create new projectiles
        /// </summary>
        public void HandleShooting()
        {
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                var cameraComp = player.GetComponent<CameraComponent>();
                var weaponComp = player.GetComponent<WeaponComponent>();

                if (cameraComp != null && weaponComp != null)
                {
                    var newProjectiles = weaponComp.TryShoot(cameraComp.Camera);
                    if (newProjectiles.Count > 0)
                    {
                        projectiles.AddRange(newProjectiles);

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

                        particleManager.SpawnImpact(muzzlePos, muzzleColor, 2); // Reduced from 15 to 5

                        // Muzzle flash light
                        lightManager.AddMuzzleFlash(muzzlePos, muzzleColor);
                    }
                }
            }
        }

        /// <summary>
        /// Spawns a projectile fired by an enemy
        /// </summary>
        public void SpawnEnemyProjectile(Vector3 position, Vector3 direction, float damage)
        {
            var projectile = new GameObject(position);

            // Projectile Component
            var projComp = new ProjectileComponent
            {
                Velocity = Vector3.Normalize(direction) * 20.0f, // Faster (was 15.0f)
                Damage = damage,
                Lifetime = 5.0f,
                IsEnemyProjectile = true,
            };
            projectile.AddComponent(projComp);

            // Visuals
            projectile.AddComponent(
                new MeshRendererComponent(Color.Red, new Vector3(0.4f, 0.4f, 0.4f)) // Slightly larger
            );

            // Light
            lightManager.AddMuzzleFlash(position, Color.Red);

            projectiles.Add(projectile);

            // Sound
            AudioManager.Instance.PlaySound(SoundType.Shoot, 0.2f);
        }

        /// <summary>
        /// Update all projectiles and remove inactive ones
        /// </summary>
        public void Update(float deltaTime)
        {
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

                // Check wall collisions
                if (walls != null)
                {
                    foreach (var wall in walls)
                    {
                        if (wall.CheckPointCollision(proj.Position))
                        {
                            // Hit wall
                            proj.IsActive = false;

                            // Visuals
                            var projMesh = proj.GetComponent<MeshRendererComponent>();
                            Color impactColor = projMesh != null ? projMesh.Color : Color.Yellow;
                            particleManager.SpawnImpact(proj.Position, impactColor, 5);
                            lightManager.AddImpactLight(proj.Position, impactColor);

                            // Sound
                            AudioManager.Instance.PlaySound(SoundType.Hit, 0.1f);

                            break;
                        }
                    }
                }

                if (!proj.IsActive)
                {
                    projectiles.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Render all projectiles
        /// </summary>
        public void Render()
        {
            foreach (var projectile in projectiles)
            {
                projectile.Render();
            }
        }

        /// <summary>
        /// Render projectile collider debug wireframes
        /// </summary>
        public void RenderColliderDebug()
        {
            foreach (var projectile in projectiles)
            {
                var projCollider = projectile.GetComponent<ColliderComponent>();
                if (projCollider != null)
                {
                    projCollider.Render();
                }
            }
        }
    }
}
