using System.Collections.Generic;
using System.Numerics;

namespace DarkArmsProto.Core
{
    public class GameObject
    {
        public Vector3 Position { get; set; }
        public bool IsActive { get; set; } = true;

        private List<Component> components = new List<Component>();

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

        public void Update(float deltaTime)
        {
            if (!IsActive)
                return;
            foreach (var c in components)
                c.Update(deltaTime);
        }

        public void Render()
        {
            if (!IsActive)
                return;
            foreach (var c in components)
                c.Render();
        }
    }
}
