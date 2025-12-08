using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Systems
{
    public class InputSystem
    {
        public event Action<Vector2> ForwardEvent;
        public event Action<Vector2> RightEvent;

        private Vector2 _moveDir = Vector2.Zero;

        private void OnForward()
        {
            _moveDir = Vector2.Zero;

            // Use IsKeyDown for continuous input detection
            if (Raylib.IsKeyDown(KeyboardKey.W))
            {
                _moveDir.Y = 1;
            }
            else if (Raylib.IsKeyDown(KeyboardKey.S))
            {
                _moveDir.Y = -1;
            }

            // Trigger the event only if there is movement
            if (_moveDir.Y != 0)
            {
                ForwardEvent?.Invoke(_moveDir);
            }
            else
            {
                ForwardEvent?.Invoke(Vector2.Zero);
            }
        }

        private void OnRight()
        {
            _moveDir = Vector2.Zero;

            // Use IsKeyDown for continuous input detection
            if (Raylib.IsKeyDown(KeyboardKey.D))
            {
                _moveDir.X = 1;
            }
            else if (Raylib.IsKeyDown(KeyboardKey.A))
            {
                _moveDir.X = -1;
            }

            // Trigger the event only if there is movement
            if (_moveDir.X != 0)
            {
                RightEvent?.Invoke(_moveDir);
            }
            else
            {
                RightEvent?.Invoke(Vector2.Zero);
            }
        }

        public void Update()
        {
            OnForward();
            OnRight();
        }
    }
}
