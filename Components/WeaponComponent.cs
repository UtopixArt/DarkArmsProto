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
        public string WeaponName { get; private set; } = "Flesh Pistol";
        public int EvolutionStage { get; private set; }
        public float Damage { get; private set; }
        public float FireRate { get; private set; }

        // Soul tracking
        public Dictionary<SoulType, int> AbsorbedSouls { get; private set; } =
            new Dictionary<SoulType, int>();
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
            GameConfig.RequiredSoulsStage5,
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
            if (EvolutionStage > 4)
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
            else if (EvolutionStage == 3)
            {
                switch (dominant)
                {
                    case SoulType.Beast:
                        newName = "Feral Shredder";
                        damageMult = GameConfig.FeralShredderDamageMult;
                        fireRateMult = GameConfig.FeralShredderFireRateMult;
                        break;
                    case SoulType.Undead:
                        newName = "Plague Spreader";
                        damageMult = GameConfig.PlagueSpreaderDamageMult;
                        fireRateMult = GameConfig.PlagueSpreaderFireRateMult;
                        break;
                    case SoulType.Demon:
                        newName = "Hellfire Missiles";
                        damageMult = GameConfig.HellfireMissilesDamageMult;
                        fireRateMult = GameConfig.HellfireMissilesFireRateMult;
                        break;
                }
            }
            else if (EvolutionStage == 4)
            {
                switch (dominant)
                {
                    case SoulType.Beast:
                        newName = "Omega Fang";
                        damageMult = GameConfig.OmegaFangDamageMult;
                        fireRateMult = GameConfig.OmegaFangFireRateMult;
                        break;
                    case SoulType.Undead:
                        newName = "Death's Hand";
                        damageMult = GameConfig.DeathsHandDamageMult;
                        fireRateMult = GameConfig.DeathsHandFireRateMult;
                        break;
                    case SoulType.Demon:
                        newName = "Armageddon";
                        damageMult = GameConfig.ArmageddonDamageMult;
                        fireRateMult = GameConfig.ArmageddonFireRateMult;
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
                    // SMG Style: Fast, low damage, slight spread
                    Vector3 smgDir = direction;
                    float smgSpread = (float)(new Random().NextDouble() * 0.1f - 0.05f);
                    smgDir.X += smgSpread;
                    smgDir.Y += smgSpread;
                    smgDir = Vector3.Normalize(smgDir);

                    projectiles.Add(
                        CreateProjectile(
                            position,
                            smgDir,
                            Damage * 0.4f, // Lower damage per shot
                            25f,
                            new Color(255, 200, 150, 255),
                            0.2f,
                            false,
                            false,
                            false
                        )
                    );
                    break;

                case "Apex Predator":
                    // Minigun Style: Extreme fire rate handled by stats, here just spread
                    Vector3 miniDir = direction;
                    float miniSpread = (float)(new Random().NextDouble() * 0.2f - 0.1f);
                    miniDir.X += miniSpread;
                    miniDir.Y += miniSpread;
                    miniDir = Vector3.Normalize(miniDir);

                    projectiles.Add(
                        CreateProjectile(
                            position,
                            miniDir,
                            Damage * 0.3f,
                            30f,
                            new Color(255, 100, 50, 255),
                            0.2f,
                            true, // Piercing
                            false,
                            false
                        )
                    );
                    break;

                case "Tendril Burst":
                    // Shotgun
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 spreadDir = direction;
                        float spread = (i - 2.5f) * 0.1f;
                        spreadDir.X += spread;
                        spreadDir = Vector3.Normalize(spreadDir);

                        projectiles.Add(
                            CreateProjectile(
                                position,
                                spreadDir,
                                Damage * 0.5f,
                                18f,
                                new Color(100, 255, 100, 255),
                                0.25f,
                                false,
                                true, // Lifesteal
                                false
                            )
                        );
                    }
                    break;

                case "Necrotic Cannon":
                    // Grenade Launcher: Explosive
                    projectiles.Add(
                        CreateProjectile(
                            position,
                            direction,
                            Damage * 2.0f,
                            15f,
                            new Color(50, 255, 50, 255),
                            0.6f,
                            false,
                            false,
                            false,
                            true, // Explosive
                            5.0f // Radius
                        )
                    );
                    break;

                case "Parasite Swarm":
                    // Homing Swarm
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 swarmDir = direction;
                        float spread = (i - 1) * 0.3f;
                        swarmDir.X += spread;
                        swarmDir = Vector3.Normalize(swarmDir);

                        projectiles.Add(
                            CreateProjectile(
                                position,
                                swarmDir,
                                Damage * 0.6f,
                                12f,
                                new Color(200, 0, 255, 255),
                                0.3f,
                                false,
                                false,
                                true // Homing
                            )
                        );
                    }
                    break;

                case "Inferno Beast":
                    // Railgun: Fast, Piercing, High Damage
                    projectiles.Add(
                        CreateProjectile(
                            position,
                            direction,
                            Damage * 3.0f,
                            60f, // Very fast
                            new Color(255, 50, 0, 255),
                            0.4f,
                            true, // Piercing
                            false,
                            false
                        )
                    );
                    break;

                // === STAGE 4 WEAPONS ===

                case "Feral Shredder":
                    // Chain Gun: High spread, high speed, piercing
                    for (int i = 0; i < 2; i++) // Double barrel
                    {
                        Vector3 feralDir = direction;
                        float feralSpread = (float)(new Random().NextDouble() * 0.3f - 0.15f);
                        feralDir.X += feralSpread;
                        feralDir.Y += feralSpread;
                        feralDir = Vector3.Normalize(feralDir);

                        projectiles.Add(
                            CreateProjectile(
                                position,
                                feralDir,
                                Damage * 0.4f,
                                40f,
                                new Color(255, 255, 0, 255),
                                0.25f,
                                true, // Piercing
                                false,
                                false
                            )
                        );
                    }
                    break;

                case "Plague Spreader":
                    // Explosive Shotgun
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 plagueDir = direction;
                        float spread = (i - 3.5f) * 0.15f;
                        plagueDir.X += spread;
                        plagueDir = Vector3.Normalize(plagueDir);

                        projectiles.Add(
                            CreateProjectile(
                                position,
                                plagueDir,
                                Damage * 0.8f,
                                20f,
                                new Color(0, 255, 0, 255),
                                0.3f,
                                false,
                                false,
                                false,
                                true, // Explosive
                                3.0f // Smaller radius per pellet
                            )
                        );
                    }
                    break;

                case "Hellfire Missiles":
                    // Explosive Homing
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 hellDir = direction;
                        float spread = (i - 1.5f) * 0.4f;
                        hellDir.X += spread;
                        hellDir = Vector3.Normalize(hellDir);

                        projectiles.Add(
                            CreateProjectile(
                                position,
                                hellDir,
                                Damage * 1.5f,
                                15f,
                                new Color(255, 100, 0, 255),
                                0.5f,
                                false,
                                false,
                                true, // Homing
                                true, // Explosive
                                4.0f
                            )
                        );
                    }
                    break;

                // === STAGE 5 WEAPONS ===

                case "Omega Fang":
                    // Triple Minigun Stream
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 omegaDir = direction;
                        float angle = (i - 1) * 0.2f; // -0.2, 0, 0.2 radians roughly
                        // Rotate vector simply by adding to X/Z for 2D-ish spread
                        omegaDir.X += angle;
                        omegaDir = Vector3.Normalize(omegaDir);

                        projectiles.Add(
                            CreateProjectile(
                                position,
                                omegaDir,
                                Damage * 0.5f,
                                50f,
                                new Color(255, 255, 255, 255),
                                0.3f,
                                true, // Piercing
                                true, // Lifesteal
                                false
                            )
                        );
                    }
                    break;

                case "Death's Hand":
                    // Wall of Death: Many projectiles in a line
                    for (int i = 0; i < 12; i++)
                    {
                        Vector3 deathDir = direction;
                        // Create a wall perpendicular to direction?
                        // For simplicity, just a very wide shotgun
                        float spread = (i - 5.5f) * 0.05f;
                        deathDir.X += spread;
                        deathDir = Vector3.Normalize(deathDir);

                        projectiles.Add(
                            CreateProjectile(
                                position,
                                deathDir,
                                Damage * 2.0f,
                                10f, // Slow
                                new Color(50, 0, 50, 255),
                                0.8f, // Large
                                true, // Piercing
                                true, // Lifesteal
                                false
                            )
                        );
                    }
                    break;

                case "Armageddon":
                    // Nuke: Single massive slow projectile
                    projectiles.Add(
                        CreateProjectile(
                            position,
                            direction,
                            Damage * 10.0f,
                            8f, // Very slow
                            new Color(255, 0, 0, 255),
                            2.0f, // Huge
                            false,
                            false,
                            false,
                            true, // Explosive
                            15.0f // Massive radius
                        )
                    );
                    break;

                default: // Flesh Pistol
                    projectiles.Add(
                        CreateProjectile(
                            position,
                            direction,
                            Damage * 1.5f, // Increased power
                            45f, // Increased speed (was 15f)
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
            bool homing,
            bool explosive = false,
            float explosionRadius = 0f
        )
        {
            var go = new GameObject(position);

            var projComp = new ProjectileComponent();
            projComp.Velocity = direction * speed;
            projComp.Damage = damage;
            projComp.Piercing = piercing;
            projComp.Lifesteal = lifesteal;
            projComp.Homing = homing;
            projComp.Explosive = explosive;
            projComp.ExplosionRadius = explosionRadius;
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
