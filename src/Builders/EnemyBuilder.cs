using System;
using System.Numerics;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Builders
{
    /// <summary>
    /// Builder Pattern: Fluent API for creating enemies.
    /// Simplifies enemy creation with sensible defaults.
    /// </summary>
    public class EnemyBuilder
    {
        private Vector3 position;
        private SoulType soulType = SoulType.Undead;
        private float health = 100f;
        private float speed = 4f;
        private float damage = 10f;
        private float attackRange = 1.5f;
        private float detectionRange = 15f;
        private float attackCooldown = 1.0f;
        private bool isFlying = false;
        private bool isRanged = false;
        private string spritePath = "";
        private float spriteSize = 3.5f;
        private Color color = Color.White;
        private Vector3 meshSize = new Vector3(1.5f, 4.5f, 1.5f);
        private Vector3 colliderSize = new Vector3(0.75f, 2.25f, 0.75f);
        private Action<Vector3, Vector3, float, SoulType>? onShootCallback = null;

        public EnemyBuilder() { }

        // === POSITION ===

        public EnemyBuilder AtPosition(Vector3 pos)
        {
            this.position = pos;
            return this;
        }

        // === SOUL TYPE ===

        public EnemyBuilder OfType(SoulType type)
        {
            this.soulType = type;
            return this;
        }

        // === STATS ===

        public EnemyBuilder WithHealth(float hp)
        {
            this.health = hp;
            return this;
        }

        public EnemyBuilder WithSpeed(float spd)
        {
            this.speed = spd;
            return this;
        }

        public EnemyBuilder WithDamage(float dmg)
        {
            this.damage = dmg;
            return this;
        }

        public EnemyBuilder WithAttackRange(float range)
        {
            this.attackRange = range;
            return this;
        }

        public EnemyBuilder WithDetectionRange(float range)
        {
            this.detectionRange = range;
            return this;
        }

        public EnemyBuilder WithAttackCooldown(float cooldown)
        {
            this.attackCooldown = cooldown;
            return this;
        }

        // === ABILITIES ===

        public EnemyBuilder AsFlying()
        {
            this.isFlying = true;
            return this;
        }

        public EnemyBuilder AsRanged()
        {
            this.isRanged = true;
            return this;
        }

        public EnemyBuilder WithShootCallback(Action<Vector3, Vector3, float, SoulType> callback)
        {
            this.onShootCallback = callback;
            return this;
        }

        // === VISUALS ===

        public EnemyBuilder WithSprite(string path, float size)
        {
            this.spritePath = path;
            this.spriteSize = size;
            return this;
        }

        public EnemyBuilder WithColor(Color col)
        {
            this.color = col;
            return this;
        }

        public EnemyBuilder WithMeshSize(Vector3 size)
        {
            this.meshSize = size;
            return this;
        }

        public EnemyBuilder WithColliderSize(Vector3 size)
        {
            this.colliderSize = size;
            return this;
        }

        // === BUILD ===

        public GameObject Build()
        {
            // Adjust spawn position for sprite
            Vector3 spawnPos = position;
            if (!string.IsNullOrEmpty(spritePath))
            {
                spawnPos.Y += spriteSize / 2.0f;
            }
            if (isFlying)
            {
                spawnPos.Y += 1.5f;
            }

            var enemy = new GameObject(spawnPos);

            // Health
            enemy.AddComponent(new HealthComponent(health));

            // AI
            var ai = new EnemyAIComponent(soulType, speed)
            {
                AttackRange = attackRange,
                DetectionRange = detectionRange,
                AttackCooldown = attackCooldown,
                Damage = damage,
                IsFlying = isFlying,
                IsRanged = isRanged,
            };

            if (onShootCallback != null)
            {
                ai.OnShoot += onShootCallback;
            }

            enemy.AddComponent(ai);

            // Visuals
            if (!string.IsNullOrEmpty(spritePath))
            {
                enemy.AddComponent(
                    new SpriteRendererComponent(spritePath, spriteSize, Color.White)
                );
            }
            else
            {
                // Fallback to mesh
                enemy.AddComponent(new MeshRendererComponent(color, meshSize));
            }

            // Enemy tag
            enemy.AddComponent(new EnemyComponent(soulType));

            // Health bar
            float healthBarOffset = isFlying ? 1.5f : GameConfig.EnemyHealthBarOffsetY;
            enemy.AddComponent(
                new HealthBarComponent(
                    new Vector3(0, healthBarOffset, 0),
                    new Vector2(GameConfig.EnemyHealthBarWidth, GameConfig.EnemyHealthBarHeight)
                )
            );

            // Collider
            enemy.AddComponent(new ColliderComponent { Size = colliderSize });

            return enemy;
        }
    }
}
