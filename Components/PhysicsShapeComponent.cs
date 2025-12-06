using System;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using DarkArmsProto.Core;
using DarkArmsProto.Systems;

namespace DarkArmsProto.Components
{
    /// <summary>
    /// Defines the collision shape for physics bodies.
    /// Must be added before RigidbodyComponent.CreateBody() is called.
    /// </summary>
    public class PhysicsShapeComponent : Component
    {
        public enum ShapeType
        {
            Sphere,
            Box,
            Capsule,
            Cylinder
        }

        private PhysicsSystem? physicsSystem;
        private TypedIndex shapeIndex;

        public ShapeType Type { get; private set; } = ShapeType.Sphere;
        public Vector3 Size { get; private set; } = Vector3.One;
        public float Radius { get; private set; } = 0.5f;
        public float Height { get; private set; } = 2f;

        /// <summary>
        /// Initialize with physics system.
        /// </summary>
        public void Initialize(PhysicsSystem physics)
        {
            physicsSystem = physics;
        }

        /// <summary>
        /// Create a sphere shape.
        /// </summary>
        public void SetSphere(float radius)
        {
            if (physicsSystem == null)
            {
                Console.WriteLine("Warning: PhysicsShapeComponent not initialized");
                return;
            }

            Type = ShapeType.Sphere;
            Radius = radius;

            var sphere = new Sphere(radius);
            shapeIndex = physicsSystem.Simulation.Shapes.Add(sphere);
        }

        /// <summary>
        /// Create a box shape.
        /// </summary>
        public void SetBox(Vector3 size)
        {
            if (physicsSystem == null)
            {
                Console.WriteLine("Warning: PhysicsShapeComponent not initialized");
                return;
            }

            Type = ShapeType.Box;
            Size = size;

            var box = new Box(size.X, size.Y, size.Z);
            shapeIndex = physicsSystem.Simulation.Shapes.Add(box);
        }

        /// <summary>
        /// Create a capsule shape (perfect for characters).
        /// </summary>
        public void SetCapsule(float radius, float height)
        {
            if (physicsSystem == null)
            {
                Console.WriteLine("Warning: PhysicsShapeComponent not initialized");
                return;
            }

            Type = ShapeType.Capsule;
            Radius = radius;
            Height = height;

            var capsule = new Capsule(radius, height);
            shapeIndex = physicsSystem.Simulation.Shapes.Add(capsule);
        }

        /// <summary>
        /// Create a cylinder shape.
        /// </summary>
        public void SetCylinder(float radius, float height)
        {
            if (physicsSystem == null)
            {
                Console.WriteLine("Warning: PhysicsShapeComponent not initialized");
                return;
            }

            Type = ShapeType.Cylinder;
            Radius = radius;
            Height = height;

            var cylinder = new Cylinder(radius, height);
            shapeIndex = physicsSystem.Simulation.Shapes.Add(cylinder);
        }

        /// <summary>
        /// Get the shape index for creating a rigidbody.
        /// </summary>
        public TypedIndex GetShapeIndex()
        {
            return shapeIndex;
        }

        /// <summary>
        /// Get the effective radius (for inertia calculation).
        /// </summary>
        public float GetEffectiveRadius()
        {
            return Type switch
            {
                ShapeType.Sphere => Radius,
                ShapeType.Capsule => Radius,
                ShapeType.Cylinder => Radius,
                ShapeType.Box => Math.Max(Size.X, Math.Max(Size.Y, Size.Z)) * 0.5f,
                _ => 0.5f
            };
        }
    }
}
