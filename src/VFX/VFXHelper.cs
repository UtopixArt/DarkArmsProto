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
        public static void SpawnExplosion(
            Vector3 position,
            Color? color = null,
            int particleCount = 40
        )
        {
            Color finalColor = color ?? Color.Orange;

            if (Data.ParticleDatabase.GetEffect("Explosion") != null)
            {
                // Pass color only if it was explicitly provided (not null)
                // If color is null, SpawnEffect will use the JSON color
                particleManager?.SpawnEffect("Explosion", position, color);
            }
            else
            {
                particleManager?.SpawnExplosion(position, finalColor, particleCount);
            }

            lightManager?.AddExplosionLight(position, finalColor);
            AudioManager.Instance.PlaySound(SoundType.Explosion, 0.5f);
        }

        /// <summary>
        /// Spawn trail effect
        /// </summary>
        public static void SpawnTrail(Vector3 position, Color color)
        {
            particleManager?.SpawnEffect("Trail", position, color);
        }

        /// <summary>
        /// Spawn muzzle flash effect (particles + light)
        /// </summary>
        public static void SpawnMuzzleFlash(Vector3 position, Color color)
        {
            // Try to use data-driven MuzzleFlash, fallback to Impact
            if (Data.ParticleDatabase.GetEffect("MuzzleFlash") != null)
            {
                particleManager?.SpawnEffect("MuzzleFlash", position, color);
            }
            else
            {
                particleManager?.SpawnImpact(position, color, 8);
            }

            lightManager?.AddMuzzleFlash(position, color);
        }

        /// <summary>
        /// Spawn blood effect
        /// </summary>
        public static void SpawnBlood(Vector3 position)
        {
            if (Data.ParticleDatabase.GetEffect("Blood") != null)
            {
                // Pass null so JSON color is used
                particleManager?.SpawnEffect("Blood", position, null);
            }
            else
            {
                particleManager?.SpawnImpact(position, Color.Red, 15);
            }
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
            // Use SpawnExplosion helper which handles data-driven logic
            SpawnExplosion(position, color, 80);

            // lightManager?.AddExplosionLight(position, color); // Already called by SpawnExplosion
            // AudioManager.Instance.PlaySound(SoundType.Kill, 0.4f); // Keep this if different from Explosion sound

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
