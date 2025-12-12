using System;
using System.Numerics;
using DarkArmsProto.Core;
using DarkArmsProto.Helpers;
using Raylib_cs;

namespace DarkArmsProto.Components.Behaviors
{
    /// <summary>
    /// Laser raycast behavior - instant hit instead of projectile movement.
    /// Uses CollisionHelper for raycast physics.
    /// </summary>
    public class LaserBehavior : IProjectileBehavior
    {
        private Vector3 startPosition;
        private Vector3 direction;
        private float maxRange;
        private bool hasPerformedRaycast = false;
        private Vector3 endPosition;
        private float damage;
        private bool isExplosive;
        private float explosionRadius;
        private System.Action<Vector3, float, float>? explosionCallback;
        private int maxBounces;
        private List<Vector3> bouncePoints = new List<Vector3>(); // For multi-segment rendering

        public LaserBehavior(
            Vector3 start,
            Vector3 dir,
            float range,
            float dmg,
            bool explosive = false,
            float explosionRad = 0f,
            System.Action<Vector3, float, float>? explCallback = null,
            int bounces = 0
        )
        {
            startPosition = start;
            direction = Vector3.Normalize(dir);
            maxRange = range;
            damage = dmg;
            endPosition = start + direction * range;
            isExplosive = explosive;
            explosionRadius = explosionRad;
            explosionCallback = explCallback;
            maxBounces = bounces;
            bouncePoints.Add(start); // First point is always the start
        }

        public void Update(GameObject projectile, ProjectileComponent component, float deltaTime)
        {
            // Perform raycast only once on first update
            if (!hasPerformedRaycast)
            {
                PerformRaycast(projectile, component);
                hasPerformedRaycast = true;
            }

            // Laser disappears after a short duration (handled by ProjectileComponent.Lifetime)
        }

        private void PerformRaycast(GameObject laserObject, ProjectileComponent component)
        {
            Vector3 currentStart = startPosition;
            Vector3 currentDirection = direction;
            float remainingRange = maxRange;
            int bouncesLeft = maxBounces;

            Console.WriteLine($"[LASER] Starting raycast - maxBounces: {maxBounces}, range: {maxRange}");
            Console.WriteLine($"[LASER] WallColliders: {(component.WallColliders != null ? component.WallColliders.Count : 0)}");

            // Perform bouncing raycast loop
            while (true)
            {
                GameObject? closestHit = null;
                float closestDistance = remainingRange;
                Vector3 hitPoint = currentStart + currentDirection * remainingRange;
                Vector3 hitNormal = Vector3.Zero;
                bool hitWall = false;

                // Check enemies
                var targets = GameWorld.Instance.FindAllWithTag(
                    component.IsEnemyProjectile ? "Player" : "Enemy"
                );

                foreach (var target in targets)
                {
                    var collider = target.GetComponent<ColliderComponent>();
                    if (collider == null) continue;

                    // Get AABB bounds
                    var (min, max) = CollisionHelper.GetBounds(
                        target.Position,
                        collider.Size,
                        collider.Offset
                    );

                    // Raycast against enemy AABB
                    if (CollisionHelper.RaycastAABB(
                        currentStart,
                        currentDirection,
                        remainingRange,
                        min,
                        max,
                        out float distance,
                        out Vector3 normal,
                        out Vector3 hit
                    ))
                    {
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestHit = target;
                            hitPoint = hit;
                            hitNormal = normal;
                            hitWall = false;
                        }
                    }
                }

                // Check walls
                if (component.WallColliders != null)
                {
                    foreach (var wall in component.WallColliders)
                    {
                        if (wall.Owner == null || !wall.Owner.IsActive) continue;

                        var (min, max) = CollisionHelper.GetBounds(
                            wall.Owner.Position,
                            wall.Size,
                            wall.Offset
                        );

                        if (CollisionHelper.RaycastAABB(
                            currentStart,
                            currentDirection,
                            remainingRange,
                            min,
                            max,
                            out float distance,
                            out Vector3 normal,
                            out Vector3 hit
                        ))
                        {
                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                closestHit = null; // Wall hit
                                hitPoint = hit;
                                hitNormal = normal;
                                hitWall = true;
                            }
                        }
                    }
                }

