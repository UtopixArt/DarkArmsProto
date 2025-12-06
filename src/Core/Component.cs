namespace DarkArmsProto.Core
{
    public abstract class Component
    {
        public GameObject Owner { get; set; } = null!;

        public virtual void Start() { }

        public virtual void Update(float deltaTime) { }

        public virtual void Render() { }
    }
}
