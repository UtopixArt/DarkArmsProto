using System;
using System.Collections.Generic;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuUtilities;
using BepuUtilities.Memory;

namespace DarkArmsProto.Systems
{
    [Flags]
    public enum CollisionGroup
    {
        Default = 1,
        Player = 2,
        Enemy = 4,
        PlayerProjectile = 8,
        EnemyProjectile = 16,
        Wall = 32,
    }

    public static class CollisionRegistry
    {
        public static Dictionary<int, CollisionGroup> BodyGroups =
            new Dictionary<int, CollisionGroup>();
        public static Dictionary<int, CollisionGroup> StaticGroups =
            new Dictionary<int, CollisionGroup>();

        public static void RegisterBody(BodyHandle handle, CollisionGroup group)
        {
            BodyGroups[handle.Value] = group;
        }

        public static void RegisterStatic(StaticHandle handle, CollisionGroup group)
        {
            StaticGroups[handle.Value] = group;
        }

        public static void UnregisterBody(BodyHandle handle)
        {
            BodyGroups.Remove(handle.Value);
        }

        public static void UnregisterStatic(StaticHandle handle)
        {
            StaticGroups.Remove(handle.Value);
        }

        public static CollisionGroup GetGroup(CollidableReference collidable)
        {
            if (collidable.Mobility == CollidableMobility.Static)
            {
                return StaticGroups.TryGetValue(collidable.StaticHandle.Value, out var g)
                    ? g
                    : CollisionGroup.Default;
            }
            else
            {
                return BodyGroups.TryGetValue(collidable.BodyHandle.Value, out var g)
                    ? g
                    : CollisionGroup.Default;
            }
        }
    }

    /// <summary>
    /// Manages the BepuPhysics simulation world.
    /// Simplified implementation for easy integration.
    /// </summary>
    public class PhysicsSystem : IDisposable
    {
        public Simulation Simulation { get; private set; }
        private BufferPool bufferPool;

        // Configuration
        public Vector3 Gravity { get; set; } = new Vector3(0, -30f, 0);

        public PhysicsSystem()
        {
            // Create buffer pool
            bufferPool = new BufferPool();

            // Create simulation (single-threaded for simplicity)
            Simulation = Simulation.Create(
                bufferPool,
                new NarrowPhaseCallbacks(),
                new PoseIntegratorCallbacks(Gravity),
                new SolveDescription(8, 1)
            );
        }

        /// <summary>
        /// Update physics simulation.
        /// </summary>
        public void Update(float deltaTime)
        {
            // BepuPhysics requires positive timestep
            if (deltaTime <= 0)
                return;

            // Clamp to avoid spiral of death
            float clampedDt = Math.Min(deltaTime, 1f / 30f);
            Simulation.Timestep(clampedDt);
        }

        /// <summary>
        /// Add a static box collider.
        /// </summary>
        public StaticHandle AddStaticBox(
            Vector3 position,
            Vector3 size,
            CollisionGroup group = CollisionGroup.Wall
        )
        {
            var shape = new Box(size.X, size.Y, size.Z);
            var shapeIndex = Simulation.Shapes.Add(shape);

            var handle = Simulation.Statics.Add(
                new StaticDescription(position, Quaternion.Identity, shapeIndex)
            );
            CollisionRegistry.RegisterStatic(handle, group);
            return handle;
        }

        /// <summary>
        /// Remove a static body.
        /// </summary>
        public void RemoveStatic(StaticHandle handle)
        {
            Simulation.Statics.Remove(handle);
        }

        /// <summary>
        /// Add a dynamic body.
        /// </summary>
        public BodyHandle AddDynamicBody(
            Vector3 position,
            Quaternion rotation,
            CollidableDescription collidable,
            BodyInertia inertia,
            float mass = 1f,
            bool kinematic = false,
            CollisionGroup group = CollisionGroup.Default
        )
        {
            var activity = new BodyActivityDescription(0.01f);

            BodyDescription bodyDescription;
            if (kinematic)
            {
                bodyDescription = BodyDescription.CreateKinematic(position, collidable, activity);
            }
            else
            {
                bodyDescription = BodyDescription.CreateDynamic(
                    position,
                    inertia,
                    collidable,
                    activity
                );
            }

            var handle = Simulation.Bodies.Add(bodyDescription);
            CollisionRegistry.RegisterBody(handle, group);
            return handle;
        }

        /// <summary>
        /// Remove a dynamic body.
        /// </summary>
        public void RemoveBody(BodyHandle handle)
        {
            Simulation.Bodies.Remove(handle);
            CollisionRegistry.UnregisterBody(handle);
        }

        /// <summary>
        /// Raycast into the physics world.
        /// NOTE: Advanced raycasting coming soon. For now, use collision detection.
        /// </summary>
        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RayHit hit)
        {
            // TODO: Implement proper raycasting when API is stable
            // For now, use sphere sweeps or collision detection
            hit = new RayHit();
            return false;
        }

