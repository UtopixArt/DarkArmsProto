using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.World;
using Raylib_cs;

namespace DarkArmsProto.Systems
{
    public class GameUI
    {
        private GameObject player;
        private RoomManager roomManager;

        public GameUI(GameObject player, RoomManager roomManager)
        {
            this.player = player;
            this.roomManager = roomManager;
        }

        /// <summary>
        /// Render all 2D UI elements
        /// </summary>
        public void RenderUI(int kills, bool showColliderDebug)
        {
            var healthComp = player.GetComponent<HealthComponent>();
            float currentHealth = healthComp != null ? healthComp.CurrentHealth : 0;

            var currentRoom = roomManager.CurrentRoom;
            string roomInfo =
                $"Room: ({currentRoom.GridPosition.X}, {currentRoom.GridPosition.Y}) - {currentRoom.Type}";

            // Stats panel
            Raylib.DrawRectangle(10, 10, 300, 100, new Color(0, 0, 0, 200));
            Raylib.DrawText($"HP: {(int)currentHealth}/100", 20, 20, 20, Color.Green);
            Raylib.DrawText($"Kills: {kills}", 20, 45, 20, Color.White);
            Raylib.DrawText(roomInfo, 20, 70, 16, Color.White);

            // Weapon info
            var weaponUI = player.GetComponent<WeaponUIComponent>();
            if (weaponUI != null)
            {
                weaponUI.RenderUI();
            }

            // Minimap
            RenderMinimap();

            // Crosshair
            RenderCrosshair();

            // Debug indicator
            RenderDebugIndicator(showColliderDebug);
        }

        /// <summary>
        /// Render damage numbers in world space
        /// </summary>
        public void RenderDamageNumbers(List<DamageNumber> damageNumbers, Camera3D camera)
        {
            foreach (var dn in damageNumbers)
            {
                var screenPos = Raylib.GetWorldToScreen(dn.Position, camera);
                byte alpha = (byte)(dn.Lifetime * 255);
                Raylib.DrawText(
                    ((int)dn.Damage).ToString(),
                    (int)screenPos.X,
                    (int)screenPos.Y,
                    60,
                    new Color(Color.Yellow.R, Color.Yellow.G, Color.Yellow.B, alpha)
                );
            }
        }

        /// <summary>
        /// Render enemy health bars (called after 3D mode)
        /// </summary>
        public void RenderEnemyHealthBars(List<GameObject> enemies)
        {
            foreach (var enemy in enemies)
            {
                var healthBar = enemy.GetComponent<HealthBarComponent>();
                if (healthBar != null)
                {
                    healthBar.DrawUI();
                }
            }
        }

        private void RenderCrosshair()
        {
            int centerX = Raylib.GetScreenWidth() / 2;
            int centerY = Raylib.GetScreenHeight() / 2;
            Raylib.DrawLine(centerX - 10, centerY, centerX + 10, centerY, Color.White);
            Raylib.DrawLine(centerX, centerY - 10, centerX, centerY + 10, Color.White);
        }

        private void RenderDebugIndicator(bool showColliderDebug)
        {
            if (showColliderDebug)
            {
                Raylib.DrawText(
                    "[F3] Colliders: ON",
                    Raylib.GetScreenWidth() - 170,
                    10,
                    16,
                    Color.Green
                );
            }
            else
            {
                Raylib.DrawText(
                    "[F3] Colliders: OFF",
                    Raylib.GetScreenWidth() - 170,
                    10,
                    16,
                    Color.Gray
                );
            }
        }

        private void RenderMinimap()
        {
            int screenWidth = Raylib.GetScreenWidth();
            int minimapSize = 200;
            int minimapX = screenWidth - minimapSize - 10;
            int minimapY = 10;
            int cellSize = 20;

            // Background
            Raylib.DrawRectangle(
                minimapX - 5,
                minimapY - 5,
                minimapSize + 10,
                minimapSize + 10,
                new Color(0, 0, 0, 200)
            );
            Raylib.DrawText("MAP", minimapX + minimapSize / 2 - 20, minimapY - 25, 16, Color.White);

            var currentRoom = roomManager.CurrentRoom;
            var allRooms = roomManager.GetAllRooms();

            // Find min/max grid positions to center the map
            float minX = float.MaxValue,
                minY = float.MaxValue;
            float maxX = float.MinValue,
                maxY = float.MinValue;

            foreach (var room in allRooms.Values)
            {
                if (room.GridPosition.X < minX)
                    minX = room.GridPosition.X;
                if (room.GridPosition.Y < minY)
                    minY = room.GridPosition.Y;
                if (room.GridPosition.X > maxX)
                    maxX = room.GridPosition.X;
                if (room.GridPosition.Y > maxY)
                    maxY = room.GridPosition.Y;
            }

            float gridWidth = maxX - minX + 1;
            float gridHeight = maxY - minY + 1;

            // Calculate cell size to fit all rooms
            int dynamicCellSize = (int)
                System.Math.Min((minimapSize - 20) / gridWidth, (minimapSize - 20) / gridHeight);
            cellSize = System.Math.Min(cellSize, dynamicCellSize);

            // Draw all visited rooms
            foreach (var room in allRooms.Values)
            {
                if (!room.IsVisited)
                    continue;

                int cellX = minimapX + (int)((room.GridPosition.X - minX) * cellSize) + 10;
                int cellY = minimapY + (int)((room.GridPosition.Y - minY) * cellSize) + 10;

                // Room color based on type
                Color roomColor = room.Type switch
                {
                    RoomType.Start => new Color(0, 255, 0, 255),
                    RoomType.Boss => new Color(255, 0, 0, 255),
                    RoomType.Treasure => new Color(255, 215, 0, 255),
                    RoomType.Shop => new Color(0, 150, 255, 255),
                    _ => room.IsCleared
                        ? new Color(100, 100, 100, 255)
                        : new Color(200, 200, 200, 255),
                };

                // Highlight current room
                if (room == currentRoom)
                {
                    Raylib.DrawRectangle(
                        cellX - 2,
                        cellY - 2,
                        cellSize + 4,
                        cellSize + 4,
                        Color.Yellow
                    );
                    roomColor = new Color(255, 255, 0, 255);
                }

                Raylib.DrawRectangle(cellX, cellY, cellSize, cellSize, roomColor);

                // Draw connections (doors)
                foreach (var dir in room.Connections.Keys)
                {
                    var connectedRoom = room.Connections[dir];
                    if (connectedRoom != null && connectedRoom.IsVisited)
                    {
                        int doorX = cellX + cellSize / 2;
                        int doorY = cellY + cellSize / 2;
                        int doorEndX = doorX;
                        int doorEndY = doorY;

                        switch (dir)
                        {
                            case Direction.North:
                                doorY = cellY;
                                doorEndY = cellY - cellSize / 4;
                                break;
                            case Direction.South:
                                doorY = cellY + cellSize;
                                doorEndY = cellY + cellSize + cellSize / 4;
                                break;
                            case Direction.East:
                                doorX = cellX + cellSize;
                                doorEndX = cellX + cellSize + cellSize / 4;
                                break;
                            case Direction.West:
                                doorX = cellX;
                                doorEndX = cellX - cellSize / 4;
                                break;
                        }

                        Raylib.DrawLine(
                            doorX,
                            doorY,
                            doorEndX,
                            doorEndY,
                            new Color(150, 150, 150, 255)
                        );
                    }
                }
            }
        }
    }
}
