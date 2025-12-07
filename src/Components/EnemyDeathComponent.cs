using System.Numerics;
using DarkArmsProto.Core;
using DarkArmsProto.VFX;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    /// <summary>
    /// Component that handles enemy death automatically via HealthComponent.OnDeath event.
    /// Spawns souls, VFX, and notifies the game of kills.
    /// </summary>
    public class EnemyDeathComponent : Component
    {
        private SoulType enemyType;
        private GameObject? player;

        public static event System.Action? OnEnemyKilled; // Global event for kill counting
        public static SoulManager? GlobalSoulManager { get; set; } // Static reference for soul spawning

        public EnemyDeathComponent(SoulType type)
        {
            enemyType = type;
        }

        public override void Start()
        {
            // Subscribe to death event
            var health = Owner.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.OnDeath += HandleDeath;
            }

            // Find player via GameWorld
            player = GameWorld.Instance.Player;
        }

        private void HandleDeath()
        {
            Vector3 position = Owner.Position;

            // Death VFX
            var mesh = Owner.GetComponent<MeshRendererComponent>();
            Color color = mesh != null ? mesh.Color : Color.Red;
            VFXHelper.SpawnDeathEffect(position, color, player, GameConfig.ScreenShakeOnKill);

            // Spawn soul via global manager
            if (GlobalSoulManager != null)
            {
                GlobalSoulManager.SpawnSoul(position, enemyType);
            }

            // Notify global kill counter
            OnEnemyKilled?.Invoke();

            // Unregister from GameWorld
            GameWorld.Instance.Unregister(Owner);

            // Mark as inactive
            Owner.IsActive = false;
        }
    }
}
