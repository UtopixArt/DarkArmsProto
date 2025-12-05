using DarkArmsProto.Core;

namespace DarkArmsProto.Components
{
    public class EnemyComponent : Component
    {
        public SoulType Type { get; set; }

        public EnemyComponent(SoulType type)
        {
            Type = type;
        }
    }
}
