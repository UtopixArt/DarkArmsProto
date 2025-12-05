using DarkArmsProto.Core;

namespace DarkArmsProto.Components
{
    public class HealthComponent : Component
    {
        public float Current { get; private set; }
        public float Max { get; private set; }
        public float HitFlashTime { get; set; }

        public bool IsDead => Current <= 0;

        public HealthComponent(float max)
        {
            Max = max;
            Current = max;
        }

        public void TakeDamage(float amount)
        {
            Current = Math.Max(0, Current - amount);
            HitFlashTime = GameConfig.HitFlashDuration;
        }

        public override void Update(float deltaTime)
        {
            if (HitFlashTime > 0)
                HitFlashTime -= deltaTime;
        }
    }
}
