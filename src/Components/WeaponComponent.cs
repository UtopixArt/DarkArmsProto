using System;
using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Core;
using DarkArmsProto.Data;
using DarkArmsProto.Factories;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class WeaponComponent : Component
    {
        // Current weapon
        private WeaponData? currentWeapon;
        public string WeaponName => currentWeapon?.Name ?? "Flesh Pistol";
        public int EvolutionStage { get; private set; }

        // Stats (calculated from weapon data + base)
        public float Damage => GameConfig.BaseDamage * (currentWeapon?.DamageMultiplier ?? 1.0f);
        public float FireRate =>
            GameConfig.BaseFireRate * (currentWeapon?.FireRateMultiplier ?? 1.0f);

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
            // Load weapon database
            WeaponDatabase.Load();

            // Initialize with starting weapon
            currentWeapon = WeaponDatabase.GetStartingWeapon();
            EvolutionStage = 1;
            lastShotTime = 0f;
            canEvolve = false;

            AbsorbedSouls = new Dictionary<SoulType, int>
            {
                { SoulType.Beast, 0 },
                { SoulType.Undead, 0 },
                { SoulType.Demon, 0 },
            };

            Console.WriteLine(
                $"[WeaponComponent] Initialized with {currentWeapon?.Name ?? "Unknown"}"
            );
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
            int nextStage = EvolutionStage + 1;

            // Get weapon from database
            var newWeapon = WeaponDatabase.GetForEvolution(nextStage, dominant);

            if (newWeapon == null)
            {
                Console.WriteLine(
                    $"[EVOLUTION] No weapon found for stage {nextStage} + {dominant}"
                );
                return;
            }

            currentWeapon = newWeapon;
            EvolutionStage = nextStage;
            canEvolve = false;

            Console.WriteLine($"[EVOLVED] {WeaponName}!");
        }

        public List<GameObject> TryShoot(
            Camera3D camera,
            Action<Vector3, float, float>? explosionCallback = null
        )
        {
            float timeSinceLastShot = lastShotTime;
            if (timeSinceLastShot < 1f / FireRate)
                return new List<GameObject>();

            lastShotTime = 0f;

            // Get spawn position
            Vector3 spawnPos = camera.Position;
            var renderComp = Owner.GetComponent<WeaponRenderComponent>();
            if (renderComp != null)
            {
                spawnPos = renderComp.GetMuzzlePosition();
                renderComp.TriggerRecoil();
            }

            // Calculate direction
            Vector3 cameraForward = Vector3.Normalize(camera.Target - camera.Position);
            Vector3 targetPoint = camera.Position + cameraForward * 50f;
            Vector3 direction = Vector3.Normalize(targetPoint - spawnPos);

            // Heal callback
            Action<float>? healCallback = (amount) =>
            {
                var health = Owner.GetComponent<HealthComponent>();
                health?.Heal(amount);
            };

            // Use factory to create projectiles
            var projectiles =
                currentWeapon != null
                    ? ProjectileFactory.CreateProjectiles(
                        spawnPos,
                        direction,
                        Damage,
                        currentWeapon,
                        explosionCallback,
                        healCallback
                    )
                    : new List<GameObject>();

            OnShoot?.Invoke(projectiles);
            return projectiles;
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
