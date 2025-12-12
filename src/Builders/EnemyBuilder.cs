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
        private Vector3 spriteOffset = Vector3.Zero;
        private Color color = Color.White;
        private Vector3 meshSize = new Vector3(1.5f, 4.5f, 1.5f);
        private Vector3 colliderSize = new Vector3(0.75f, 2.25f, 0.75f);
        private Vector3 colliderOffset = Vector3.Zero;
        private Action<Vector3, Vector3, float, SoulType>? onShootCallback = null;
        private List<ColliderComponent>? wallColliders = null;

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

        public EnemyBuilder WithSprite(string path, float size, Vector3 offset)
        {
            this.spritePath = path;
            this.spriteSize = size;
            this.spriteOffset = offset;
            return this;
        }

        public EnemyBuilder WithSprite(string path, float size)
        {
            return WithSprite(path, size, Vector3.Zero);
        }

        public EnemyBuilder WithColor(Color col)
        {
            this.color = col;
            return this;
        }

        public EnemyBuilder WithColliderSize(Vector3 size)
        {
            this.colliderSize = size;
            return this;
        }

        public EnemyBuilder WithColliderOffset(Vector3 offset)
        {
            this.colliderOffset = offset;
            return this;
        }

        public EnemyBuilder WithWallColliders(List<ColliderComponent> colliders)
        {
            this.wallColliders = colliders;
            return this;
        }

        // === BUILD ===

        public GameObject Build()
        {
            // Use spawn position as-is for ground enemies (rigidbody will handle ground snapping)
            // Only adjust Y for flying enemies to make them hover slightly above ground
            Vector3 spawnPos = position;
            if (isFlying)
            {
                spawnPos.Y += 0.5f; // Flying enemies hover just above ground level
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
                var spriteComp = new SpriteRendererComponent(spritePath, spriteSize, Color.White);
                spriteComp.Offset = spriteOffset;
                enemy.AddComponent(spriteComp);
            }
            else
            {
                // Fallback to mesh
                enemy.AddComponent(new MeshRendererComponent(color, meshSize));
            }

            // Health bar
            float healthBarOffset = isFlying ? 1.5f : GameConfig.EnemyHealthBarOffsetY;
            enemy.AddComponent(
                new HealthBarComponent(
                    new Vector3(0, healthBarOffset, 0),
                    new Vector2(GameConfig.EnemyHealthBarWidth, GameConfig.EnemyHealthBarHeight)
                )
            );

            // Collider
            enemy.AddComponent(
                new ColliderComponent { Size = colliderSize, Offset = colliderOffset }
            );

            // Rigidbody (physics) - must be added before AI component
            var rigidbody = new RigidbodyComponent();
            rigidbody.UseGravity = !isFlying; // Flying enemies don't use gravity
            rigidbody.WallColliders = wallColliders; // Assign wall colliders before adding component
            rigidbody.GroundRayLength = 10.0f; // Long enough to detect ground below
            rigidbody.UseColliderBottomForRaycast = true; // Start ray from feet
            rigidbody.ShowDebugRaycast = false; // Disable debug visualization
            rigidbody.UseKillZone = true; // Auto-kill enemies that fall too far
            rigidbody.KillZoneY = -50f; // Kill enemies below Y = -50
            enemy.AddComponent(rigidbody);

            // Death handler (manages soul spawning and VFX on death)
            enemy.AddComponent(new EnemyDeathComponent(soulType));

            return enemy;
        }
    }
}
