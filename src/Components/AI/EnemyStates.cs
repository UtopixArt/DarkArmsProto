using System;
using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components.AI
{
    // ------------------------------------------------------------------------
    // IDLE STATE
    // ------------------------------------------------------------------------
    public class IdleState : IEnemyState
    {
        public void Enter(EnemyAIComponent enemy) { }

        public void Update(EnemyAIComponent enemy, float deltaTime)
        {
            if (enemy.TargetObject == null)
                return;

            float dist = Vector3.Distance(enemy.Owner.Position, enemy.TargetObject.Position);
            if (dist < enemy.DetectionRange)
            {
                enemy.ChangeState(new ChaseState());
                return;
            }

            // Randomly switch to Wander
            if (enemy.Random.NextDouble() < 0.01f)
            {
                enemy.ChangeState(new WanderState());
            }
        }

        public void Exit(EnemyAIComponent enemy) { }
    }

    // ------------------------------------------------------------------------
    // WANDER STATE
    // ------------------------------------------------------------------------
    public class WanderState : IEnemyState
    {
        private float timer;
        private Vector3 targetPos;

        public void Enter(EnemyAIComponent enemy)
        {
            timer = (float)enemy.Random.NextDouble() * 2.0f + 1.0f;

            // Pick a random point around current pos
            float angle = (float)enemy.Random.NextDouble() * MathF.PI * 2;
            float dist = (float)enemy.Random.NextDouble() * 3.0f + 1.0f;
            targetPos =
                enemy.Owner.Position + new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * dist;
        }

        public void Update(EnemyAIComponent enemy, float deltaTime)
        {
            if (enemy.TargetObject != null)
            {
                float dist = Vector3.Distance(enemy.Owner.Position, enemy.TargetObject.Position);
                if (dist < enemy.DetectionRange)
                {
                    enemy.ChangeState(new ChaseState());
                    return;
                }
            }

            timer -= deltaTime;
            if (timer <= 0)
            {
                enemy.ChangeState(new IdleState());
                return;
            }

            enemy.MoveTowards(targetPos, enemy.Speed * 0.5f, deltaTime);
        }

        public void Exit(EnemyAIComponent enemy) { }
    }

    // ------------------------------------------------------------------------
    // CHASE STATE
    // ------------------------------------------------------------------------
    public class ChaseState : IEnemyState
    {
        public void Enter(EnemyAIComponent enemy) { }

        public void Update(EnemyAIComponent enemy, float deltaTime)
        {
            if (enemy.TargetObject == null)
            {
                enemy.ChangeState(new IdleState());
                return;
            }

            float dist = Vector3.Distance(enemy.Owner.Position, enemy.TargetObject.Position);

            // If lost target
            if (dist > enemy.DetectionRange * 1.5f)
            {
                enemy.ChangeState(new IdleState());
                return;
            }

            // If in attack range
            if (dist <= enemy.AttackRange)
            {
                enemy.ChangeState(new AttackState());
                return;
            }

            // Move towards target
            // If flying, we might want to hover at a certain height relative to player
            Vector3 targetPos = enemy.TargetObject.Position;
            if (enemy.IsFlying)
            {
                // Fly slightly above player
                targetPos.Y += 1.0f;
            }

            enemy.MoveTowards(targetPos, enemy.Speed, deltaTime);
        }

        public void Exit(EnemyAIComponent enemy) { }
    }

    // ------------------------------------------------------------------------
    // ATTACK STATE
    // ------------------------------------------------------------------------
    public class AttackState : IEnemyState
    {
        private float delayTimer = 0.2f;
        private int shotsFired = 0;
        private int shotsToFire = 1;
        private float timeBetweenShots = 0.15f;
        private bool isFinished = false;

        public void Enter(EnemyAIComponent enemy)
        {
            delayTimer = 0.2f;
            shotsFired = 0;
            isFinished = false;

            if (enemy.IsRanged)
            {
                shotsToFire = enemy.Random.Next(2, 4); // 2 or 3 shots
            }
            else
            {
                shotsToFire = 1;
            }
        }

        public void Update(EnemyAIComponent enemy, float deltaTime)
        {
            delayTimer -= deltaTime;

            if (delayTimer <= 0 && !isFinished)
            {
                PerformAttack(enemy);
                shotsFired++;

                if (shotsFired >= shotsToFire)
                {
                    isFinished = true;
                    enemy.ChangeState(new CooldownState());
                }
                else
                {
                    delayTimer = timeBetweenShots;
                }
            }
        }

        private void PerformAttack(EnemyAIComponent enemy)
        {
            if (enemy.TargetObject == null)
                return;

            if (enemy.IsRanged)
            {
                // Shoot projectile
                Vector3 dir = Vector3.Normalize(enemy.TargetObject.Position - enemy.Owner.Position);
                // Add slight inaccuracy
                float spread = 0.1f;
                dir.X += (float)(enemy.Random.NextDouble() * 2 - 1) * spread;
                dir.Y += (float)(enemy.Random.NextDouble() * 2 - 1) * spread;
                dir.Z += (float)(enemy.Random.NextDouble() * 2 - 1) * spread;
                dir = Vector3.Normalize(dir);

                enemy.FireProjectile(enemy.Owner.Position + dir * 0.5f, dir);
            }
            else
            {
                // Melee hit
                float dist = Vector3.Distance(enemy.Owner.Position, enemy.TargetObject.Position);
                if (dist <= enemy.AttackRange * 1.2f)
                {
                    var health = enemy.TargetObject.GetComponent<HealthComponent>();
                    health?.TakeDamage(enemy.Damage);
                }
            }
        }

        public void Exit(EnemyAIComponent enemy) { }
    }

    // ------------------------------------------------------------------------
    // COOLDOWN STATE
    // ------------------------------------------------------------------------
    public class CooldownState : IEnemyState
    {
        private float timer;
        private Vector3 moveDirection;
        private bool isMoving = false;

        public void Enter(EnemyAIComponent enemy)
        {
            timer = enemy.AttackCooldown;

            if (enemy.IsRanged && enemy.TargetObject != null)
            {
                isMoving = true;
                // Pick a random direction to strafe (perpendicular to player)
                Vector3 toPlayer = Vector3.Normalize(
                    enemy.TargetObject.Position - enemy.Owner.Position
                );
                Vector3 right = Vector3.Cross(toPlayer, Vector3.UnitY);

                // Randomly go left or right
                float dir = enemy.Random.NextDouble() > 0.5 ? 1f : -1f;
                moveDirection = right * dir;

                // Add some randomness
                moveDirection += new Vector3(
                    (float)(enemy.Random.NextDouble() * 2 - 1) * 0.5f,
                    (float)(enemy.Random.NextDouble() * 2 - 1) * 0.5f,
                    (float)(enemy.Random.NextDouble() * 2 - 1) * 0.5f
                );

                moveDirection = Vector3.Normalize(moveDirection);
            }
            else if (enemy.Type == SoulType.Beast && enemy.TargetObject != null)
            {
                // Beast Retreat Logic
                isMoving = true;
                // Move AWAY from player
                Vector3 toPlayer = Vector3.Normalize(
                    enemy.TargetObject.Position - enemy.Owner.Position
                );
                moveDirection = -toPlayer; // Backwards

                // Add slight side variation so they don't just back up in a straight line
                moveDirection += new Vector3(
                    (float)(enemy.Random.NextDouble() * 2 - 1) * 0.5f,
                    0,
                    (float)(enemy.Random.NextDouble() * 2 - 1) * 0.5f
                );
                moveDirection = Vector3.Normalize(moveDirection);
            }
        }

        public void Update(EnemyAIComponent enemy, float deltaTime)
        {
            timer -= deltaTime;

            if (isMoving)
            {
                if (enemy.IsFlying)
                {
                    // Move while cooling down (Repositioning)
                    enemy.MoveTowards(
                        enemy.Owner.Position + moveDirection * 5f,
                        enemy.Speed * 0.8f,
                        deltaTime
                    );
                }
                else if (enemy.Type == SoulType.Beast)
                {
                    // Beast Retreat
                    enemy.MoveTowards(
                        enemy.Owner.Position + moveDirection * 5f,
                        enemy.Speed * 0.6f,
                        deltaTime
                    );
                }
            }

            if (timer <= 0)
            {
                enemy.ChangeState(new ChaseState());
            }
        }

        public void Exit(EnemyAIComponent enemy) { }
    }
}
