using System;
using System.Numerics;
using DarkArmsProto.Core;

namespace DarkArmsProto.Components
{
    public class HealthComponent : Component
    {
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; set; }
        public float HitFlashTime { get; set; }

        public bool IsDead => CurrentHealth <= 0;

        // Events for damage and death
        public event Action<float>? OnDamageTaken; // (damage amount)
        public event Action<float, Vector3>? OnDamageTakenWithPosition; // (damage, position) - for damage numbers
        public event Action? OnDeath; // Called when health reaches 0

        public HealthComponent() { }

        public HealthComponent(float max)
        {
            MaxHealth = max;
            CurrentHealth = max;
        }

        public void TakeDamage(float amount)
        {
            bool wasAlive = CurrentHealth > 0;
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
            HitFlashTime = GameConfig.HitFlashDuration;

            OnDamageTaken?.Invoke(amount);
            OnDamageTakenWithPosition?.Invoke(amount, Owner.Position);

            // Trigger death event if just died
            if (wasAlive && IsDead)
            {
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        }

        public override void Update(float deltaTime)
        {
            if (HitFlashTime > 0)
                HitFlashTime -= deltaTime;
        }
    }
}
