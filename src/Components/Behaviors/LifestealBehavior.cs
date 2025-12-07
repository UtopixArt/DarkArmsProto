using System;
using System.Numerics;
using DarkArmsProto.Core;

namespace DarkArmsProto.Components.Behaviors
{
    /// <summary>
    /// Lifesteal behavior: Heals player when projectile hits enemy
    /// </summary>
    public class LifestealBehavior : IProjectileBehavior
    {
        private float lifestealPercent;
        private Action<float>? onHeal; // Callback: heal amount

        public LifestealBehavior(float percent = 0.3f, Action<float>? healCallback = null)
        {
            this.lifestealPercent = percent;
            this.onHeal = healCallback;
        }

        public void Update(GameObject projectile, ProjectileComponent component, float deltaTime)
        {
            // Lifesteal doesn't need update logic
        }

        public bool OnHit(
            GameObject projectile,
            ProjectileComponent component,
            GameObject target,
            Vector3 hitPosition
        )
        {
            // Heal player
            float healAmount = component.Damage * lifestealPercent;
            onHeal?.Invoke(healAmount);

            return false; // Don't destroy (let other behaviors decide)
        }

        public bool OnWallHit(
            GameObject projectile,
            ProjectileComponent component,
            Vector3 hitPosition
        )
        {
            // Lifesteal doesn't prevent wall destruction
            return true;
        }
    }
}
