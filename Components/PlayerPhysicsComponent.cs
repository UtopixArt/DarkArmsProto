using System;
using System.Numerics;
using DarkArmsProto.Core;
using DarkArmsProto.Systems;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    /// <summary>
    /// FPS player controller using BepuPhysics (kinematic rigidbody).
    /// This is a ready-to-use replacement for PlayerInputComponent.
    ///
    /// SETUP EXAMPLE:
    /// <code>
    /// // In Game.cs Initialize():
    /// var player = new GameObject(startPosition);
    ///
    /// // 1. Add physics shape
    /// var shape = new PhysicsShapeComponent();
    /// shape.Initialize(physicsSystem);
    /// shape.SetCapsule(0.4f, 1.6f);
    /// player.AddComponent(shape);
    ///
    /// // 2. Add rigidbody
    /// var rb = new RigidbodyComponent();
    /// rb.Initialize(physicsSystem);
    /// rb.IsKinematic = true;
    /// rb.LockRotationX = true;
    /// rb.LockRotationY = true;
    /// rb.LockRotationZ = true;
    /// rb.CreateBody(shape.GetShapeIndex(), shape.GetEffectiveRadius());
    /// player.AddComponent(rb);
    ///
    /// // 3. Add this controller
    /// var controller = new PlayerPhysicsComponent();
    /// player.AddComponent(controller);
    /// </code>
    /// </summary>
    public class PlayerPhysicsComponent : Component
    {
        // References
        private RigidbodyComponent? rigidbody;

        // Movement settings
        public float MoveSpeed { get; set; } = 10f;
        public float MouseSensitivity { get; set; } = 0.003f;
        public float JumpForce { get; set; } = 12f;
        public float Gravity { get; set; } = 30f;

        // State
        private float yaw;
        private float pitch;
        private float verticalVelocity = 0f;

        public override void Start()
        {
            rigidbody = Owner.GetComponent<RigidbodyComponent>();

            if (rigidbody == null)
            {
                Console.WriteLine("ERROR: PlayerPhysicsComponent requires RigidbodyComponent!");
            }
        }

        public override void Update(float deltaTime)
        {
            if (rigidbody == null)
                return;

            // Mouse look
            HandleMouseLook();

            // Movement with physics
            HandlePhysicsMovement(deltaTime);

            // Update camera
            UpdateCamera();
        }

        private void HandleMouseLook()
        {
            Vector2 mouseDelta = Raylib.GetMouseDelta();
            yaw -= mouseDelta.X * MouseSensitivity;
            pitch -= mouseDelta.Y * MouseSensitivity;
            pitch = Math.Clamp(pitch, -MathF.PI / 2 + 0.1f, MathF.PI / 2 - 0.1f);
        }

        private void HandlePhysicsMovement(float deltaTime)
        {
            if (rigidbody == null)
                return;

            // Check if grounded
            bool isGrounded = rigidbody.IsGrounded();

            // === VERTICAL MOVEMENT ===
            if (isGrounded)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Space))
                {
                    verticalVelocity = JumpForce;
                }
                else
                {
                    verticalVelocity = 0;
                }
            }
            else
            {
                verticalVelocity -= Gravity * deltaTime;
            }

            // Clamp fall speed
            verticalVelocity = Math.Max(verticalVelocity, -50f);

            // === HORIZONTAL MOVEMENT ===
            Vector3 forward = new Vector3(MathF.Sin(yaw), 0, MathF.Cos(yaw));
            Vector3 right = Vector3.Cross(Vector3.UnitY, forward);

            Vector3 moveDir = Vector3.Zero;
            if (Raylib.IsKeyDown(KeyboardKey.W)) moveDir += forward;
            if (Raylib.IsKeyDown(KeyboardKey.S)) moveDir -= forward;
            if (Raylib.IsKeyDown(KeyboardKey.D)) moveDir += right;
            if (Raylib.IsKeyDown(KeyboardKey.A)) moveDir -= right;

            if (moveDir != Vector3.Zero)
                moveDir = Vector3.Normalize(moveDir);

            // === COMBINE AND APPLY ===
            Vector3 velocity = new Vector3(
                moveDir.X * MoveSpeed,
                verticalVelocity,
                moveDir.Z * MoveSpeed
            );

            Vector3 newPosition = Owner.Position + velocity * deltaTime;

            // Ground clamp
            if (newPosition.Y < 0)
            {
                newPosition.Y = 0;
                verticalVelocity = 0;
            }

            // Apply movement
            rigidbody.Teleport(newPosition);
        }

        private void UpdateCamera()
        {
            var camera = Owner.GetComponent<CameraComponent>();
            if (camera != null)
            {
                camera.Camera.Position = Owner.Position + camera.Offset;
                camera.UpdateRotation(yaw, pitch);
            }
        }

        /// <summary>
        /// Get the direction the player is looking.
        /// </summary>
        public Vector3 GetLookDirection()
        {
            return Vector3.Normalize(new Vector3(
                MathF.Cos(pitch) * MathF.Sin(yaw),
                MathF.Sin(pitch),
                MathF.Cos(pitch) * MathF.Cos(yaw)
            ));
        }

        /// <summary>
        /// Get current forward direction (horizontal only).
        /// </summary>
        public Vector3 GetForwardDirection()
        {
            return new Vector3(MathF.Sin(yaw), 0, MathF.Cos(yaw));
        }
    }
}
