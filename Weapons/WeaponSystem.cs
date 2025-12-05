using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace DarkArmsProto
{
    public enum SoulType
    {
        Beast,
        Undead,
        Demon,
    }

    public class WeaponSystem
    {
        public string WeaponName { get; private set; }
        public int EvolutionStage { get; private set; }
        public Dictionary<SoulType, int> AbsorbedSouls { get; private set; }

        private PlayerController player;
        private float damage;
        private float fireRate;
        private float lastShotTime;
        private int totalSouls;
        private int[] requiredSouls = { 10, 25, 50 };
        private bool canEvolve;

        public WeaponSystem(PlayerController player)
        {
            this.player = player;
            WeaponName = "Flesh Pistol";
            EvolutionStage = 1;
            damage = 20f;
            fireRate = 3f; // Shots per second
            lastShotTime = 0f;
            totalSouls = 0;
            canEvolve = false;

            AbsorbedSouls = new Dictionary<SoulType, int>
            {
                { SoulType.Beast, 0 },
                { SoulType.Undead, 0 },
                { SoulType.Demon, 0 },
            };
        }

        public void FeedSoul(SoulType soulType)
        {
            AbsorbedSouls[soulType]++;
            totalSouls++;

            CheckEvolution();
        }

        private void CheckEvolution()
        {
            if (EvolutionStage > 3)
                return;

            int required = requiredSouls[EvolutionStage - 1];
            if (totalSouls >= required && !canEvolve)
            {
                canEvolve = true;
                Console.WriteLine($"[EVOLUTION AVAILABLE] Press E to evolve!");
            }
        }

        public void Update(float deltaTime)
        {
            lastShotTime += deltaTime;

            // Check for evolution input
            if (canEvolve && Raylib.IsKeyPressed(KeyboardKey.E))
            {
                ShowEvolutionChoice();
            }
        }

        private void ShowEvolutionChoice()
        {
            // Get dominant soul type
            SoulType dominant = GetDominantSoulType();

            // Auto evolve based on dominant type (in full version, show UI choice)
            string newName = "";
            float damageMult = 1f;
            float fireRateMult = 1f;

            if (EvolutionStage == 1)
            {
                switch (dominant)
                {
                    case SoulType.Beast:
                        newName = "Bone Revolver";
                        damageMult = 2.0f;
                        fireRateMult = 0.5f;
                        break;
                    case SoulType.Undead:
                        newName = "Tendril Burst";
                        damageMult = 1.3f;
                        fireRateMult = 1.5f;
                        break;
                    case SoulType.Demon:
                        newName = "Parasite Swarm";
                        damageMult = 0.8f;
                        fireRateMult = 2.0f;
                        break;
                }
            }
            else if (EvolutionStage == 2)
            {
                switch (dominant)
                {
                    case SoulType.Beast:
                        newName = "Apex Predator";
                        damageMult = 3.0f;
                        fireRateMult = 0.7f;
                        break;
                    case SoulType.Undead:
                        newName = "Necrotic Cannon";
                        damageMult = 2.5f;
                        fireRateMult = 1.2f;
                        break;
                    case SoulType.Demon:
                        newName = "Inferno Beast";
                        damageMult = 2.0f;
                        fireRateMult = 1.8f;
                        break;
                }
            }

            WeaponName = newName;
            damage *= damageMult;
            fireRate *= fireRateMult;
            EvolutionStage++;
            canEvolve = false;

            Console.WriteLine($"[EVOLVED] {WeaponName}!");
        }

        public List<Projectile> Shoot(Camera3D camera)
        {
            float timeSinceLastShot = lastShotTime;
            if (timeSinceLastShot < 1f / fireRate)
                return new List<Projectile>();

            lastShotTime = 0f;

            Vector3 direction = Vector3.Normalize(camera.Target - camera.Position);

            // Create projectiles based on weapon type
            return CreateProjectilesForWeapon(camera.Position, direction);
        }

        private List<Projectile> CreateProjectilesForWeapon(Vector3 position, Vector3 direction)
        {
            var projectiles = new List<Projectile>();

            switch (WeaponName)
            {
                case "Bone Revolver":
                case "Apex Predator":
                    // Single big piercing shot
                    projectiles.Add(
                        new Projectile(
                            position,
                            direction,
                            damage,
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
                    // Shotgun spread
                    for (int i = 0; i < 5; i++)
                    {
                        Vector3 spreadDir = direction;
                        float spread = (i - 2) * 0.15f;
                        spreadDir.X += spread;
                        spreadDir = Vector3.Normalize(spreadDir);

                        projectiles.Add(
                            new Projectile(
                                position,
                                spreadDir,
                                damage * 0.6f,
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
                    // Fast homing shots
                    projectiles.Add(
                        new Projectile(
                            position,
                            direction,
                            damage,
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
                        new Projectile(
                            position,
                            direction,
                            damage,
                            15f,
                            new Color(255, 0, 255, 255),
                            0.3f,
                            false,
                            false,
                            true
                        )
                    );
                    break;
            }

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

        public void RenderUI()
        {
            int x = 10;
            int y = Raylib.GetScreenHeight() - 180;

            // Weapon info panel
            Raylib.DrawRectangle(x, y, 400, 170, new Color(0, 0, 0, 200));
            Raylib.DrawText(WeaponName, x + 10, y + 10, 20, new Color(255, 0, 255, 255));
            Raylib.DrawText(
                $"Stage: {EvolutionStage}/3",
                x + 10,
                y + 35,
                16,
                new Color(0, 255, 255, 255)
            );

            // Soul bars
            int barY = y + 60;
            DrawSoulBar("Beast", SoulType.Beast, x + 10, barY, new Color(255, 136, 0, 255));
            DrawSoulBar("Undead", SoulType.Undead, x + 10, barY + 25, new Color(0, 255, 0, 255));
            DrawSoulBar("Demon", SoulType.Demon, x + 10, barY + 50, new Color(255, 0, 0, 255));

            // Total progress
            int required = EvolutionStage > 3 ? 999 : requiredSouls[EvolutionStage - 1];
            Raylib.DrawText(
                $"Total Souls: {totalSouls} / {required}",
                x + 10,
                barY + 80,
                14,
                new Color(0, 255, 255, 255)
            );

            if (canEvolve)
            {
                Raylib.DrawText("[Press E to Evolve]", x + 10, barY + 100, 16, Color.Green);
            }
        }

        private void DrawSoulBar(string name, SoulType type, int x, int y, Color color)
        {
            int count = AbsorbedSouls[type];
            int maxSouls = Math.Max(10, totalSouls);
            float percentage = (float)count / maxSouls;

            Raylib.DrawText(name, x, y, 12, Color.White);
            Raylib.DrawRectangle(x + 70, y, 200, 15, new Color(50, 50, 50, 255));
            Raylib.DrawRectangle(x + 70, y, (int)(200 * percentage), 15, color);
            Raylib.DrawText(count.ToString(), x + 275, y, 12, Color.White);
        }
    }
}
