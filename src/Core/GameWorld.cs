using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkArmsProto.Core
{
    /// <summary>
    /// Central registry for all GameObjects in the scene (like Unity's Scene).
    /// Allows components to find objects without manual dependency injection.
    /// </summary>
    public class GameWorld
    {
        private static GameWorld? instance;
        public static GameWorld Instance => instance ??= new GameWorld();

        private List<GameObject> allObjects = new();
        private Dictionary<string, List<GameObject>> objectsByTag = new();
        private GameObject? playerObject;

        public GameObject? Player => playerObject;

        /// <summary>
        /// Register a GameObject to the world
        /// </summary>
        public void Register(GameObject obj, string tag = "Untagged")
        {
            if (!allObjects.Contains(obj))
            {
                allObjects.Add(obj);
            }

            if (!string.IsNullOrEmpty(tag))
            {
                obj.Tag = tag;
                AddToTagIndex(obj, tag);

                // Cache player reference for fast access
                if (tag == "Player")
                {
                    playerObject = obj;
                }
            }
        }

        /// <summary>
        /// Unregister a GameObject from the world
        /// </summary>
        public void Unregister(GameObject obj)
        {
            allObjects.Remove(obj);
            RemoveFromTagIndex(obj, obj.Tag);

            if (obj == playerObject)
            {
                playerObject = null;
            }
        }

        /// <summary>
        /// Find first GameObject with tag (like Unity's GameObject.FindWithTag)
        /// </summary>
        public GameObject? FindWithTag(string tag)
        {
            if (objectsByTag.TryGetValue(tag, out var list))
            {
                return list.FirstOrDefault(o => o.IsActive);
            }
            return null;
        }

        /// <summary>
        /// Find all GameObjects with tag (like Unity's GameObject.FindGameObjectsWithTag)
        /// </summary>
        public List<GameObject> FindAllWithTag(string tag)
        {
            if (objectsByTag.TryGetValue(tag, out var list))
            {
                return list.Where(o => o.IsActive).ToList();
            }
            return new List<GameObject>();
        }

        /// <summary>
        /// Get all active GameObjects
        /// </summary>
        public List<GameObject> GetAllActive()
        {
            return allObjects.Where(o => o.IsActive).ToList();
        }

        /// <summary>
        /// Find all components of a specific type (like Unity's FindObjectsOfType)
        /// </summary>
        public List<T> FindComponentsOfType<T>() where T : Component
        {
            var results = new List<T>();
            foreach (var obj in allObjects)
            {
                if (!obj.IsActive)
                    continue;

                var comp = obj.GetComponent<T>();
                if (comp != null)
                {
                    results.Add(comp);
                }
            }
            return results;
        }

        /// <summary>
        /// Clear all registered objects (use when changing scenes)
        /// </summary>
        public void Clear()
        {
            allObjects.Clear();
            objectsByTag.Clear();
            playerObject = null;
        }

        private void AddToTagIndex(GameObject obj, string tag)
        {
            if (!objectsByTag.ContainsKey(tag))
            {
                objectsByTag[tag] = new List<GameObject>();
            }

            if (!objectsByTag[tag].Contains(obj))
            {
                objectsByTag[tag].Add(obj);
            }
        }

        private void RemoveFromTagIndex(GameObject obj, string tag)
        {
            if (objectsByTag.TryGetValue(tag, out var list))
            {
                list.Remove(obj);
            }
        }

        /// <summary>
        /// Update tag for an object (if it changes at runtime)
        /// </summary>
        public void UpdateTag(GameObject obj, string oldTag, string newTag)
        {
            RemoveFromTagIndex(obj, oldTag);
            AddToTagIndex(obj, newTag);

            if (newTag == "Player")
            {
                playerObject = obj;
            }
            else if (oldTag == "Player" && obj == playerObject)
            {
                playerObject = null;
            }
        }
    }
}