        /// <summary>
        /// Check if a sphere overlaps any physics objects (useful for ground detection).
        /// TODO: Implement proper overlap detection with BepuPhysics queries.
        /// </summary>
        public bool OverlapSphere(Vector3 center, float radius)
        {
            // Placeholder for overlap detection
            // Will be implemented with proper BepuPhysics collision queries
            return false;
        }

        /// <summary>
        /// Get physics body reference for advanced operations.
        /// </summary>
        public BodyReference GetBodyReference(BodyHandle handle)
        {
            return Simulation.Bodies[handle];
        }

        // ===================
        // HELPER METHODS
        // ===================

        /// <summary>
        /// Create a floor (infinite static plane at Y=0).
        /// </summary>
        public StaticHandle CreateFloor()
        {
            return AddStaticBox(
                new Vector3(0, -1, 0),
                new Vector3(1000, 1, 1000),
                CollisionGroup.Wall
            );
        }

        /// <summary>
        /// Create a wall at specified position and size.
        /// </summary>
        public StaticHandle CreateWall(Vector3 position, Vector3 size)
        {
            return AddStaticBox(position, size, CollisionGroup.Wall);
        }

        /// <summary>
        /// Create a platform (static box).
        /// </summary>
        public StaticHandle CreatePlatform(Vector3 position, Vector3 size)
        {
            return AddStaticBox(position, size, CollisionGroup.Wall);
        }

        public void Dispose()
        {
            Simulation.Dispose();
            bufferPool.Clear();
        }
    }

    // ===========================
    // CALLBACKS
    // ===========================

    struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
    {
        public bool AllowContactGeneration(
            int workerIndex,
            CollidableReference a,
            CollidableReference b,
            ref float speculativeMargin
        )
        {
            var groupA = CollisionRegistry.GetGroup(a);
            var groupB = CollisionRegistry.GetGroup(b);

            // Player vs PlayerProjectile
            if (
                (groupA == CollisionGroup.Player && groupB == CollisionGroup.PlayerProjectile)
                || (groupA == CollisionGroup.PlayerProjectile && groupB == CollisionGroup.Player)
            )
                return false;

            // Enemy vs EnemyProjectile
            if (
                (groupA == CollisionGroup.Enemy && groupB == CollisionGroup.EnemyProjectile)
                || (groupA == CollisionGroup.EnemyProjectile && groupB == CollisionGroup.Enemy)
            )
                return false;

            // Projectile vs Projectile
            if (
                (
                    groupA == CollisionGroup.PlayerProjectile
                    || groupA == CollisionGroup.EnemyProjectile
                )
                && (
                    groupB == CollisionGroup.PlayerProjectile
                    || groupB == CollisionGroup.EnemyProjectile
                )
            )
                return false;

            return true;
        }

        public bool AllowContactGeneration(
            int workerIndex,
            CollidablePair pair,
            int childIndexA,
            int childIndexB
        )
        {
            return true;
        }

        public unsafe bool ConfigureContactManifold<TManifold>(
            int workerIndex,
            CollidablePair pair,
            ref TManifold manifold,
            out PairMaterialProperties pairMaterial
        )
            where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial = new PairMaterialProperties
            {
                FrictionCoefficient = 0.5f,
                MaximumRecoveryVelocity = 2f,
            };
            return true;
        }

        public bool ConfigureContactManifold(
            int workerIndex,
            CollidablePair pair,
            int childIndexA,
            int childIndexB,
            ref ConvexContactManifold manifold
        )
        {
            return true;
        }

        public void Initialize(Simulation simulation) { }

        public void Dispose() { }
    }

    public struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
    {
        public Vector3 Gravity;

        public PoseIntegratorCallbacks(Vector3 gravity)
            : this()
        {
            Gravity = gravity;
        }

        public readonly AngularIntegrationMode AngularIntegrationMode =>
            AngularIntegrationMode.Nonconserving;
        public readonly bool AllowSubstepsForUnconstrainedBodies => false;
        public readonly bool IntegrateVelocityForKinematics => false;

        public void Initialize(Simulation simulation) { }

        public void PrepareForIntegration(float dt) { }

        public void IntegrateVelocity(
            Vector<int> bodyIndices,
            Vector3Wide position,
            QuaternionWide orientation,
            BodyInertiaWide localInertia,
            Vector<int> integrationMask,
            int workerIndex,
            Vector<float> dt,
            ref BodyVelocityWide velocity
        )
        {
            velocity.Linear.Y += Gravity.Y * dt;
        }
    }

    /// <summary>
    /// Simple raycast hit result.
    /// </summary>
    public struct RayHit
    {
        public float T;
        public Vector3 Normal;
        public Vector3 HitPoint;

        public Vector3 GetHitPoint(Vector3 rayOrigin, Vector3 rayDirection)
        {
            return rayOrigin + rayDirection * T;
        }
    }
}
