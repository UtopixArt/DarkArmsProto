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
        public float Gravity { get; set; } = 30f;
        public float JumpForce { get; set; } = 12f;
        public float VerticalVelocity { get; set; } = 0f;
        public bool IsGrounded { get; private set; } = false;

        public Vector3 RoomCenter { get; set; }

        public List<ColliderComponent>? WallColliders { get; set; }

        // Shooting dependencies (injected)
        public ProjectileManager? ProjectileManager { get; set; }
        public Action<Vector3, float, float>? ExplosionCallback { get; set; }

        private float yaw;
        private float pitch;

        private float footRayLength = 1.5f;
        private float footOffset = 0.8f; // Start higher to avoid starting inside floor
        private float slopeMaxCos = 0.5f; // cos(60°)

        private InputSystem? inputSystem;

        private Vector2 movementInput = Vector2.Zero;

        public override void Start()
        {
            inputSystem = new InputSystem();
            inputSystem?.ForwardEvent += OnForward;
            inputSystem?.RightEvent += OnRight;
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
            var playerCollider = Owner.GetComponent<ColliderComponent>();

            // Input direction
            Vector3 forward = Vector3.Normalize(new Vector3(MathF.Sin(yaw), 0, MathF.Cos(yaw)));
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));

            Vector3 moveDir = Vector3.Zero;
            moveDir += forward * movementInput.Y;
            moveDir += right * movementInput.X;
            if (moveDir != Vector3.Zero)
                moveDir = Vector3.Normalize(moveDir);

            // Jump
            if (IsGrounded && Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                VerticalVelocity = JumpForce;
                IsGrounded = false;
            }

            // Gravity
            VerticalVelocity -= Gravity * deltaTime;
            IsGrounded = false;

            // Ground check via raycast
            if (playerCollider != null && WallColliders != null)
            {
                Vector3 rayOrigin = Owner.Position + new Vector3(0, footOffset, 0);
                Vector3 rayDir = -Vector3.UnitY;
                float bestY = float.MinValue;

                foreach (var wall in WallColliders)
                {
                    if (wall == null)
                        continue;
                    if (
                        wall.Raycast(
                            rayOrigin,
                            rayDir,
                            footRayLength,
                            out float hitDist,
                            out Vector3 hitNormal,
                            out Vector3 hitPoint
                        )
                    )
                    {
                        if (Vector3.Dot(hitNormal, Vector3.UnitY) >= slopeMaxCos)
                        {
                            bestY = MathF.Max(bestY, hitPoint.Y);
                        }
                    }
                }

                if (bestY > float.MinValue)
                {
                    float predictedY = Owner.Position.Y + VerticalVelocity * deltaTime;
                    // Snap if on/above ground and descending
                    if (predictedY <= bestY + 0.05f)
                    {
                        Owner.Position = new Vector3(Owner.Position.X, bestY, Owner.Position.Z);
                        VerticalVelocity = Math.Max(0, VerticalVelocity);
                        IsGrounded = true;
                    }
                }
            }

            // Apply vertical motion (remaining fall)
            if (!IsGrounded)
            {
                Owner.Position += new Vector3(0, VerticalVelocity * deltaTime, 0);
            }

            // Horizontal move + slide
            if (moveDir != Vector3.Zero)
            {
                Vector3 original = Owner.Position;
                Vector3 target = original + moveDir * MoveSpeed * deltaTime;

                if (playerCollider != null && WallColliders != null)
                {
                    Owner.Position = target;
                    bool fullHit = Collides(playerCollider, WallColliders);

                    if (fullHit)
                    {
                        Owner.Position = original;

                        Vector3 xPos = new Vector3(target.X, original.Y, original.Z);
                        Owner.Position = xPos;
                        bool xHit = Collides(playerCollider, WallColliders);

                        Vector3 zPos = new Vector3(original.X, original.Y, target.Z);
                        Owner.Position = zPos;
                        bool zHit = Collides(playerCollider, WallColliders);

                        if (xHit && zHit)
                            Owner.Position = original;
                        else if (xHit)
                            Owner.Position = zPos;
                        else if (zHit)
                            Owner.Position = xPos;
                    }
                }
                else
                {
                    Owner.Position = target;
                }
            }
        }

        private bool Collides(ColliderComponent self, List<ColliderComponent> walls)
        {
            // Position is snapped to floor, so we use it as feet reference
            float feetY = self.Owner.Position.Y;
            float stepHeight = 0.2f; // Tolerance for floor/steps

            foreach (var w in walls)
            {
                if (w != null && self.CheckCollision(w))
                {
                    var (minW, maxW) = w.GetBounds();

                    // Ignore if it's a floor (top is at or below our feet + step tolerance)
                    if (maxW.Y <= feetY + stepHeight)
                        continue;

                    return true;
                }
            }
            return false;
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
