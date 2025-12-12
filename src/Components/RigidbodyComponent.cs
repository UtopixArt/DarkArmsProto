using System;
using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    /// <summary>
    /// Component that handles physics simulation: gravity, velocity, ground detection.
    /// Centralizes physics logic previously duplicated in PlayerInputComponent and EnemyAIComponent.
    /// </summary>
    public class RigidbodyComponent : Component
    {
        // Physics properties
        public float Gravity { get; set; } = 30f;
        public Vector3 Velocity { get; set; } = Vector3.Zero;
        public bool IsGrounded { get; private set; } = false;
        public bool UseGravity { get; set; } = true;

        // Ground detection settings
        public float GroundRayLength { get; set; } = 5.0f;
        public float GroundRayOffset { get; set; } = 0.5f; // Offset from bottom of collider
        public float SlopeMaxCos { get; set; } = 0.5f; // cos(60°) - max walkable slope
        public bool UseColliderBottomForRaycast { get; set; } = true; // Auto-calculate ray origin from collider bottom

        // Kill zone (auto-kill entities that fall too far)
        public float KillZoneY { get; set; } = -10f; // Y position below which entities are killed (reduced from -50)
        public bool UseKillZone { get; set; } = true; // Enable kill zone

        // Collision references
        public List<ColliderComponent>? WallColliders { get; set; }

        // Debug visualization
        public bool ShowDebugRaycast { get; set; } = true;
        private Vector3 lastRayOrigin;
        private Vector3 lastRayEnd;
        private bool lastRayHit;

        public override void Update(float deltaTime)
        {
            // Check kill zone - auto-kill entities that fall too far
            if (UseKillZone && Owner.Position.Y < KillZoneY)
            {
                var health = Owner.GetComponent<HealthComponent>();
                if (health != null && !health.IsDead)
                {
                    health.TakeDamage(health.CurrentHealth); // Kill instantly
                }
                return; // Don't process physics for dead entities
            }

            // Always perform ground detection to update debug visuals, even if flying
            float groundY = DetectGround();

            if (UseGravity)
            {
                ApplyGravity(deltaTime, groundY);
            }
        }

        /// <summary>
        /// Performs the raycast to detect ground and updates debug visualization data.
        /// Uses 4 raycasts at the corners of the collider for better ground detection.
        /// Returns the Y position of the ground, or float.MinValue if no ground found.
        /// </summary>
        private float DetectGround()
        {
            var myCollider = Owner.GetComponent<ColliderComponent>();
            if (myCollider == null)
                return float.MinValue;

            if (WallColliders == null || WallColliders.Count == 0)
                return float.MinValue;

            // Calculate feet Y position
            float feetY = UseColliderBottomForRaycast
                ? Owner.Position.Y - myCollider.Size.Y + myCollider.Offset.Y
                : Owner.Position.Y + GroundRayOffset;

            // Add small epsilon to avoid starting exactly on the surface
            feetY += 0.01f;

            // Calculate the 4 corner positions at the bottom of the collider
            // Use slightly smaller radius to avoid edge cases
            float rayRadius = MathF.Min(myCollider.Size.X, myCollider.Size.Z) * 0.8f;
            Vector3[] rayOrigins = new Vector3[]
            {
                new Vector3(Owner.Position.X + rayRadius, feetY, Owner.Position.Z + rayRadius), // Front-Right
                new Vector3(Owner.Position.X - rayRadius, feetY, Owner.Position.Z + rayRadius), // Front-Left
                new Vector3(Owner.Position.X + rayRadius, feetY, Owner.Position.Z - rayRadius), // Back-Right
                new Vector3(Owner.Position.X - rayRadius, feetY, Owner.Position.Z - rayRadius)  // Back-Left
            };

            Vector3 rayDir = -Vector3.UnitY;
            float bestY = float.MinValue;
            bool anyHit = false;
            Vector3 hitPoint = Vector3.Zero;

            // Test all 4 corners
            foreach (var rayOrigin in rayOrigins)
            {
                foreach (var wall in WallColliders)
                {
                    if (wall == null)
                        continue;

                    var (minWall, maxWall) = wall.GetBounds();

                    if (
                        Helpers.CollisionHelper.RaycastAABB(
                            rayOrigin,
                            rayDir,
                            GroundRayLength,
                            minWall,
                            maxWall,
                            out float hitDist,
                            out Vector3 hitNormal,
                            out Vector3 currentHitPoint
                        )
                    )
                    {
                        float normalDot = Vector3.Dot(hitNormal, Vector3.UnitY);

                        // Check if it's a walkable surface (normal pointing up enough)
                        if (normalDot >= SlopeMaxCos)
                        {
                            if (currentHitPoint.Y > bestY)
                            {
                                bestY = currentHitPoint.Y;
                                hitPoint = currentHitPoint;
                                anyHit = true;
                            }
                        }
                    }
                }
            }

            // Store ray info for debug visualization (center ray)
            lastRayOrigin = new Vector3(Owner.Position.X, feetY, Owner.Position.Z);
            lastRayHit = anyHit;
            lastRayEnd = anyHit ? hitPoint : lastRayOrigin + rayDir * GroundRayLength;

            return bestY;
        }

        /// <summary>
        /// Apply gravity using the pre-calculated ground height
        /// </summary>
        private void ApplyGravity(float deltaTime, float bestY)
        {
            // Apply gravity to vertical velocity
            Velocity = new Vector3(Velocity.X, Velocity.Y - Gravity * deltaTime, Velocity.Z);
            IsGrounded = false;

            // Snap to ground if found
            if (bestY > float.MinValue)
            {
                var myCollider = Owner.GetComponent<ColliderComponent>();
                if (myCollider == null) return;

                // bestY is the floor position. We need to position the entity center above it
                // Center Y = floor Y + collider half-height - collider offset
                float targetCenterY = bestY + myCollider.Size.Y - myCollider.Offset.Y;
                float predictedY = Owner.Position.Y + Velocity.Y * deltaTime;

                // Snap if on/above ground and descending
                if (predictedY <= targetCenterY + 0.05f)
                {
                    Owner.Position = new Vector3(Owner.Position.X, targetCenterY, Owner.Position.Z);
                    Velocity = new Vector3(Velocity.X, Math.Max(0, Velocity.Y), Velocity.Z);
                    IsGrounded = true;
                }
            }

            // Apply vertical motion (remaining fall)
            if (!IsGrounded)
            {
                Owner.Position += new Vector3(0, Velocity.Y * deltaTime, 0);
            }
        }

        /// <summary>
        /// Apply an impulse force (e.g., for jumping)
        /// </summary>
        public void AddForce(Vector3 force)
        {
            Velocity += force;
        }

        /// <summary>
        /// Set vertical velocity directly (e.g., for jumping)
        /// </summary>
        public void SetVerticalVelocity(float verticalVelocity)
        {
            Velocity = new Vector3(Velocity.X, verticalVelocity, Velocity.Z);
        }

        /// <summary>
        /// Move the rigidbody with collision detection and sliding
        /// Supports both horizontal (ground) and 3D (flying) movement
        /// </summary>
        public void Move(Vector3 moveDirection, float speed, float deltaTime)
        {
            var myCollider = Owner.GetComponent<ColliderComponent>();
            if (myCollider == null || moveDirection == Vector3.Zero)
                return;

            Vector3 original = Owner.Position;
            Vector3 target = original + moveDirection * speed * deltaTime;

            if (WallColliders != null && WallColliders.Count > 0)
            {
                // Try full move
                Owner.Position = target;
                bool fullHit = CollidesWithWalls(myCollider);

                if (fullHit)
                {
                    // Try sliding along X, Y, and Z axes separately
                    Owner.Position = original;

                    // Try X axis
                    Vector3 xPos = new Vector3(target.X, original.Y, original.Z);
                    Owner.Position = xPos;
                    bool xHit = CollidesWithWalls(myCollider);

                    // Try Y axis (for flying enemies)
                    Vector3 yPos = new Vector3(original.X, target.Y, original.Z);
                    Owner.Position = yPos;
                    bool yHit = CollidesWithWalls(myCollider);

                    // Try Z axis
                    Vector3 zPos = new Vector3(original.X, original.Y, target.Z);
                    Owner.Position = zPos;
                    bool zHit = CollidesWithWalls(myCollider);

                    // Try combinations for best sliding
                    if (!xHit && !zHit)
                    {
                        // Can slide on XZ plane
                        Owner.Position = new Vector3(target.X, original.Y, target.Z);
                    }
                    else if (!xHit && !yHit)
                    {
                        // Can slide on XY plane (flying up/down and sideways)
                        Owner.Position = new Vector3(target.X, target.Y, original.Z);
                    }
                    else if (!zHit && !yHit)
                    {
                        // Can slide on YZ plane
                        Owner.Position = new Vector3(original.X, target.Y, target.Z);
                    }
                    else if (!xHit)
                    {
                        Owner.Position = xPos; // Slide along X only
                    }
                    else if (!yHit)
                    {
                        Owner.Position = yPos; // Slide along Y only
                    }
                    else if (!zHit)
                    {
                        Owner.Position = zPos; // Slide along Z only
                    }
                    else
                    {
                        Owner.Position = original; // Completely stuck
                    }
                }
            }
            else
            {
                // No collision detection, just move
                Owner.Position = target;
            }
        }

        /// <summary>
        /// Check if the collider is colliding with any walls (excluding floors)
        /// </summary>
        private bool CollidesWithWalls(ColliderComponent self)
        {
            if (WallColliders == null)
                return false;

            // Calculate actual feet position (bottom of collider)
            // Position is center, so subtract collider half-height and add offset
            float feetY = self.Owner.Position.Y - self.Size.Y + self.Offset.Y;
            float stepHeight = 0.2f; // Tolerance for floor/steps

            foreach (var wall in WallColliders)
            {
                if (wall != null && self.CheckCollision(wall))
                {
                    var (minW, maxW) = wall.GetBounds();

                    // Ignore if it's a floor (top is at or below our feet + step tolerance)
                    if (maxW.Y <= feetY + stepHeight)
                        continue;

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Render debug visualization of the ground detection raycast
        /// </summary>
        public override void Render()
        {
            if (!ShowDebugRaycast) return;

            // Draw raycast line
            Color rayColor = lastRayHit ? Color.Green : Color.Red;
            Raylib.DrawLine3D(lastRayOrigin, lastRayEnd, rayColor);

            // Draw sphere at ray origin
            Raylib.DrawSphere(lastRayOrigin, 0.2f, Color.Yellow);

            // Draw sphere at ray end (hit point or max length)
            Raylib.DrawSphere(lastRayEnd, 0.3f, rayColor);
        }

    }
}
