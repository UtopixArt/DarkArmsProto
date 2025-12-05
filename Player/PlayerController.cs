using System.Numerics;
using Raylib_cs;

namespace DarkArmsProto
{
    public class PlayerController
    {
        public Vector3 Position { get; private set; }
        public float Health { get; private set; }

        private Camera3D camera;
        private float yaw;
        private float pitch;

        private float moveSpeed = 10f;
        private float mouseSensitivity = 0.003f;
        private float maxHealth = 100f;

        private float shakeIntensity = 0f;
        private Vector3 shakeOffset = Vector3.Zero;

        public PlayerController(Vector3 startPosition)
        {
            Position = startPosition;
            Health = maxHealth;

            camera = new Camera3D();
            camera.Position = startPosition;
            camera.Target = startPosition + new Vector3(0, 0, -1);
            camera.Up = new Vector3(0, 1, 0);
            camera.FovY = GameConfig.CameraFOV;
            camera.Projection = CameraProjection.Perspective;

            yaw = 0f;
            pitch = 0f;
        }

        public void Update(float deltaTime)
        {
            HandleMouseLook();
            HandleMovement(deltaTime);
            UpdateCamera();

            ShakeEffect();
        }

        private void ShakeEffect()
        {
            if (shakeIntensity > 0)
            {
                shakeOffset = new Vector3(
                    (float)(Random.Shared.NextDouble() - 0.5) * shakeIntensity,
                    (float)(Random.Shared.NextDouble() - 0.5) * shakeIntensity,
                    0
                );
                shakeIntensity *= GameConfig.ScreenShakeDecay;
            }
            camera.Position += shakeOffset;
        }

        public void AddScreenShake(float intensity)
        {
            shakeIntensity = intensity;
        }

        private void HandleMouseLook()
        {
            Vector2 mouseDelta = Raylib.GetMouseDelta();

            yaw -= mouseDelta.X * mouseSensitivity;
            pitch -= mouseDelta.Y * mouseSensitivity;

            // Clamp pitch to avoid gimbal lock
            pitch = Math.Clamp(pitch, -MathF.PI / 2 + 0.1f, MathF.PI / 2 - 0.1f);
        }

        private void HandleMovement(float deltaTime)
        {
            Vector3 forward = GetForwardVector();
            Vector3 right = GetRightVector();

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
                Position += moveDirection * moveSpeed * deltaTime;
            }

            // Keep player in bounds
            float boundary = GameConfig.PlayerBoundary;
            Position = new Vector3(
                Math.Clamp(Position.X, -boundary, boundary),
                Position.Y,
                Math.Clamp(Position.Z, -boundary, boundary)
            );
        }

        private void UpdateCamera()
        {
            camera.Position = Position;

            // Calculate forward direction from yaw and pitch
            Vector3 forward = new Vector3(
                MathF.Cos(pitch) * MathF.Sin(yaw),
                MathF.Sin(pitch),
                MathF.Cos(pitch) * MathF.Cos(yaw)
            );

            camera.Target = Position + forward;
        }

        private Vector3 GetForwardVector()
        {
            // Forward vector on XZ plane (ignore pitch for movement)
            return Vector3.Normalize(new Vector3(MathF.Sin(yaw), 0, MathF.Cos(yaw)));
        }

        private Vector3 GetRightVector()
        {
            // Right vector perpendicular to forward
            Vector3 forward = GetForwardVector();
            return Vector3.Normalize(Vector3.Cross(forward, new Vector3(0, 1, 0)));
        }

        public Vector3 GetLookDirection()
        {
            return Vector3.Normalize(camera.Target - camera.Position);
        }

        public Camera3D GetCamera()
        {
            return camera;
        }

        public void TakeDamage(float damage)
        {
            Health = Math.Max(0, Health - damage);
        }

        public void Heal(float amount)
        {
            Health = Math.Min(maxHealth, Health + amount);
        }
    }
}
