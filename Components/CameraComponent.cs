using System;
using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class CameraComponent : Component
    {
        public Camera3D Camera;
        public Vector3 Offset { get; set; } = new Vector3(0, 0.8f, 0); // Lowered camera height

        public override void Start()
        {
            Camera = new Camera3D();
            Camera.Position = Owner.Position + Offset;
            Camera.Target = Owner.Position + Offset + new Vector3(0, 0, 1);
            Camera.Up = new Vector3(0, 1, 0);
            Camera.FovY = 60.0f;
            Camera.Projection = CameraProjection.Perspective;
        }

        public override void Update(float deltaTime)
        {
            // Camera is now updated directly in PlayerInputComponent for better sync
            // This prevents any frame delay between movement and camera update
        }

        public void UpdateRotation(float yaw, float pitch)
        {
            // Calculate forward direction from yaw and pitch
            Vector3 forward = new Vector3(
                MathF.Cos(pitch) * MathF.Sin(yaw),
                MathF.Sin(pitch),
                MathF.Cos(pitch) * MathF.Cos(yaw)
            );

            // Update target based on current camera position (already updated)
            Camera.Target = Camera.Position + forward;
        }
    }
}
