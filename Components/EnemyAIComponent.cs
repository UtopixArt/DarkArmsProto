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

        // References
        public GameObject? TargetObject { get; private set; }
        public List<ColliderComponent>? WallColliders { get; set; }
        public Random Random { get; private set; } = new Random();

        // Events
        public event Action<Vector3, Vector3, float>? OnShoot; // Pos, Dir, Damage

        public EnemyAIComponent(SoulType type, float speed)
        {
            this.Type = type;
            this.Speed = speed;

            // Customize behavior based on type
            switch (type)
            {
                case SoulType.Beast: // Fast, melee rusher
                    AttackRange = 1.5f;
                    DetectionRange = 20.0f;
                    AttackCooldown = 0.8f;
                    Damage = 15f;
                    IsFlying = false;
                    IsRanged = false;
                    break;
                case SoulType.Undead: // Slow, standard
                    AttackRange = 1.8f;
                    DetectionRange = 12.0f;
                    AttackCooldown = 1.5f;
                    Damage = 10f;
                    IsFlying = false;
                    IsRanged = false;
                    break;
                case SoulType.Demon: // Flying, Ranged, Strong
                    AttackRange = 15.0f; // Increased range
                    DetectionRange = 35.0f; // Increased detection
                    AttackCooldown = 1.0f; // Much faster shooting (was 2.5f)
                    Damage = 25f; // Increased damage
                    IsFlying = true;
                    IsRanged = true;
                    break;
            }

            CurrentState = new IdleState();
            CurrentState.Enter(this);
        }

        public void SetTarget(GameObject target)
        {
            TargetObject = target;
        }

        public void ChangeState(IEnemyState newState)
        {
            CurrentState.Exit(this);
            CurrentState = newState;
            CurrentState.Enter(this);
        }

        public void FireProjectile(Vector3 pos, Vector3 dir)
        {
            OnShoot?.Invoke(pos, dir, Damage);
        }

        public override void Update(float deltaTime)
        {
            CurrentState.Update(this, deltaTime);
        }

        public void MoveTowards(Vector3 target, float speed, float deltaTime)
        {
            Vector3 direction = target - Owner.Position;

            if (!IsFlying)
            {
                direction.Y = 0; // Keep on ground
            }

            if (direction.LengthSquared() > 0.01f)
            {
                Vector3 moveDir = Vector3.Normalize(direction);
                Vector3 desiredMove = moveDir * speed * deltaTime;
                Vector3 originalPos = Owner.Position;

                // Try moving on X axis
                Owner.Position = new Vector3(
                    originalPos.X + desiredMove.X,
                    originalPos.Y,
                    originalPos.Z
                );
                if (CheckCollision())
                {
                    // Blocked on X, revert X
                    Owner.Position = new Vector3(originalPos.X, Owner.Position.Y, Owner.Position.Z);
                }

                // Try moving on Z axis
                Vector3 currentPos = Owner.Position;
                Owner.Position = new Vector3(
                    currentPos.X,
                    currentPos.Y,
                    currentPos.Z + desiredMove.Z
                );
                if (CheckCollision())
                {
                    // Blocked on Z, revert Z
                    Owner.Position = new Vector3(Owner.Position.X, Owner.Position.Y, currentPos.Z);
                }

                // Try moving on Y axis (only if flying)
                if (IsFlying)
                {
                    currentPos = Owner.Position;
                    Owner.Position = new Vector3(
                        currentPos.X,
                        currentPos.Y + desiredMove.Y,
                        currentPos.Z
                    );
                    if (CheckCollision())
                    {
                        // Blocked on Y, revert Y
                        Owner.Position = new Vector3(
                            Owner.Position.X,
                            currentPos.Y,
                            Owner.Position.Z
                        );
                    }
                }
            }
        }

        private bool CheckCollision()
        {
            if (WallColliders == null)
                return false;

            var myCollider = Owner.GetComponent<ColliderComponent>();
            if (myCollider == null)
                return false;

            foreach (var wall in WallColliders)
            {
                if (myCollider.CheckCollision(wall))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
