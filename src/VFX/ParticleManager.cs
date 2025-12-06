using System;
using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.VFX
{
    public class ParticleManager
    {
        private List<GameObject> particles = new List<GameObject>();

        public void SpawnExplosion(Vector3 position, Color color, int count = 60)
        {
            Random rng = new Random();
            for (int i = 0; i < count; i++)
            {
                // Random direction
                float angle = (float)(rng.NextDouble() * Math.PI * 2);
                float elevation = (float)(rng.NextDouble() * Math.PI - Math.PI / 2);
                float speed = (float)(rng.NextDouble() * 4 + 2); // Reduced speed from 5-13 to 2-6

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
                particleComp.Size = (float)(rng.NextDouble() * 0.15 + 0.05); // Slightly smaller
                particle.AddComponent(particleComp);

                particles.Add(particle);
            }
        }

        public void SpawnImpact(Vector3 position, Color color, int count = 15)
        {
            Random rng = new Random();
            for (int i = 0; i < count; i++)
            {
                float angle = (float)(rng.NextDouble() * Math.PI * 2);
                float speed = (float)(rng.NextDouble() * 2 + 1); // Reduced speed from 3-8 to 1-3

                Vector3 velocity = new Vector3(
                    MathF.Cos(angle) * speed,
                    (float)(rng.NextDouble() * 2 + 1), // Reduced vertical speed
                    MathF.Sin(angle) * speed
                );

                var particle = new GameObject(position);
                var particleComp = new ParticleComponent(
                    velocity,
                    color,
                    (float)(rng.NextDouble() * 0.2 + 0.1)
                ); // Reduced lifetime
                particleComp.Size = (float)(rng.NextDouble() * 0.08 + 0.02); // Smaller particles
                particle.AddComponent(particleComp);

                particles.Add(particle);
            }
        }

        public void SpawnSoulCollectEffect(Vector3 position, Color color)
        {
            Random rng = new Random();
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
                particleComp.Size = 0.1f; // Smaller
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
