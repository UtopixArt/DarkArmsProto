using System.Numerics;
using DarkArmsProto.Core;

namespace DarkArmsProto.Components.AI
{
    public interface IEnemyState
    {
        void Enter(EnemyAIComponent enemy);
        void Update(EnemyAIComponent enemy, float deltaTime);
        void Exit(EnemyAIComponent enemy);
    }
}
