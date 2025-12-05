using System;
using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class WeaponComponent : Component
    {
        // Weapon stats
        public string WeaponName { get; private set; }
        public int EvolutionStage { get; private set; }
        public float Damage { get; private set; }
        public float FireRate { get; private set; }

        // Soul tracking
        public Dictionary<SoulType, int> AbsorbedSouls { get; private set; }
        public int TotalSouls =>
            AbsorbedSouls[SoulType.Beast]
            + AbsorbedSouls[SoulType.Undead]
            + AbsorbedSouls[SoulType.Demon];

        // Firing
        private float lastShotTime;
        private bool canEvolve;
        private int[] requiredSouls =
        {
            GameConfig.RequiredSoulsStage2,
            GameConfig.RequiredSoulsStage3,
            GameConfig.RequiredSoulsStage4,
        };

        // Callbacks
        public Action<List<GameObject>>? OnShoot;

        public override void Start()
        {
            WeaponName = "Flesh Pistol";
            EvolutionStage = 1;
            Damage = GameConfig.BaseDamage;
            FireRate = GameConfig.BaseFireRate;
            lastShotTime = 0f;
            canEvolve = false;

            AbsorbedSouls = new Dictionary<SoulType, int>
            {
                { SoulType.Beast, 0 },
                { SoulType.Undead, 0 },
                { SoulType.Demon, 0 },
            };
        }

        public override void Update(float deltaTime)
        {
            lastShotTime += deltaTime;

            // Check for evolution input
            if (canEvolve && Raylib.IsKeyPressed(KeyboardKey.E))
            {
                Evolve();
            }
        }

        public void FeedSoul(SoulType soulType)
        {
            AbsorbedSouls[soulType]++;
            CheckEvolution();
        }

        private void CheckEvolution()
        {
            if (EvolutionStage > 3)
                return;

            int required = requiredSouls[EvolutionStage - 1];
            if (TotalSouls >= required && !canEvolve)
            {
                canEvolve = true;
                Console.WriteLine($"[EVOLUTION AVAILABLE] Press E to evolve!");
            }
        }

        private void Evolve()
        {
            SoulType dominant = GetDominantSoulType();

            string newName = "";
            float damageMult = 1f;
            float fireRateMult = 1f;

            if (EvolutionStage == 1)
            {
                switch (dominant)
                {
                    case SoulType.Beast:
                        newName = "Bone Revolver";
                        damageMult = GameConfig.BoneRevolverDamageMult;
                        fireRateMult = GameConfig.BoneRevolverFireRateMult;
                        break;
                    case SoulType.Undead:
                        newName = "Tendril Burst";
                        damageMult = GameConfig.TendrilBurstDamageMult;
                        fireRateMult = GameConfig.TendrilBurstFireRateMult;
                        break;
                    case SoulType.Demon:
                        newName = "Parasite Swarm";
                        damageMult = GameConfig.ParasiteSwarmDamageMult;
                        fireRateMult = GameConfig.ParasiteSwarmFireRateMult;
                        break;
                }
            }
            else if (EvolutionStage == 2)
            {
                switch (dominant)
                {
                    case SoulType.Beast:
                        newName = "Apex Predator";
                        damageMult = GameConfig.ApexPredatorDamageMult;
                        fireRateMult = GameConfig.ApexPredatorFireRateMult;
                        break;
                    case SoulType.Undead:
                        newName = "Necrotic Cannon";
                        damageMult = GameConfig.NecroticCannonDamageMult;
                        fireRateMult = GameConfig.NecroticCannonFireRateMult;
                        break;
                    case SoulType.Demon:
                        newName = "Inferno Beast";
                        damageMult = GameConfig.InfernoBeastDamageMult;
                        fireRateMult = GameConfig.InfernoBeastFireRateMult;
                        break;
                }
            }

            WeaponName = newName;
            Damage *= damageMult;
            FireRate *= fireRateMult;
            EvolutionStage++;
            canEvolve = false;

            Console.WriteLine($"[EVOLVED] {WeaponName}!");
        }

        public List<GameObject> TryShoot(Camera3D camera)
        {
            float timeSinceLastShot = lastShotTime;
            if (timeSinceLastShot < 1f / FireRate)
                return new List<GameObject>();

            lastShotTime = 0f;

            // Get spawn position from render component if available
            Vector3 spawnPos = camera.Position;
            var renderComp = Owner.GetComponent<WeaponRenderComponent>();
            if (renderComp != null)
            {
                spawnPos = renderComp.GetMuzzlePosition();
                renderComp.TriggerRecoil();
            }

            // Calculate direction towards the center of the screen (crosshair)
            // We cast a ray from camera center to find target point
            Vector3 cameraForward = Vector3.Normalize(camera.Target - camera.Position);
            Vector3 targetPoint = camera.Position + cameraForward * 50f; // Aim at 50 units distance

            // Direction from muzzle to target
            Vector3 direction = Vector3.Normalize(targetPoint - spawnPos);

            var projectiles = CreateProjectilesForWeapon(spawnPos, direction);

            OnShoot?.Invoke(projectiles);

            return projectiles;
        }

        private List<GameObject> CreateProjectilesForWeapon(Vector3 position, Vector3 direction)
        {
            var projectiles = new List<GameObject>();

            switch (WeaponName)
            {
                case "Bone Revolver":
                case "Apex Predator":
                    projectiles.Add(
                        CreateProjectile(
                            position,
                            direction,
                            Damage,
                            20f,
                            new Color(255, 170, 102, 255),
                            0.5f,
                            true,
                            false,
                            false
                        )
                    );
                    break;

                case "Tendril Burst":
                case "Necrotic Cannon":
                    for (int i = 0; i < 5; i++)
                    {
                        Vector3 spreadDir = direction;
                        float spread = (i - 2) * 0.15f;
                        spreadDir.X += spread;
                        spreadDir = Vector3.Normalize(spreadDir);

                        projectiles.Add(
                            CreateProjectile(
                                position,
                                spreadDir,
                                Damage * 0.6f,
                                15f,
                                new Color(0, 255, 102, 255),
                                0.25f,
                                false,
                                true,
                                false
                            )
                        );
                    }
                    break;

                case "Parasite Swarm":
                case "Inferno Beast":
                    projectiles.Add(
                        CreateProjectile(
                            position,
                            direction,
                            Damage,
                            18f,
                            new Color(255, 0, 102, 255),
                            0.3f,
                            false,
                            false,
                            true
                        )
                    );
                    break;

                default: // Flesh Pistol
                    projectiles.Add(
                        CreateProjectile(
                            position,
                            direction,
                            Damage,
                            15f,
                            new Color(255, 0, 255, 255),
                            0.3f,
                            false,
                            false,
                            false
                        )
                    );
                    break;
            }

            return projectiles;
        }

        private GameObject CreateProjectile(
            Vector3 position,
            Vector3 direction,
            float damage,
            float speed,
            Color color,
            float size,
            bool piercing,
            bool lifesteal,
            bool homing
        )
        {
            var go = new GameObject(position);

            var projComp = new ProjectileComponent();
            projComp.Velocity = direction * speed;
            projComp.Damage = damage;
            projComp.Piercing = piercing;
            projComp.Lifesteal = lifesteal;
            projComp.Homing = homing;
            go.AddComponent(projComp);

            var meshComp = new MeshRendererComponent();
            meshComp.MeshType = MeshType.Sphere;
            meshComp.Color = color;
            meshComp.Scale = new Vector3(size);
            go.AddComponent(meshComp);

            var colComp = new ColliderComponent();
            colComp.Size = new Vector3(size, size, size);
            colComp.IsTrigger = true;
            go.AddComponent(colComp);

            return go;
        }

        private SoulType GetDominantSoulType()
        {
            SoulType dominant = SoulType.Beast;
            int maxCount = AbsorbedSouls[SoulType.Beast];

            if (AbsorbedSouls[SoulType.Undead] > maxCount)
            {
                dominant = SoulType.Undead;
                maxCount = AbsorbedSouls[SoulType.Undead];
            }
            if (AbsorbedSouls[SoulType.Demon] > maxCount)
            {
                dominant = SoulType.Demon;
            }

            return dominant;
        }

        public bool CanEvolve => canEvolve;
    }
}
