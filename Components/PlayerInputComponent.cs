using System;
using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class PlayerInputComponent : Component
    {
        public float MoveSpeed { get; set; } = 10f;
        public float MouseSensitivity { get; set; } = 0.003f;

        public Vector3 RoomCenter { get; set; }
        public float Boundary { get; set; } = 9f;

        private float yaw;
        private float pitch;

        public override void Update(float deltaTime)
        {
            // Update mouse look first to get latest rotation
            HandleMouseLook();

            // Move player based on current rotation
            HandleMovement(deltaTime);

            // Force camera update after movement to ensure sync
            var cameraComp = Owner.GetComponent<CameraComponent>();
            if (cameraComp != null)
            {
                cameraComp.Camera.Position = Owner.Position + cameraComp.Offset;
                cameraComp.UpdateRotation(yaw, pitch);
            }
        }

        private void HandleMouseLook()
        {
            Vector2 mouseDelta = Raylib.GetMouseDelta();
            yaw -= mouseDelta.X * MouseSensitivity;
            pitch -= mouseDelta.Y * MouseSensitivity;
            pitch = Math.Clamp(pitch, -MathF.PI / 2 + 0.1f, MathF.PI / 2 - 0.1f);
        }

        private void HandleMovement(float deltaTime)
        {
            Vector3 forward = Vector3.Normalize(new Vector3(MathF.Sin(yaw), 0, MathF.Cos(yaw)));
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, new Vector3(0, 1, 0)));

            Vector3 moveDirection = Vector3.Zero;
            if (Raylib.IsKeyDown(KeyboardKey.W))
                moveDirection += forward;
            if (Raylib.IsKeyDown(KeyboardKey.S))
                moveDirection -= forward;
            if (Raylib.IsKeyDown(KeyboardKey.A))
                moveDirection -= right;
            if (Raylib.IsKeyDown(KeyboardKey.D))
                moveDirection += right;

            if (moveDirection != Vector3.Zero)
            {
                moveDirection = Vector3.Normalize(moveDirection);
                Owner.Position += moveDirection * MoveSpeed * deltaTime;
            }

            Owner.Position = new Vector3(
                Math.Clamp(Owner.Position.X, RoomCenter.X - Boundary, RoomCenter.X + Boundary),
                Owner.Position.Y,
                Math.Clamp(Owner.Position.Z, RoomCenter.Z - Boundary, RoomCenter.Z + Boundary)
            );
        }

        public Vector3 GetLookDirection()
        {
            return Vector3.Normalize(
                new Vector3(
                    MathF.Cos(pitch) * MathF.Sin(yaw),
                    MathF.Sin(pitch),
                    MathF.Cos(pitch) * MathF.Cos(yaw)
                )
            );
        }
    }
}
