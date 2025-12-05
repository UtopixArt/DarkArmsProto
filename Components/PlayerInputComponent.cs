using System;
using System.Collections.Generic;
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
        public List<ColliderComponent>? WallColliders { get; set; }

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
                Vector3 newPosition = Owner.Position + moveDirection * MoveSpeed * deltaTime;

                // Check collision with walls using proper AABB collision
                var playerCollider = Owner.GetComponent<ColliderComponent>();
                if (playerCollider != null && WallColliders != null)
                {
                    Vector3 originalPosition = Owner.Position;
                    Owner.Position = newPosition;

                    // Check if new position collides with any wall
                    bool collided = false;
                    foreach (var wall in WallColliders)
                    {
                        if (wall != null && playerCollider.CheckCollision(wall))
                        {
                            collided = true;
                            break;
                        }
                    }

                    // If collision detected, try sliding along walls
                    if (collided)
                    {
                        Owner.Position = originalPosition;

                        // Try X-axis only movement
                        Vector3 xOnlyMove =
                            originalPosition
                            + new Vector3(moveDirection.X * MoveSpeed * deltaTime, 0, 0);
                        Owner.Position = xOnlyMove;
                        bool xCollides = false;
                        foreach (var wall in WallColliders)
                        {
                            if (wall != null && playerCollider.CheckCollision(wall))
                            {
                                xCollides = true;
                                break;
                            }
                        }

                        if (xCollides)
                        {
                            // Try Z-axis only movement
                            Owner.Position = originalPosition;
                            Vector3 zOnlyMove =
                                originalPosition
                                + new Vector3(0, 0, moveDirection.Z * MoveSpeed * deltaTime);
                            Owner.Position = zOnlyMove;
                            bool zCollides = false;
                            foreach (var wall in WallColliders)
                            {
                                if (wall != null && playerCollider.CheckCollision(wall))
                                {
                                    zCollides = true;
                                    break;
                                }
                            }

                            // If both axes collide, stay at original position
                            if (zCollides)
                            {
                                Owner.Position = originalPosition;
                            }
                        }
                    }
                }
                else
                {
                    // Fallback to simple boundary clamp if no colliders
                    Owner.Position = newPosition;
                    Owner.Position = new Vector3(
                        Math.Clamp(
                            Owner.Position.X,
                            RoomCenter.X - Boundary,
                            RoomCenter.X + Boundary
                        ),
                        Owner.Position.Y,
                        Math.Clamp(
                            Owner.Position.Z,
                            RoomCenter.Z - Boundary,
                            RoomCenter.Z + Boundary
                        )
                    );
                }
            }
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
