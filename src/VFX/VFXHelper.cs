using System.Numerics;
using DarkArmsProto.Audio;
using Raylib_cs;

namespace DarkArmsProto.VFX
{
    /// <summary>
    /// Helper class to centralize common VFX patterns and reduce code duplication.
    /// Combines particles, lights, and sounds for common effects.
    /// </summary>
    public static class VFXHelper
    {
        private static ParticleManager? particleManager;
        private static LightManager? lightManager;

        public static void Initialize(ParticleManager particles, LightManager lights)
        {
            particleManager = particles;
            lightManager = lights;
        }

        /// <summary>
        /// Spawn impact effect (particles + light + sound)
        /// </summary>
        public static void SpawnImpact(Vector3 position, Color color, int particleCount = 10)
        {
            particleManager?.SpawnImpact(position, color, particleCount);
            lightManager?.AddImpactLight(position, color);
            AudioManager.Instance.PlaySound(SoundType.Hit, 0.2f);
        }

        /// <summary>
        /// Spawn explosion effect (particles + light + sound)
        /// </summary>
        public static void SpawnExplosion(Vector3 position, Color color, int particleCount = 40)
        {
            particleManager?.SpawnExplosion(position, color, particleCount);
            lightManager?.AddExplosionLight(position, color);
            AudioManager.Instance.PlaySound(SoundType.Explosion, 0.5f);
        }

        /// <summary>
        /// Spawn muzzle flash effect (particles + light)
        /// </summary>
        public static void SpawnMuzzleFlash(Vector3 position, Color color)
        {
            particleManager?.SpawnImpact(position, color, 2);
            lightManager?.AddMuzzleFlash(position, color);
        }

        /// <summary>
        /// Spawn death effect (explosion + screen shake if player is provided)
        /// </summary>
        public static void SpawnDeathEffect(
            Vector3 position,
            Color color,
            Core.GameObject? player = null,
            float shakeAmount = 0.3f
        )
        {
            particleManager?.SpawnExplosion(position, color, 40);
            lightManager?.AddExplosionLight(position, color);
            AudioManager.Instance.PlaySound(SoundType.Kill, 0.4f);

            // Screen shake if player is provided
            if (player != null)
            {
                var screenShake = player.GetComponent<Components.ScreenShakeComponent>();
                if (screenShake != null)
                {
                    screenShake.AddTrauma(shakeAmount);
                }
            }
        }

        /// <summary>
        /// Spawn soul collect effect
        /// </summary>
        public static void SpawnSoulCollect(Vector3 position, Color color)
        {
            // Use impact particles for soul collection
            particleManager?.SpawnImpact(position, color, 12);
            lightManager?.AddImpactLight(position, color);
        }
    }
}
