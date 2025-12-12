using System;
using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components.AI;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class EnemyAIComponent : Component
    {
        public IEnemyState CurrentState { get; private set; }
        public SoulType Type { get; private set; }

        // Stats
        public float Speed { get; set; }
        public float AttackRange { get; set; } = 1.5f;
        public float DetectionRange { get; set; } = 15.0f;
        public float AttackCooldown { get; set; } = 1.0f;
        public float Damage { get; set; } = 10f;

        // Capabilities
        public bool IsFlying { get; set; } = false;
        public bool IsRanged { get; set; } = false;

        // References (automatically resolved via GameWorld)
        private GameObject? targetObject;
        public GameObject? TargetObject
        {
            get
            {
                // Auto-find player if not set (using GameWorld)
                if (targetObject == null || !targetObject.IsActive)
                {
                    targetObject = Core.GameWorld.Instance.Player;
                }
                return targetObject;
            }
        }

        public List<GameObject>? RoomEnemies { get; set; }
        public Random Random { get; private set; } = new Random();
        public Navigation.NavMesh? NavMesh { get; set; } // Set by spawn system

        private RigidbodyComponent? rigidbody;

        // Events
        public event Action<Vector3, Vector3, float, SoulType>? OnShoot; // Pos, Dir, Damage, Type

        public EnemyAIComponent(SoulType type, float speed)
        {
            this.Type = type;
            this.Speed = speed;

            CurrentState = new IdleState();
            CurrentState.Enter(this);
        }

        public override void Start()
        {
            // Get rigidbody component if it exists
            rigidbody = Owner.GetComponent<RigidbodyComponent>();

            // Configure rigidbody based on enemy type
            if (rigidbody != null)
            {
                rigidbody.UseGravity = !IsFlying;
            }
        }

        public void SetTarget(GameObject target)
        {
            targetObject = target;
        }

        public void ChangeState(IEnemyState newState)
        {
            CurrentState.Exit(this);
            CurrentState = newState;
            CurrentState.Enter(this);
        }

        public void FireProjectile(Vector3 pos, Vector3 dir)
        {
            OnShoot?.Invoke(pos, dir, Damage, Type);
        }

        public override void Update(float deltaTime)
        {
            CurrentState.Update(this, deltaTime);
            // Rigidbody handles gravity automatically based on UseGravity flag
        }

        public void MoveTowards(Vector3 target, float speed, float deltaTime)
        {
            Vector3 direction = target - Owner.Position;

            if (!IsFlying)
            {
                direction.Y = 0; // Keep on ground
            }

            // Calculate Separation (Avoidance)
            Vector3 separation = Vector3.Zero;
            if (RoomEnemies != null)
            {
                int count = 0;
                foreach (var other in RoomEnemies)
                {
                    if (other == Owner || !other.IsActive)
                        continue;

                    float dist = Vector3.Distance(Owner.Position, other.Position);
                    if (dist < 2.5f && dist > 0.01f) // Avoidance radius
                    {
                        Vector3 push = Owner.Position - other.Position;
                        push = Vector3.Normalize(push) / dist; // Stronger when closer
                        separation += push;
                        count++;
                    }
                }
                if (count > 0)
                {
                    separation /= count;
                    separation *= 3.0f; // Separation strength
                }
            }

            if (!IsFlying)
                separation.Y = 0;

            Vector3 finalDir = Vector3.Zero;
            if (direction.LengthSquared() > 0.01f)
                finalDir = Vector3.Normalize(direction);

            finalDir += separation;

            if (finalDir.LengthSquared() > 0.01f)
            {
                Vector3 moveDir = Vector3.Normalize(finalDir);

                // Use rigidbody.Move() for proper collision handling with wall sliding
                if (rigidbody != null)
                {
                    rigidbody.Move(moveDir, speed, deltaTime);
                }
                else
                {
                    // Fallback if no rigidbody (shouldn't happen)
                    Owner.Position += moveDir * speed * deltaTime;
                }
            }
        }

        private bool CheckCollision()
        {
            if (rigidbody == null || rigidbody.WallColliders == null)
                return false;

            var myCollider = Owner.GetComponent<ColliderComponent>();
            if (myCollider == null)
                return false;

            var (myMin, myMax) = myCollider.GetBounds();

            foreach (var wall in rigidbody.WallColliders)
            {
                if (myCollider.CheckCollision(wall))
                {
                    // Ignore floors (objects below feet)
                    var (wallMin, wallMax) = wall.GetBounds();
                    if (wallMax.Y <= myMin.Y + 0.2f)
                        continue;

                    return true;
                }
            }
            return false;
        }
    }
}
