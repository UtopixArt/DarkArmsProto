using System;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using DarkArmsProto.Core;
using DarkArmsProto.Systems;

namespace DarkArmsProto.Components
{
    /// <summary>
    /// Wrapper component for BepuPhysics dynamic bodies.
    /// Automatically syncs GameObject position with physics simulation.
    /// </summary>
    public class RigidbodyComponent : Component
    {
        private PhysicsSystem? physicsSystem;
        private BodyHandle? bodyHandle;
        private bool isInitialized = false;

        // Configuration
        public float Mass { get; set; } = 1f;
        public bool IsKinematic { get; set; } = false;
        public bool UseGravity { get; set; } = true;
        public Vector3 LinearVelocity { get; set; } = Vector3.Zero;
        public Vector3 AngularVelocity { get; set; } = Vector3.Zero;

        // Lock physics on specific axes (useful for FPS controllers)
        public bool LockRotationX { get; set; } = true;
        public bool LockRotationY { get; set; } = true;
        public bool LockRotationZ { get; set; } = true;

        // Collision Group
        public CollisionGroup Group { get; set; } = CollisionGroup.Default;

        /// <summary>
        /// Initialize with the physics system reference.
        /// </summary>
        public void Initialize(PhysicsSystem physics)
        {
            physicsSystem = physics;
        }

        /// <summary>
        /// Create the physics body in the simulation.
        /// Should be called after setting up the shape component.
        /// </summary>
        public void CreateBody(TypedIndex shapeIndex, float radius = 0.5f)
        {
            if (physicsSystem == null)
            {
                Console.WriteLine("Warning: RigidbodyComponent not initialized with PhysicsSystem");
                return;
            }

            if (isInitialized)
            {
                Console.WriteLine("Warning: RigidbodyComponent already initialized");
                return;
            }

            // Create collidable description
            var collidable = new CollidableDescription(shapeIndex, 0.1f);

            // Calculate inertia based on shape
            BodyInertia inertia;
            if (IsKinematic)
            {
                inertia = new BodyInertia(); // Kinematic bodies don't need inertia
            }
            else
            {
                // Use sphere inertia as default (can be customized per shape)
                inertia = new Sphere(radius).ComputeInertia(Mass);
            }

            // Add body to simulation
            bodyHandle = physicsSystem.AddDynamicBody(
                Owner.Position,
                Quaternion.Identity,
                collidable,
                inertia,
                Mass,
                IsKinematic,
                Group
            );

            isInitialized = true;
        }

        public override void Update(float deltaTime)
        {
            if (!isInitialized || bodyHandle == null || physicsSystem == null)
                return;

            // Auto-cleanup when GameObject is destroyed
            if (!Owner.IsActive)
            {
                Destroy();
                return;
            }

            var bodyReference = physicsSystem.Simulation.Bodies[bodyHandle.Value];

            if (IsKinematic)
            {
                // Kinematic: Update physics from GameObject
                bodyReference.Pose.Position = Owner.Position;
            }
            else
            {
                // Dynamic: Update GameObject from physics
                Owner.Position = bodyReference.Pose.Position;

                // Store velocity for easy access
                LinearVelocity = bodyReference.Velocity.Linear;
                AngularVelocity = bodyReference.Velocity.Angular;

                // Lock rotations if needed (for FPS controllers)
                if (LockRotationX || LockRotationY || LockRotationZ)
                {
                    var velocity = bodyReference.Velocity;
                    if (LockRotationX)
                        velocity.Angular.X = 0;
                    if (LockRotationY)
                        velocity.Angular.Y = 0;
                    if (LockRotationZ)
                        velocity.Angular.Z = 0;
                    bodyReference.Velocity = velocity;
                }
            }
        }

        /// <summary>
        /// Set the velocity of the rigidbody.
        /// </summary>
        public void SetVelocity(Vector3 velocity)
        {
            if (!isInitialized || bodyHandle == null || physicsSystem == null)
                return;

            var bodyReference = physicsSystem.Simulation.Bodies[bodyHandle.Value];
            bodyReference.Velocity.Linear = velocity;
            LinearVelocity = velocity;
        }

        /// <summary>
        /// Add force to the rigidbody.
        /// </summary>
        public void AddForce(Vector3 force)
        {
            if (!isInitialized || bodyHandle == null || physicsSystem == null)
                return;

            var bodyReference = physicsSystem.Simulation.Bodies[bodyHandle.Value];
            bodyReference.Velocity.Linear += force / Mass;
        }

        /// <summary>
        /// Add impulse to the rigidbody (instant velocity change).
        /// </summary>
        public void AddImpulse(Vector3 impulse)
        {
            if (!isInitialized || bodyHandle == null || physicsSystem == null)
                return;

            var bodyReference = physicsSystem.Simulation.Bodies[bodyHandle.Value];
            bodyReference.Velocity.Linear += impulse;
        }

        /// <summary>
        /// Teleport the rigidbody to a new position.
        /// </summary>
        public void Teleport(Vector3 position)
        {
            if (!isInitialized || bodyHandle == null || physicsSystem == null)
                return;

            Owner.Position = position;
            var bodyReference = physicsSystem.Simulation.Bodies[bodyHandle.Value];
            bodyReference.Pose.Position = position;
            bodyReference.Velocity.Linear = Vector3.Zero;
            LinearVelocity = Vector3.Zero;
        }

        /// <summary>
        /// Check if the rigidbody is grounded (has contact below).
        /// Uses simple position and velocity check for now.
        /// </summary>
        public bool IsGrounded()
        {
            if (!isInitialized || bodyHandle == null || physicsSystem == null)
                return false;

            var bodyRef = physicsSystem.Simulation.Bodies[bodyHandle.Value];

            // Simple ground check: low Y position and not moving up significantly
            bool nearGround = Owner.Position.Y <= 0.2f;
            bool notMovingUp = bodyRef.Velocity.Linear.Y <= 0.1f;

            return nearGround && notMovingUp;
        }

        /// <summary>
        /// Get the body handle (useful for advanced physics operations).
        /// </summary>
        public BodyHandle? GetBodyHandle()
        {
            return bodyHandle;
        }

        /// <summary>
        /// Cleanup when component is destroyed.
        /// </summary>
        public void Destroy()
        {
            if (isInitialized && bodyHandle != null && physicsSystem != null)
            {
                physicsSystem.RemoveBody(bodyHandle.Value);
                isInitialized = false;
                bodyHandle = null;
            }
        }
    }
}
