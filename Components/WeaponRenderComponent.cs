using System;
using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class WeaponRenderComponent : Component
    {
        public Vector3 Offset { get; set; } = new Vector3(0.5f, -0.4f, 0.8f); // Right, Down, Forward relative to camera
        public Vector3 RecoilOffset { get; private set; } = Vector3.Zero;

        private float recoilTimer = 0f;
        private float recoilDuration = 0.15f;
        private float recoilAmount = 0.3f;

        private Camera3D camera;

        public void SetCamera(Camera3D cam)
        {
            this.camera = cam;
        }

        public void TriggerRecoil()
        {
            recoilTimer = recoilDuration;
        }

        public override void Update(float deltaTime)
        {
            // Update camera reference from Game if needed, or assume it's passed/updated
            // For now we'll get it from the player's camera component if possible,
            // but since we need it for rendering relative to view, we might need to fetch it.
            var camComp = Owner.GetComponent<CameraComponent>();
            if (camComp != null)
            {
                camera = camComp.Camera;
            }

            // Handle recoil animation
            if (recoilTimer > 0)
            {
                recoilTimer -= deltaTime;
                float progress = 1.0f - (recoilTimer / recoilDuration);

                // Simple kickback and return
                float zRecoil = 0;
                if (progress < 0.2f) // Kick back fast
                {
                    zRecoil = -recoilAmount * (progress / 0.2f);
                }
                else // Return slow
                {
                    zRecoil = -recoilAmount * (1.0f - (progress - 0.2f) / 0.8f);
                }

                RecoilOffset = new Vector3(0, 0, zRecoil);
            }
            else
            {
                RecoilOffset = Vector3.Zero;
            }
        }

        public Vector3 GetMuzzlePosition()
        {
            // Calculate world position of the muzzle based on camera transform
            Vector3 forward = Vector3.Normalize(camera.Target - camera.Position);
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, new Vector3(0, 1, 0)));
            Vector3 up = Vector3.Cross(right, forward);

            // Apply offset relative to camera orientation
            Vector3 weaponPos =
                camera.Position + right * Offset.X + up * Offset.Y + forward * Offset.Z;

            // Apply recoil (local Z is backward)
            weaponPos += forward * RecoilOffset.Z;

            // Muzzle is at the tip of the weapon
            return weaponPos + forward * 0.8f; // Length of weapon
        }

        public override void Render()
        {
            // We need to construct the weapon matrix manually or just calculate positions
            Vector3 forward = Vector3.Normalize(camera.Target - camera.Position);
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, new Vector3(0, 1, 0)));
            Vector3 up = Vector3.Cross(right, forward);

            Vector3 weaponPos =
                camera.Position + right * Offset.X + up * Offset.Y + forward * Offset.Z;

            weaponPos += forward * RecoilOffset.Z;

            // Calculate end point for capsule
            Vector3 endPos = weaponPos + forward * 0.8f;

            // Draw main barrel
            Raylib.DrawCapsule(weaponPos, endPos, 0.08f, 8, 8, new Color(192, 192, 192, 255)); // Light Gray (Silver)

            // Draw handle/body part (simple box approximation)
            Vector3 handlePos = weaponPos - up * 0.15f - forward * 0.1f;
            Raylib.DrawCube(handlePos, 0.15f, 0.3f, 0.2f, new Color(80, 80, 80, 255)); // Dark Gray
        }
    }
}
