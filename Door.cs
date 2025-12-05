using System.Numerics;
using Raylib_cs;

namespace DarkArmsProto
{
    public class Door
    {
        public Vector3 Position { get; private set; }
        public Direction Direction { get; private set; }
        public bool IsLocked { get; private set; }
        private Room parentRoom;

        private const float DoorWidth = 4f;
        private const float DoorHeight = 3f;
        private const float TriggerRadius = 2f;

        public Door(Vector3 position, Direction direction, Room parentRoom)
        {
            Position = position;
            Direction = direction;
            this.parentRoom = parentRoom;
            IsLocked = false;
        }

        public void Lock()
        {
            IsLocked = true;
        }

        public void Unlock()
        {
            IsLocked = false;
        }

        public bool CanPass(Vector3 playerPosition)
        {
            if (IsLocked)
                return false;

            float distance = Vector3.Distance(
                new Vector3(playerPosition.X, 0, playerPosition.Z),
                new Vector3(Position.X, 0, Position.Z)
            );

            return distance < TriggerRadius;
        }

        public Room? GetDestinationRoom()
        {
            return parentRoom.GetConnectedRoom(Direction);
        }

        public void Render()
        {
            if (IsLocked)
            {
                // Render locked door (red barrier)
                Color barrierColor = new Color(255, 50, 50, 150);

                if (Direction == Direction.North || Direction == Direction.South)
                {
                    Raylib.DrawCubeV(
                        Position + new Vector3(0, DoorHeight / 2f, 0),
                        new Vector3(DoorWidth, DoorHeight, 0.2f),
                        barrierColor
                    );
                }
                else
                {
                    Raylib.DrawCubeV(
                        Position + new Vector3(0, DoorHeight / 2f, 0),
                        new Vector3(0.2f, DoorHeight, DoorWidth),
                        barrierColor
                    );
                }
            }
            else
            {
                // Render open door (green glow)
                Color glowColor = new Color(50, 255, 50, 100);

                if (Direction == Direction.North || Direction == Direction.South)
                {
                    Raylib.DrawCubeV(
                        Position + new Vector3(0, 0.5f, 0),
                        new Vector3(DoorWidth, 0.1f, 0.2f),
                        glowColor
                    );
                }
                else
                {
                    Raylib.DrawCubeV(
                        Position + new Vector3(0, 0.5f, 0),
                        new Vector3(0.2f, 0.1f, DoorWidth),
                        glowColor
                    );
                }
            }
        }
    }
}
