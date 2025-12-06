using System;
using DarkArmsProto.Core;

namespace DarkArmsProto.Components
{
    public class HealthComponent : Component
    {
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; set; }
        public float HitFlashTime { get; set; }

        public bool IsDead => CurrentHealth <= 0;
        public event Action<float>? OnDamageTaken;

        public HealthComponent() { }

        public HealthComponent(float max)
        {
            MaxHealth = max;
            CurrentHealth = max;
        }

        public void TakeDamage(float amount)
        {
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
            HitFlashTime = GameConfig.HitFlashDuration;
            OnDamageTaken?.Invoke(amount);
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
