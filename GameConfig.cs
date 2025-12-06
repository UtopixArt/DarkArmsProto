namespace DarkArmsProto
{
    /// <summary>
    /// Centralized configuration for all game parameters.
    /// Modify these values to tweak gameplay without diving into code.
    /// </summary>
    public static class GameConfig
    {
        // === PLAYER SETTINGS ===
        public const float PlayerMoveSpeed = 10f;
        public const float PlayerMouseSensitivity = 0.003f;
        public const float PlayerMaxHealth = 100f;
        public const float PlayerBoundary = 13.5f; // Updated for larger rooms (RoomSize 30)
        public const float Gravity = -30f; // Global gravity

        // Player collider settings
        public const float PlayerColliderWidth = 0.4f;
        public const float PlayerColliderHeight = 0.8f;
        public const float PlayerColliderDepth = 0.4f;

        // === SCREEN EFFECTS ===
        public const float ScreenShakeOnShoot = 0.025f;
        public const float ScreenShakeOnKill = 0.3f;

        // === WEAPON SETTINGS ===
        public const float BaseDamage = 20f;
        public const float BaseFireRate = 3f; // Shots per second

        // Weapon evolution thresholds
        public const int RequiredSoulsStage2 = 10;
        public const int RequiredSoulsStage3 = 25;
        public const int RequiredSoulsStage4 = 50;
        public const int RequiredSoulsStage5 = 100;

        // Weapon evolution multipliers
        // Stage 2
        public const float BoneRevolverDamageMult = 0.5f; // SMG
        public const float BoneRevolverFireRateMult = 3.0f;

        public const float TendrilBurstDamageMult = 1.0f; // Shotgun
        public const float TendrilBurstFireRateMult = 0.8f;

        public const float ParasiteSwarmDamageMult = 0.6f; // Homing
        public const float ParasiteSwarmFireRateMult = 1.5f;

        // Stage 3
        public const float ApexPredatorDamageMult = 0.4f; // Minigun
        public const float ApexPredatorFireRateMult = 6.0f;

        public const float NecroticCannonDamageMult = 3.0f; // Grenade
        public const float NecroticCannonFireRateMult = 0.5f;

        public const float InfernoBeastDamageMult = 5.0f; // Railgun
        public const float InfernoBeastFireRateMult = 0.4f;

        // Stage 4
        public const float FeralShredderDamageMult = 0.5f; // Ricochet/Chain
        public const float FeralShredderFireRateMult = 8.0f;

        public const float PlagueSpreaderDamageMult = 1.5f; // Explosive Shotgun
        public const float PlagueSpreaderFireRateMult = 0.7f;

        public const float HellfireMissilesDamageMult = 2.0f; // Explosive Homing
        public const float HellfireMissilesFireRateMult = 1.0f;

        // Stage 5
        public const float OmegaFangDamageMult = 0.6f; // Triple Minigun
        public const float OmegaFangFireRateMult = 10.0f;

        public const float DeathsHandDamageMult = 4.0f; // Wall of Death
        public const float DeathsHandFireRateMult = 0.3f;

        public const float ArmageddonDamageMult = 20.0f; // Nuke
        public const float ArmageddonFireRateMult = 0.1f;

        // === PROJECTILE SETTINGS ===
        public const float ProjectileMaxLifetime = 5f;
        public const float HomingStrength = 0.1f; // Lerp factor for homing
        public const float HomingRange = 15f; // Max distance to track enemies

        // Weapon-specific projectile stats
        public const float BoneRevolverSpeed = 40f; // Was 20f
        public const float BoneRevolverSize = 0.5f;

        public const float TendrilBurstSpeed = 30f; // Was 15f
        public const float TendrilBurstSize = 0.25f;
        public const float TendrilBurstSpread = 0.15f;
        public const int TendrilBurstPellets = 5;
        public const float TendrilBurstDamagePerPellet = 0.6f;
        public const float LifestealPercent = 0.3f;

        public const float ParasiteSwarmSpeed = 35f; // Was 18f
        public const float ParasiteSwarmSize = 0.3f;

        public const float FleshPistolSpeed = 30f; // Was 15f
        public const float FleshPistolSize = 0.3f;

        // === ENEMY SETTINGS ===
        public const float BeastEnemyHealth = 50f; // Augmenté de 30 à 50
        public const float BeastEnemySpeed = 8.0f; // Charge speed (was 4.5)

        public const float UndeadEnemyHealth = 80f; // Augmenté de 50 à 80
        public const float UndeadEnemySpeed = 3f; // Augmenté de 2 à 3

        public const float DemonEnemyHealth = 65f; // Augmenté de 40 à 65
        public const float DemonEnemySpeed = 6f; // Faster flying (was 4)

        public const float EnemyTouchDamagePerSecond = 15f; // Augmenté de 10 à 15
        public const float EnemyCollisionRadius = 1.5f;
        public const float HitFlashDuration = 0.1f;

        // Enemy visual settings
        public const float EnemyMeshWidth = 1.5f;
        public const float EnemyMeshHeight = 4.5f;
        public const float EnemyMeshDepth = 1.5f;

        // Demon specific visual settings (Small Cube)
        public const float DemonMeshSize = 1.2f;

        // Enemy collider (half-extents of mesh)
        public const float EnemyColliderWidth = 0.75f; // MeshWidth / 2
        public const float EnemyColliderHeight = 2.25f; // MeshHeight / 2
        public const float EnemyColliderDepth = 0.75f; // MeshDepth / 2

        public const float DemonColliderSize = 0.6f; // DemonMeshSize / 2

        // Enemy health bar settings
        public const float EnemyHealthBarOffsetY = 2.5f;
        public const float EnemyHealthBarWidth = 2f;
        public const float EnemyHealthBarHeight = 0.3f;

        // === SOUL SETTINGS ===
        public const float SoulMagnetRadius = 3f;
        public const float SoulCollectRadius = 1.5f;
        public const float SoulFloatSpeed = 2f;
        public const float SoulMoveSpeed = 10f; // When magnetized

        // === ENEMY SPAWNING ===
        public const int MinEnemiesPerRoom = 5; // Minimum enemies in a normal room
        public const int MaxEnemiesPerRoom = 12; // Maximum enemies in a normal room
        public const int BossRoomEnemyCount = 1; // Boss room enemy count

        // === ROOM SETTINGS ===
        public const float RoomSize = 30f;
        public const float WallHeight = 15f; // Increased from 5f to 15f for taller rooms
    }
}