                // Add bounce point
                bouncePoints.Add(hitPoint);

                // Apply damage immediately if hit enemy
                if (closestHit != null)
                {
                    var health = closestHit.GetComponent<HealthComponent>();
                    if (health != null)
                    {
                        health.TakeDamage(damage);

                        // Damage number
                        Systems.DamageNumberManager.AddDamageNumber(closestHit.Position, damage);

                        // VFX
                        VFX.VFXHelper.SpawnBlood(hitPoint);

                        var mesh = laserObject.GetComponent<MeshRendererComponent>();
                        Color color = mesh != null ? mesh.Color : new Color(0, 255, 255, 255);
                        VFX.VFXHelper.SpawnImpact(hitPoint, color, 15);

                        // Explosion if explosive
                        if (isExplosive && explosionRadius > 0)
                        {
                            explosionCallback?.Invoke(hitPoint, explosionRadius, damage * 0.5f);
                            VFX.VFXHelper.SpawnExplosion(hitPoint, null, (int)explosionRadius);
                        }
                    }

                    // Enemy hit - stop laser (no bounce through enemies)
                    break;
                }
                else if (hitWall && bouncesLeft > 0)
                {
                    Console.WriteLine($"[LASER] BOUNCE! Point: {hitPoint}, Normal: {hitNormal}, BouncesLeft: {bouncesLeft}");

                    // Wall hit with bounces remaining - calculate reflection
                    // Impact VFX
                    var mesh = laserObject.GetComponent<MeshRendererComponent>();
                    Color color = mesh != null ? mesh.Color : new Color(0, 255, 255, 255);
                    VFX.VFXHelper.SpawnImpact(hitPoint, color, 3);

                    // Reflect direction: R = D - 2(D·N)N
                    Vector3 reflect = currentDirection - 2 * Vector3.Dot(currentDirection, hitNormal) * hitNormal;
                    currentDirection = Vector3.Normalize(reflect);

                    Console.WriteLine($"[LASER] Reflected direction: {currentDirection}, Remaining range: {remainingRange - closestDistance}");

                    // Update start position for next segment
                    currentStart = hitPoint + currentDirection * 0.01f; // Offset slightly to avoid self-hit

                    // Reduce remaining range
                    remainingRange -= closestDistance;
                    bouncesLeft--;

                    // Safety check: if remaining range is too low, stop
                    if (remainingRange <= 0.1f)
                    {
                        Console.WriteLine($"[LASER] Stopping - insufficient range remaining: {remainingRange}");
                        break;
                    }

                    // Continue loop for next bounce
                    continue;
                }
                else
                {
                    Console.WriteLine($"[LASER] End - hitWall: {hitWall}, bouncesLeft: {bouncesLeft}, hitPoint: {hitPoint}");
                    Console.WriteLine($"[LASER] Total bounce points: {bouncePoints.Count}");

                    // Wall hit without bounces OR max range - stop laser
                    var mesh = laserObject.GetComponent<MeshRendererComponent>();
                    Color color = mesh != null ? mesh.Color : new Color(0, 255, 255, 255);
                    VFX.VFXHelper.SpawnImpact(hitPoint, color, 5);

                    // Explosion on wall if explosive
                    if (isExplosive && explosionRadius > 0)
                    {
                        explosionCallback?.Invoke(hitPoint, explosionRadius, damage * 0.5f);
                        VFX.VFXHelper.SpawnExplosion(hitPoint, null, (int)explosionRadius);
                    }

                    break;
                }
            }

            // Store final endpoint
            endPosition = bouncePoints[bouncePoints.Count - 1];
            laserObject.Position = endPosition;
        }

        public bool OnHit(GameObject projectile, ProjectileComponent component, GameObject target, Vector3 hitPosition)
        {
            // Laser already performed instant hit in Update
            // Don't destroy - let lifetime handle it
            return false;
        }

        public bool OnWallHit(GameObject projectile, ProjectileComponent component, Vector3 hitPosition)
        {
            // Laser already handled wall collision in raycast
            return false;
        }

        // Accessors for rendering
        public Vector3 GetStartPosition() => startPosition;
        public Vector3 GetEndPosition() => endPosition;
        public List<Vector3> GetBouncePoints() => bouncePoints;
    }
}
