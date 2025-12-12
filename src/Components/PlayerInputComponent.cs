using System;
using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Audio;
using DarkArmsProto.Core;
using DarkArmsProto.Systems;
using DarkArmsProto.VFX;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class PlayerInputComponent : Component
    {
        public float MoveSpeed { get; set; } = 10f;
        public float MouseSensitivity { get; set; } = 0.003f;
        public float JumpForce { get; set; } = 12f;

        public Vector3 RoomCenter { get; set; }

        // Shooting dependencies (injected)
        public ProjectileManager? ProjectileManager { get; set; }
        public Action<Vector3, float, float>? ExplosionCallback { get; set; }

        private float yaw;
        private float pitch;

        private InputSystem? inputSystem;
        private RigidbodyComponent? rigidbody;

        private Vector2 movementInput = Vector2.Zero;

        public override void Start()
        {
            inputSystem = new InputSystem();
            inputSystem?.ForwardEvent += OnForward;
            inputSystem?.RightEvent += OnRight;

            // Get or create rigidbody component
            rigidbody = Owner.GetComponent<RigidbodyComponent>();
        }

        public override void Update(float deltaTime)
        {
            // Update mouse look first to get latest rotation
            HandleMouseLook();

            inputSystem?.Update();

            // Handle shooting
            HandleShooting();

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

        private void OnForward(Vector2 dir)
        {
            movementInput.Y = dir.Y;
        }

        private void OnRight(Vector2 dir)
        {
            movementInput.X = dir.X;
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
            if (rigidbody == null)
                return;

            // Input direction
            Vector3 forward = Vector3.Normalize(new Vector3(MathF.Sin(yaw), 0, MathF.Cos(yaw)));
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));

            Vector3 moveDir = Vector3.Zero;
            moveDir += forward * movementInput.Y;
            moveDir += right * movementInput.X;
            if (moveDir != Vector3.Zero)
                moveDir = Vector3.Normalize(moveDir);

            // Jump
            if (rigidbody.IsGrounded && Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                rigidbody.SetVerticalVelocity(JumpForce);
            }

            // Horizontal move (rigidbody handles physics and collision)
            if (moveDir != Vector3.Zero)
            {
                rigidbody.Move(moveDir, MoveSpeed, deltaTime);
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

        private void HandleShooting()
        {
            if (!Raylib.IsMouseButtonDown(MouseButton.Left))
                return;

            if (ProjectileManager == null)
                return;

            var cameraComp = Owner.GetComponent<CameraComponent>();
            var weaponComp = Owner.GetComponent<WeaponComponent>();

            if (cameraComp == null || weaponComp == null)
                return;

            var newProjectiles = weaponComp.TryShoot(cameraComp.Camera, ExplosionCallback);

            if (newProjectiles.Count == 0)
                return;

            // Register projectiles to GameWorld
            ProjectileManager.SpawnProjectiles(newProjectiles, false);

            // Shoot feedback
            AudioManager.Instance.PlaySound(SoundType.Shoot, 0.3f);

            // Screen shake
            var screenShake = Owner.GetComponent<ScreenShakeComponent>();
            if (screenShake != null)
            {
                screenShake.AddTrauma(GameConfig.ScreenShakeOnShoot);
            }

            // Muzzle flash VFX
            SpawnMuzzleFlash(newProjectiles[0], cameraComp);
        }

        private void SpawnMuzzleFlash(GameObject firstProjectile, CameraComponent cameraComp)
        {
            var projMesh = firstProjectile.GetComponent<MeshRendererComponent>();
            Color muzzleColor = projMesh != null ? projMesh.Color : Color.Yellow;

            Vector3 muzzlePos;
            var weaponRender = Owner.GetComponent<WeaponRenderComponent>();

            if (weaponRender != null)
            {
                muzzlePos = weaponRender.GetMuzzlePosition();
            }
            else
            {
                muzzlePos =
                    cameraComp.Camera.Position
                    + Vector3.Normalize(cameraComp.Camera.Target - cameraComp.Camera.Position)
                        * 0.5f;
            }

            VFXHelper.SpawnMuzzleFlash(muzzlePos, muzzleColor);
        }
    }
}
