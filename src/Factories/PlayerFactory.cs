using System;
using System.Numerics;
using DarkArmsProto.Audio;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.World;

namespace DarkArmsProto.Factories
{
    /// <summary>
    /// Factory for creating the player GameObject with all necessary components.
    /// Centralizes player setup and configuration.
    /// </summary>
    public static class PlayerFactory
    {
        /// <summary>
        /// Create a fully configured player GameObject
        /// </summary>
        public static GameObject Create(Vector3 position, Room currentRoom)
        {
            var player = new GameObject(position, "Player");

            // Collider (must be added before Rigidbody for proper ground detection)
            var colliderComp = new ColliderComponent();
            colliderComp.Size = new Vector3(
                GameConfig.PlayerColliderWidth,
                GameConfig.PlayerColliderHeight,
                GameConfig.PlayerColliderDepth
            );
            player.AddComponent(colliderComp);

            // Rigidbody (physics)
            var rigidbody = new RigidbodyComponent();
            rigidbody.WallColliders = currentRoom.WallColliders;
            rigidbody.GroundRayLength = 10.0f;
            rigidbody.UseColliderBottomForRaycast = true; // Use collider bottom for raycast origin
            rigidbody.ShowDebugRaycast = false; // Disable debug visualization for player
            player.AddComponent(rigidbody);

            // Input & Control
            var inputComp = new PlayerInputComponent();
            inputComp.RoomCenter = currentRoom.WorldPosition;
            player.AddComponent(inputComp);

            // Camera
            var cameraComp = new CameraComponent();
            player.AddComponent(cameraComp);

            // Health
            var healthComp = new HealthComponent();
            healthComp.MaxHealth = GameConfig.PlayerMaxHealth;
            healthComp.CurrentHealth = GameConfig.PlayerMaxHealth;
            player.AddComponent(healthComp);

            // Player damage feedback
            healthComp.OnDamageTaken += (amount) =>
            {
                AudioManager.Instance.PlaySound(SoundType.Hit, 0.5f);

                var shake = player.GetComponent<ScreenShakeComponent>();
                shake?.AddTrauma(0.18f);
            };

            // Screen Shake
            var screenShake = new ScreenShakeComponent();
            player.AddComponent(screenShake);

            // Weapon System
            var weaponComp = new WeaponComponent();
            player.AddComponent(weaponComp);

            var weaponRender = new WeaponRenderComponent();
            player.AddComponent(weaponRender);

            var weaponUI = new WeaponUIComponent();
            player.AddComponent(weaponUI);

            return player;
        }
    }
}
