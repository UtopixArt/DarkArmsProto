using System.Collections.Generic;
using System.Numerics;

namespace DarkArmsProto.Core
{
    public class GameObject
    {
        public Vector3 Position { get; set; }
        public bool IsActive { get; set; } = true;

        private string tag = "Untagged";
        public string Tag
        {
            get => tag;
            set
            {
                string oldTag = tag;
                tag = value;
                GameWorld.Instance.UpdateTag(this, oldTag, value);
            }
        }

        private List<Component> components = new List<Component>();

        public GameObject(Vector3 position, string tag = "Untagged")
        {
            Position = position;
            this.tag = tag;
        }

        public void AddComponent(Component component)
        {
            component.Owner = this;
            components.Add(component);
            component.Start();
        }

        public T GetComponent<T>()
            where T : Component
        {
            foreach (var c in components)
            {
                if (c is T typed)
                    return typed;
            }
            return null!;
        }

        public List<Component> GetAllComponents()
        {
            return new List<Component>(components);
        }

        public bool HasTag(string checkTag)
        {
            return tag == checkTag;
        }

        public bool CompareTag(string checkTag)
        {
            return tag == checkTag;
        }

        public virtual void Update(float deltaTime)
        {
            if (!IsActive)
                return;
            foreach (var c in components)
                c.Update(deltaTime);
        }

        public virtual void Render()
        {
            if (!IsActive)
                return;
            foreach (var c in components)
                c.Render();
        }
    }
}
