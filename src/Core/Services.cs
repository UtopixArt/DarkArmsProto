using System;
using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Audio;
using DarkArmsProto.Components;
using Raylib_cs;

namespace DarkArmsProto.Core
{
    /// <summary>
    /// Central access point for all game services (like Unity's static classes).
    /// Allows components to access Input, Physics, Time, Audio without dependencies.
    /// </summary>
    public static class Services
    {
        public static InputService Input { get; } = new InputService();
        public static PhysicsService Physics { get; } = new PhysicsService();
        public static TimeService Time { get; } = new TimeService();
        private static AudioService audioService = new AudioService();
        public static AudioService Audio => audioService;
    }

    /// <summary>
    /// Input service (like Unity's Input class)
    /// </summary>
    public class InputService
    {
        public bool GetKey(KeyboardKey key) => Raylib.IsKeyDown(key);

        public bool GetKeyPressed(KeyboardKey key) => Raylib.IsKeyPressed(key);

        public bool GetKeyReleased(KeyboardKey key) => Raylib.IsKeyReleased(key);

        public bool GetMouseButton(MouseButton button) => Raylib.IsMouseButtonDown(button);

        public bool GetMouseButtonPressed(MouseButton button) =>
            Raylib.IsMouseButtonPressed(button);

        public bool GetMouseButtonReleased(MouseButton button) =>
            Raylib.IsMouseButtonReleased(button);

        public Vector2 GetMousePosition() => Raylib.GetMousePosition();

        public Vector2 GetMouseDelta() => Raylib.GetMouseDelta();
    }

    /// <summary>
    /// Physics service for queries and collision detection (like Unity's Physics)
    /// </summary>
    public class PhysicsService
    {
        /// <summary>
        /// Find all objects within a sphere (like Unity's Physics.OverlapSphere)
        /// </summary>
        public List<GameObject> OverlapSphere(
            Vector3 center,
            float radius,
            string tag = "",
            Func<GameObject, bool>? filter = null
        )
        {
            var results = new List<GameObject>();
            var objects = string.IsNullOrEmpty(tag)
                ? GameWorld.Instance.GetAllActive()
                : GameWorld.Instance.FindAllWithTag(tag);

            foreach (var obj in objects)
            {
                float dist = Vector3.Distance(obj.Position, center);
                if (dist <= radius)
                {
                    if (filter == null || filter(obj))
                    {
                        results.Add(obj);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Find nearest object within radius
        /// </summary>
        public GameObject? FindNearest(
            Vector3 center,
            float maxRadius,
            string tag = "",
            Func<GameObject, bool>? filter = null
        )
        {
            GameObject? nearest = null;
            float nearestDist = maxRadius;

            var objects = string.IsNullOrEmpty(tag)
                ? GameWorld.Instance.GetAllActive()
                : GameWorld.Instance.FindAllWithTag(tag);

            foreach (var obj in objects)
            {
                float dist = Vector3.Distance(obj.Position, center);
                if (dist < nearestDist)
                {
                    if (filter == null || filter(obj))
                    {
                        nearest = obj;
                        nearestDist = dist;
                    }
                }
            }

            return nearest;
        }

        /// <summary>
        /// Check if point collides with any object with tag
        /// </summary>
        public bool CheckPointCollision(Vector3 point, string tag)
        {
            var objects = GameWorld.Instance.FindAllWithTag(tag);
            foreach (var obj in objects)
            {
                var collider = obj.GetComponent<ColliderComponent>();
                if (collider != null && collider.CheckPointCollision(point))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Raycast from point in direction (simplified version)
        /// </summary>
        public bool Raycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            out GameObject? hitObject,
            string tag = ""
        )
        {
            hitObject = null;
            float nearestDist = maxDistance;

            var objects = string.IsNullOrEmpty(tag)
                ? GameWorld.Instance.GetAllActive()
                : GameWorld.Instance.FindAllWithTag(tag);

            foreach (var obj in objects)
            {
                var collider = obj.GetComponent<ColliderComponent>();
                if (collider != null)
                {
                    // Simple sphere-ray intersection
                    Vector3 toObject = obj.Position - origin;
                    float dist = Vector3.Dot(toObject, direction);

                    if (dist > 0 && dist < nearestDist)
                    {
                        Vector3 closestPoint = origin + direction * dist;
                        if (collider.CheckPointCollision(closestPoint))
                        {
                            hitObject = obj;
                            nearestDist = dist;
                        }
                    }
                }
            }

            return hitObject != null;
        }
    }

    /// <summary>
    /// Time service (like Unity's Time class)
    /// </summary>
    public class TimeService
    {
        private float timeScale = 1.0f;

        public float DeltaTime { get; private set; }
        public float UnscaledDeltaTime { get; private set; }
        public float TimeSinceStart { get; private set; }
        public float TimeScale
        {
            get => timeScale;
            set => timeScale = Math.Max(0, value);
        }

        public void Update(float deltaTime)
        {
            UnscaledDeltaTime = deltaTime;
            DeltaTime = deltaTime * timeScale;
            TimeSinceStart += DeltaTime;
        }
    }

    /// <summary>
    /// Audio service (alias to AudioManager for consistency)
    /// </summary>
    public class AudioService
    {
        public void PlaySound(SoundType type, float volume) =>
            AudioManager.Instance.PlaySound(type, volume);

        public void SetMasterVolume(float volume) => AudioManager.Instance.SetMasterVolume(volume);
    }
}
