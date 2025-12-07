namespace DarkArmsProto
{
    /// <summary>
    /// Centralized configuration for all game parameters.
    /// Modify these values to tweak gameplay without diving into code.
    /// </summary>
    public static class GameConfig
    {
        // === PLAYER SETTINGS ===
        public const float PlayerMaxHealth = 100f;

        // Player collider settings
        public const float PlayerColliderWidth = 0.4f;
        public const float PlayerColliderHeight = 1.6f;
        public const float PlayerColliderDepth = 0.4f;

        // === SCREEN EFFECTS ===
        public const float ScreenShakeOnShoot = 0.08f;
        public const float ScreenShakeOnKill = 0.2f;

        // === WEAPON SETTINGS ===
        public const float BaseDamage = 20f;
        public const float BaseFireRate = 3f; // Shots per second

        // Weapon evolution thresholds
        public const int RequiredSoulsStage2 = 5;
        public const int RequiredSoulsStage3 = 15;
        public const int RequiredSoulsStage4 = 20;
        public const int RequiredSoulsStage5 = 30;

        public const float EnemyTouchDamagePerSecond = 15f; // Augmenté de 10 à 15
        public const float EnemyCollisionRadius = 1.5f;
        public const float HitFlashDuration = 0.1f;

        // Enemy health bar settings
        public const float EnemyHealthBarOffsetY = 2.5f;
        public const float EnemyHealthBarWidth = 2f;
        public const float EnemyHealthBarHeight = 0.3f;

        // === SOUL SETTINGS ===
        public const float SoulMagnetRadius = 12f;
        public const float SoulCollectRadius = 1.5f;
        public const float SoulFloatSpeed = 2f;
        public const float SoulMoveSpeed = 10f; // When magnetized

        // === ENEMY SPAWNING ===
        public const int MinEnemiesPerRoom = 5; // Minimum enemies in a normal room
        public const int MaxEnemiesPerRoom = 12; // Maximum enemies in a normal room
        public const int BossRoomEnemyCount = 1; // Boss room enemy count

        // === ROOM SETTINGS ===
        public const float RoomSize = 60f;
        public const float WallHeight = 15f; // Increased from 5f to 15f for taller rooms
    }
}
