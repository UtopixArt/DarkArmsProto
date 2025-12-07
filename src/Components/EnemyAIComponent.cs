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

        public List<ColliderComponent>? WallColliders { get; set; }
        public List<GameObject>? RoomEnemies { get; set; }
        public Random Random { get; private set; } = new Random();

        // Events
        public event Action<Vector3, Vector3, float, SoulType>? OnShoot; // Pos, Dir, Damage, Type

        public EnemyAIComponent(SoulType type, float speed)
        {
            this.Type = type;
            this.Speed = speed;

            CurrentState = new IdleState();
            CurrentState.Enter(this);
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

            // Apply Gravity if not flying
            if (!IsFlying)
            {
                ApplyGravity(deltaTime);
            }
        }

        private void ApplyGravity(float deltaTime)
        {
            float gravity = 30f;
            Vector3 newPos = Owner.Position;
            newPos.Y -= gravity * deltaTime;

            // Check floor collision via Raycast (like Player)
            if (WallColliders != null)
            {
                var myCollider = Owner.GetComponent<ColliderComponent>();
                if (myCollider != null)
                {
                    // Raycast setup
                    // Start ray slightly above feet to avoid starting inside floor if slightly sunk
                    // Feet position is roughly CenterY - HalfHeight
                    // But we want to cast from Center down.

                    // Let's use the same logic as Player: Raycast from body center down
                    Vector3 rayOrigin = Owner.Position;
                    // Actually, better to cast from slightly above bottom of collider
                    var (min, max) = myCollider.GetBounds();
                    rayOrigin.Y = min.Y + 0.5f; // Start 0.5f above bottom

                    Vector3 rayDir = -Vector3.UnitY;
                    float rayLength = 1.0f; // Look down 1 unit (0.5 inside + 0.5 below)

                    float bestFloorY = float.MinValue;
                    bool hitFloor = false;

                    foreach (var wall in WallColliders)
                    {
                        if (
                            wall.Raycast(
                                rayOrigin,
                                rayDir,
                                rayLength,
                                out float dist,
                                out Vector3 normal,
                                out Vector3 point
                            )
                        )
                        {
                            // Check if it's a floor (normal pointing up)
                            if (Vector3.Dot(normal, Vector3.UnitY) > 0.5f)
                            {
                                if (point.Y > bestFloorY)
                                {
                                    bestFloorY = point.Y;
                                    hitFloor = true;
                                }
                            }
                        }
                    }

                    if (hitFloor)
                    {
                        // Snap to floor
                        // We want the bottom of our collider to be at bestFloorY
                        // Owner.Position is the center.
                        // So new Center Y = bestFloorY + HalfHeight
                        float halfHeight = (max.Y - min.Y) / 2f;

                        // However, Owner.Position might not be exactly center depending on offset.
                        // Let's assume Owner.Position is the pivot.
                        // If pivot is at feet, we set Y = bestFloorY.
                        // If pivot is center, we set Y = bestFloorY + halfHeight.

                        // Our ColliderComponent uses Size as HalfExtents.
                        // And GetBounds uses Owner.Position + Offset.
                        // So Bottom Y = (Owner.Position.Y + Offset.Y) - Size.Y
                        // We want Bottom Y = bestFloorY
                        // Owner.Position.Y = bestFloorY + Size.Y - Offset.Y

                        float targetY = bestFloorY + myCollider.Size.Y - myCollider.Offset.Y;

                        // Only snap if we are close enough (falling or walking on it)
                        if (newPos.Y <= targetY + 0.1f)
                        {
                            Owner.Position = new Vector3(newPos.X, targetY, newPos.Z);
                        }
                        else
                        {
                            Owner.Position = newPos; // Still falling
                        }
                    }
                    else
                    {
                        // Fall
                        Owner.Position = newPos;
                    }
                }
                else
                {
                    Owner.Position = newPos;
                }
            }
            else
            {
                Owner.Position = newPos;
            }
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

            var (myMin, myMax) = myCollider.GetBounds();

            foreach (var wall in WallColliders)
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
