using System.Numerics;
using Raylib_cs;

namespace DarkArmsProto.World
{
    public class Door
    {
        public Vector3 Position { get; private set; }
        public Direction Direction { get; private set; }
        public bool IsLocked { get; private set; }
        private Room parentRoom;

        private const float DoorWidth = 4f;
        private const float DoorHeight = 3f;
        private const float TriggerRadius = 3.5f; // Increased for easier door activation

        private readonly Color frameColor = new Color(150, 75, 0, 255); // Brownish

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
            float pulse = (float)Math.Sin(Raylib.GetTime() * 3.0f) * 0.2f + 0.8f;
            Color glowCloseColor = new Color(255, 50, 50, (int)(100 * pulse));
            Color glowOpenColor = new Color(50, 255, 50, (int)(100 * pulse));
            if (IsLocked)
            {
                RenderDoor(frameColor, glowCloseColor);
            }
            else
            {
                RenderDoor(frameColor, glowOpenColor);
            }
        }

        private void RenderDoor(Color frameColor, Color barrierColor)
        {
            // Make it pulse

            if (Direction == Direction.North || Direction == Direction.South)
            {
                // Portal veil - Thicker
                Raylib.DrawCubeV(
                    Position + new Vector3(0, DoorHeight / 2f, 0),
                    new Vector3(DoorWidth - 0.2f, DoorHeight, 0.4f),
                    barrierColor
                );

                // Top Frame
                Raylib.DrawCubeV(
                    Position + new Vector3(0, DoorHeight, 0),
                    new Vector3(DoorWidth + 1.0f, 0.5f, 0.6f),
                    frameColor
                );

                // Left Pillar
                Raylib.DrawCubeV(
                    Position + new Vector3(-(DoorWidth / 2f + 0.25f), DoorHeight / 2f, 0),
                    new Vector3(0.5f, DoorHeight, 0.6f),
                    frameColor
                );

                // Right Pillar
                Raylib.DrawCubeV(
                    Position + new Vector3(DoorWidth / 2f + 0.25f, DoorHeight / 2f, 0),
                    new Vector3(0.5f, DoorHeight, 0.6f),
                    frameColor
                );
            }
            else if (Direction == Direction.East || Direction == Direction.West)
            {
                // Portal veil
                Raylib.DrawCubeV(
                    Position + new Vector3(0, DoorHeight / 2f, 0),
                    new Vector3(0.4f, DoorHeight, DoorWidth - 0.2f),
                    barrierColor
                );

                // Top Frame
                Raylib.DrawCubeV(
                    Position + new Vector3(0, DoorHeight, 0),
                    new Vector3(0.6f, 0.5f, DoorWidth + 1.0f),
                    frameColor
                );

                // Left Pillar
                Raylib.DrawCubeV(
                    Position + new Vector3(0, DoorHeight / 2f, -(DoorWidth / 2f + 0.25f)),
                    new Vector3(0.6f, DoorHeight, 0.5f),
                    frameColor
                );

                // Right Pillar
                Raylib.DrawCubeV(
                    Position + new Vector3(0, DoorHeight / 2f, DoorWidth / 2f + 0.25f),
                    new Vector3(0.6f, DoorHeight, 0.5f),
                    frameColor
                );
            }
        }
    }
}
