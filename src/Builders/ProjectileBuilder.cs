using System;
using System.Numerics;
using DarkArmsProto.Components;
using DarkArmsProto.Components.Behaviors;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Builders
{
    /// <summary>
    /// Builder Pattern: Fluent API for creating projectiles.
    /// Simplifies projectile creation and makes it more readable.
    /// </summary>
    public class ProjectileBuilder
    {
        private Vector3 position;
        private Vector3 velocity;
        private float damage = 10f;
        private float size = 0.3f;
        private Color color = Color.White;
        private bool isEnemyProjectile = false;

        // Behaviors
        private bool addHoming = false;
        private float homingStrength = 0.1f;
        private float homingRange = 20f;

        private bool addPiercing = false;
        private int maxPierces = -1;

        private bool addExplosive = false;
        private float explosionRadius = 3.0f;
        private float explosionDamage = 1.0f;
        private Action<Vector3, float, float>? explosionCallback = null;

        private bool addLifesteal = false;
        private float lifestealPercent = 0.3f;
        private Action<float>? healCallback = null;

        private bool addTrail = false;
        private Color trailColor = Color.White;

        public ProjectileBuilder() { }

        // === POSITION & VELOCITY ===

        public ProjectileBuilder AtPosition(Vector3 pos)
        {
            this.position = pos;
            return this;
        }

        public ProjectileBuilder WithVelocity(Vector3 vel)
        {
            this.velocity = vel;
            return this;
        }

        public ProjectileBuilder WithDirection(Vector3 direction, float speed)
        {
            this.velocity = Vector3.Normalize(direction) * speed;
            return this;
        }

        // === CORE PROPERTIES ===

        public ProjectileBuilder WithDamage(float dmg)
        {
            this.damage = dmg;
            return this;
        }

        public ProjectileBuilder WithSize(float sz)
        {
            this.size = sz;
            return this;
        }

        public ProjectileBuilder WithColor(Color col)
        {
            this.color = col;
            return this;
        }

        public ProjectileBuilder AsEnemyProjectile()
        {
            this.isEnemyProjectile = true;
            return this;
        }

        // === BEHAVIORS ===

        public ProjectileBuilder WithHoming(float strength = 0.1f, float range = 20f)
        {
            this.addHoming = true;
            this.homingStrength = strength;
            this.homingRange = range;
            return this;
        }

        public ProjectileBuilder WithPiercing(int maxPierces = -1)
        {
            this.addPiercing = true;
            this.maxPierces = maxPierces;
            return this;
        }

        public ProjectileBuilder WithExplosion(
            float radius = 3.0f,
            float damageMultiplier = 1.0f,
            Action<Vector3, float, float>? callback = null
        )
        {
            this.addExplosive = true;
            this.explosionRadius = radius;
            this.explosionDamage = damageMultiplier;
            this.explosionCallback = callback;
            return this;
        }

        public ProjectileBuilder WithLifesteal(float percent = 0.3f, Action<float>? callback = null)
        {
            this.addLifesteal = true;
            this.lifestealPercent = percent;
            this.healCallback = callback;
            return this;
        }

        public ProjectileBuilder WithTrail(Color color)
        {
            this.addTrail = true;
            this.trailColor = color;
            return this;
        }

        // === BUILD ===

        public GameObject Build()
        {
            var projectile = new GameObject(position);

            // Add ProjectileComponent
            var projComp = new ProjectileComponent
            {
                Velocity = velocity,
                Damage = damage,
                IsEnemyProjectile = isEnemyProjectile,
            };

            // Add behaviors
            if (addHoming)
            {
                projComp.AddBehavior(new HomingBehavior(homingStrength, homingRange));
            }

            if (addPiercing)
            {
                projComp.AddBehavior(new PiercingBehavior(maxPierces));
            }

            if (addExplosive)
            {
                projComp.AddBehavior(
                    new ExplosiveBehavior(explosionRadius, explosionDamage, explosionCallback)
                );
            }

            if (addLifesteal)
            {
                projComp.AddBehavior(new LifestealBehavior(lifestealPercent, healCallback));
            }

            if (addTrail)
            {
                projComp.AddBehavior(new TrailBehavior(trailColor));
            }

            projectile.AddComponent(projComp);

            // Add Mesh Renderer
            var meshComp = new MeshRendererComponent
            {
                MeshType = MeshType.Sphere,
                Color = color,
                Scale = new Vector3(size),
            };
            projectile.AddComponent(meshComp);

            // Add Collider
            var colComp = new ColliderComponent
            {
                Size = new Vector3(size, size, size),
                IsTrigger = true,
                ShowDebug = false,
            };
            projectile.AddComponent(colComp);

            return projectile;
        }
    }
}
