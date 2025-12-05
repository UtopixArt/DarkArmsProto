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
        public const float PlayerBoundary = 9.5f;

        // === SCREEN EFFECTS ===
        public const float ScreenShakeIntensity = 0.05f;
        public const float ScreenShakeDecay = 0.9f;

        // === WEAPON SETTINGS ===
        public const float BaseDamage = 20f;
        public const float BaseFireRate = 3f; // Shots per second

        // Weapon evolution thresholds
        public const int RequiredSoulsStage2 = 10;
        public const int RequiredSoulsStage3 = 25;
        public const int RequiredSoulsStage4 = 50; // For future use

        // Weapon evolution multipliers
        public const float BoneRevolverDamageMult = 2.0f;
        public const float BoneRevolverFireRateMult = 0.5f;

        public const float TendrilBurstDamageMult = 1.3f;
        public const float TendrilBurstFireRateMult = 1.5f;

        public const float ParasiteSwarmDamageMult = 0.8f;
        public const float ParasiteSwarmFireRateMult = 2.0f;

        public const float ApexPredatorDamageMult = 3.0f;
        public const float ApexPredatorFireRateMult = 0.7f;

        public const float NecroticCannonDamageMult = 2.5f;
        public const float NecroticCannonFireRateMult = 1.2f;

        public const float InfernoBeastDamageMult = 2.0f;
        public const float InfernoBeastFireRateMult = 1.8f;

        // === PROJECTILE SETTINGS ===
        public const float ProjectileMaxLifetime = 5f;
        public const float HomingStrength = 0.1f; // Lerp factor for homing
        public const float HomingRange = 15f; // Max distance to track enemies

        // Weapon-specific projectile stats
        public const float BoneRevolverSpeed = 20f;
        public const float BoneRevolverSize = 0.5f;

        public const float TendrilBurstSpeed = 15f;
        public const float TendrilBurstSize = 0.25f;
        public const float TendrilBurstSpread = 0.15f;
        public const int TendrilBurstPellets = 5;
        public const float TendrilBurstDamagePerPellet = 0.6f;
        public const float LifestealPercent = 0.3f;

        public const float ParasiteSwarmSpeed = 18f;
        public const float ParasiteSwarmSize = 0.3f;

        public const float FleshPistolSpeed = 15f;
        public const float FleshPistolSize = 0.3f;

        // === ENEMY SETTINGS ===
        public const float BeastEnemyHealth = 30f;
        public const float BeastEnemySpeed = 3f;

        public const float UndeadEnemyHealth = 50f;
        public const float UndeadEnemySpeed = 2f;

        public const float DemonEnemyHealth = 40f;
        public const float DemonEnemySpeed = 2.5f;

        public const float EnemyTouchDamagePerSecond = 10f;
        public const float EnemyCollisionRadius = 1.5f;
        public const float HitFlashDuration = 0.1f;

        // === SOUL SETTINGS ===
        public const float SoulMagnetRadius = 3f;
        public const float SoulCollectRadius = 1.5f;
        public const float SoulFloatSpeed = 2f;
        public const float SoulMoveSpeed = 10f; // When magnetized

        // === ENEMY SPAWNING ===
        public const int InitialEnemyCount = 5;
        public const float MinSpawnDistance = 8f;
        public const float MaxSpawnDistance = 13f;

        // === VISUAL SETTINGS ===
        public const float DamageNumberLifetime = 1f;
        public const float DamageNumberRiseSpeed = 2f;
        public const int DamageNumberFontSize = 60;

        // === ROOM SETTINGS ===
        public const float RoomSize = 20f;
        public const float WallHeight = 5f;

        // === CAMERA SETTINGS ===
        public const float CameraFOV = 75f;
        public const int TargetFPS = 60;
    }
}
