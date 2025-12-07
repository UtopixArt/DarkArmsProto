using System;
using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.Data;
using Raylib_cs;

namespace DarkArmsProto.VFX
{
    public class ParticleManager
    {
        private List<GameObject> particles = new List<GameObject>();
        private Random rng = new Random();

        public ParticleManager()
        {
            ParticleDatabase.Load();
        }

        public void SpawnEffect(string name, Vector3 position, Color? overrideColor = null)
        {
            var effect = ParticleDatabase.GetEffect(name);
            if (effect == null)
            {
                // Console.WriteLine($"[ParticleManager] Effect '{name}' not found in database!");
                return;
            }

            // Console.WriteLine($"[ParticleManager] Spawning {name} at {position}");

            foreach (var emitter in effect.Emitters)
            {
                SpawnEmitter(emitter, position, overrideColor);
            }
        }

        private void SpawnEmitter(
            ParticleEmitterData emitter,
            Vector3 position,
            Color? overrideColor
        )
        {
            for (int i = 0; i < emitter.Count; i++)
            {
                var particle = CreateParticleObject(emitter, position, overrideColor);
                particles.Add(particle);
            }
        }

        public GameObject CreateParticleObject(
            ParticleEmitterData emitter,
            Vector3 position,
            Color? overrideColor = null
        )
        {
            // Calculate random properties
            float speed = Lerp(emitter.MinSpeed, emitter.MaxSpeed, (float)rng.NextDouble());
            float lifetime = Lerp(
                emitter.MinLifetime,
                emitter.MaxLifetime,
                (float)rng.NextDouble()
            );
            float size = Lerp(emitter.MinSize, emitter.MaxSize, (float)rng.NextDouble());

            // Generate random direction (Uniform sphere distribution)
            Vector3 dir;
            do
            {
                dir = new Vector3(
                    (float)rng.NextDouble() * 2.0f - 1.0f,
                    (float)rng.NextDouble() * 2.0f - 1.0f,
                    (float)rng.NextDouble() * 2.0f - 1.0f
                );
            } while (dir.LengthSquared() > 1.0f || dir.LengthSquared() < 0.001f);

            dir = Vector3.Normalize(dir);

            // Apply spread (scaling axes)
            dir *= emitter.Spread;

            // Re-normalize if spread changed the length significantly,
            // but if spread is (1,0,1) we want it flat.
            // If we normalize (1,0,1) * (0,1,0) -> (0,0,0) -> problem.
            // But random dir won't be exactly vertical often.
            if (dir.LengthSquared() > 0.001f)
                dir = Vector3.Normalize(dir);

            Vector3 velocity = dir * speed;

            Color color = overrideColor ?? emitter.Color.ToRaylib();
            if (emitter.UseRandomColors)
            {
                color = new Color(rng.Next(256), rng.Next(256), rng.Next(256), 255);
            }

            // Calculate spawn position offset based on InitialRadius
            Vector3 offset = Vector3.Zero;
            if (emitter.InitialRadius > 0)
            {
                // Random point inside sphere
                Vector3 randomPoint;
                do
                {
                    randomPoint = new Vector3(
                        (float)rng.NextDouble() * 2.0f - 1.0f,
                        (float)rng.NextDouble() * 2.0f - 1.0f,
                        (float)rng.NextDouble() * 2.0f - 1.0f
                    );
                } while (randomPoint.LengthSquared() > 1.0f);

                offset = randomPoint * emitter.InitialRadius;
            }

            var particle = new GameObject(position + offset);
            var particleComp = new ParticleComponent(velocity, color, lifetime);
            particleComp.Size = size;
            particleComp.Gravity = emitter.Gravity;
            particleComp.Drag = emitter.Drag;

            particle.AddComponent(particleComp);
            return particle;
        }

        private float Lerp(float min, float max, float t)
        {
            return min + (max - min) * t;
        }

        // Legacy methods kept for compatibility, but redirected to new system where possible
        public void SpawnExplosion(Vector3 position, Color color, int count = 60)
        {
            // Try to use data-driven if available, else fallback
            if (ParticleDatabase.GetEffect("Explosion") != null)
            {
                SpawnEffect("Explosion", position, color);
            }
            else
            {
                // Fallback legacy code
                for (int i = 0; i < count; i++)
                {
                    // Random direction
                    float angle = (float)(rng.NextDouble() * Math.PI * 2);
                    float elevation = (float)(rng.NextDouble() * Math.PI - Math.PI / 2);
                    float speed = (float)(rng.NextDouble() * 4 + 2);

                    Vector3 velocity = new Vector3(
                        MathF.Cos(elevation) * MathF.Cos(angle) * speed,
                        MathF.Sin(elevation) * speed,
                        MathF.Cos(elevation) * MathF.Sin(angle) * speed
                    );

                    var particle = new GameObject(position);
                    var particleComp = new ParticleComponent(
                        velocity,
                        color,
                        (float)(rng.NextDouble() * 0.5 + 0.5)
                    );
                    particleComp.Size = (float)(rng.NextDouble() * 0.15 + 0.05);
                    particle.AddComponent(particleComp);

                    particles.Add(particle);
                }
            }
        }

        public void SpawnImpact(Vector3 position, Color color, int count = 15)
        {
            if (ParticleDatabase.GetEffect("Impact") != null)
            {
                SpawnEffect("Impact", position, color);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    float angle = (float)(rng.NextDouble() * Math.PI * 2);
                    float speed = (float)(rng.NextDouble() * 2 + 1);

                    Vector3 velocity = new Vector3(
                        MathF.Cos(angle) * speed,
                        (float)(rng.NextDouble() * 2 + 1),
                        MathF.Sin(angle) * speed
                    );

                    var particle = new GameObject(position);
                    var particleComp = new ParticleComponent(
                        velocity,
                        color,
                        (float)(rng.NextDouble() * 0.2 + 0.1)
                    );
                    particleComp.Size = (float)(rng.NextDouble() * 0.08 + 0.02);
                    particle.AddComponent(particleComp);

                    particles.Add(particle);
                }
            }
        }

        public void SpawnSoulCollectEffect(Vector3 position, Color color)
        {
            // Keep legacy for specific pattern logic (spiral/circle) unless we add pattern support to data
            for (int i = 0; i < 12; i++)
            {
                float angle = (float)(i * Math.PI * 2 / 12);
                float speed = 3f;

                Vector3 velocity = new Vector3(
                    MathF.Cos(angle) * speed,
                    2f,
                    MathF.Sin(angle) * speed
                );

                var particle = new GameObject(position);
                var particleComp = new ParticleComponent(velocity, color, 0.6f);
                particleComp.Size = 0.1f;
                particleComp.Gravity = -3f;
                particle.AddComponent(particleComp);

                particles.Add(particle);
            }
        }

        public void Update(float deltaTime)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                particles[i].Update(deltaTime);
                if (!particles[i].IsActive)
                {
                    particles.RemoveAt(i);
                }
            }
        }

        public void Render()
        {
            foreach (var particle in particles)
            {
                particle.Render();
            }
        }

        public int GetParticleCount() => particles.Count;
    }
}
