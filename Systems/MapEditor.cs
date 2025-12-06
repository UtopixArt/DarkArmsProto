using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.VFX;
using DarkArmsProto.World;
using Raylib_cs;

namespace DarkArmsProto.Systems
{
    public enum EditorTool
    {
        Platform,
        Spawner,
        Light,
    }

    public class MapEditor
    {
        public bool IsActive { get; private set; } = false;

        private Camera3D editorCamera;
        private EditorTool currentTool = EditorTool.Platform;

        // Preview settings
        private Vector3 previewSize = new Vector3(2, 1, 2);
        private SoulType previewEnemyType = SoulType.Undead;
        private Color previewLightColor = Color.Orange;
        private float previewLightIntensity = 2.0f;

        // Current Room Context
        private Room? currentRoom;

        // Temporary storage for the layout being edited
        private RoomLayout currentLayout = new RoomLayout();

        // Visuals
        private List<GameObject> editorObjects = new List<GameObject>();

        // File Management
        private int currentFileIndex = 0;
        private string CurrentFileName => $"room_{currentFileIndex:D2}";

        // Light Management
        private LightManager? lightManager;
        private Color[] lightColors = new Color[]
        {
            Color.White,
            Color.Red,
            Color.Green,
            Color.Blue,
            Color.Yellow,
            Color.Orange,
            Color.Purple,
            Color.Pink,
        };
        private int currentLightColorIndex = 4; // Default Yellow

        public MapEditor()
        {
            editorCamera = new Camera3D();
            editorCamera.Position = new Vector3(0, 10, 10);
            editorCamera.Target = Vector3.Zero;
            editorCamera.Up = Vector3.UnitY;
            editorCamera.FovY = 45f;
            editorCamera.Projection = CameraProjection.Perspective;
        }

        public void SetLightManager(LightManager lm)
        {
            this.lightManager = lm;
        }

        public void Toggle(Room room, Camera3D gameCamera)
        {
            IsActive = !IsActive;
            currentRoom = room;

            if (IsActive)
            {
                // Sync editor camera with game camera
                editorCamera = gameCamera;
                Raylib.EnableCursor();
                Console.WriteLine("Editor Mode: ON");
            }
            else
            {
                Raylib.DisableCursor();
                Console.WriteLine("Editor Mode: OFF");
            }
        }

        public void Update(float deltaTime)
        {
            if (!IsActive)
                return;

            HandleCameraMovement(deltaTime);
            HandleInput();
            UpdateLights();
        }

        private void UpdateLights()
        {
            if (lightManager == null || currentRoom == null)
                return;

            // Clear static lights and re-add editor lights
            lightManager.ClearStaticLights();

            foreach (var l in currentLayout.Lights)
            {
                Vector3 pos = currentRoom.WorldPosition + new Vector3(l.X, l.Y, l.Z);
                Color color = new Color(l.R, l.G, l.B, (byte)255);
                lightManager.AddStaticLight(pos, color, l.Intensity, 2.0f);
            }

            // Add preview light if tool is active
            if (currentTool == EditorTool.Light)
            {
                Vector3 pos = GetPlacementPosition();
                lightManager.AddStaticLight(pos, previewLightColor, previewLightIntensity, 2.0f);
            }
        }

        private void HandleCameraMovement(float deltaTime)
        {
            float speed = 15.0f;
            if (Raylib.IsKeyDown(KeyboardKey.LeftShift))
                speed *= 2.0f;

            Raylib.UpdateCamera(ref editorCamera, CameraMode.Free);
        }

        private void HandleInput()
        {
            // Tool Selection
            if (Raylib.IsKeyPressed(KeyboardKey.One))
                currentTool = EditorTool.Platform;
            if (Raylib.IsKeyPressed(KeyboardKey.Two))
                currentTool = EditorTool.Spawner;
            if (Raylib.IsKeyPressed(KeyboardKey.Three))
                currentTool = EditorTool.Light;

            // Tool Settings
            if (currentTool == EditorTool.Platform)
            {
                // Resize platform
                // Width (X)
                if (Raylib.IsKeyPressed(KeyboardKey.Right))
                    previewSize.X += 1;
                if (Raylib.IsKeyPressed(KeyboardKey.Left))
                    previewSize.X = Math.Max(1, previewSize.X - 1);

                // Depth (Z)
                if (Raylib.IsKeyPressed(KeyboardKey.Up))
                    previewSize.Z += 1;
                if (Raylib.IsKeyPressed(KeyboardKey.Down))
                    previewSize.Z = Math.Max(1, previewSize.Z - 1);

                // Height (Y)
                if (Raylib.IsKeyPressed(KeyboardKey.PageUp) || Raylib.IsKeyPressed(KeyboardKey.R))
                    previewSize.Y += 1;
                if (Raylib.IsKeyPressed(KeyboardKey.PageDown) || Raylib.IsKeyPressed(KeyboardKey.F))
                    previewSize.Y = Math.Max(1, previewSize.Y - 1);
            }
            else if (currentTool == EditorTool.Spawner)
            {
                // Cycle enemy types
                if (Raylib.IsKeyPressed(KeyboardKey.Right))
                {
                    int type = (int)previewEnemyType;
                    type = (type + 1) % 3; // Assuming 3 types: Beast, Undead, Demon
                    previewEnemyType = (SoulType)type;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Left))
                {
                    int type = (int)previewEnemyType;
                    type = (type - 1);
                    if (type < 0)
                        type = 2;
                    previewEnemyType = (SoulType)type;
                }
            }
            else if (currentTool == EditorTool.Light)
            {
                // Cycle light colors
                if (Raylib.IsKeyPressed(KeyboardKey.Right))
                {
                    currentLightColorIndex = (currentLightColorIndex + 1) % lightColors.Length;
                    previewLightColor = lightColors[currentLightColorIndex];
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Left))
                {
                    currentLightColorIndex =
                        (currentLightColorIndex - 1 + lightColors.Length) % lightColors.Length;
                    previewLightColor = lightColors[currentLightColorIndex];
                }

                // Intensity
                if (Raylib.IsKeyPressed(KeyboardKey.R))
                    previewLightIntensity += 0.5f;
                if (Raylib.IsKeyPressed(KeyboardKey.F))
                    previewLightIntensity = Math.Max(0.5f, previewLightIntensity - 0.5f);
            }

            // Placement
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                PlaceObject();
            }

            // Undo (Remove last object)
            if (
                Raylib.IsKeyPressed(KeyboardKey.Z)
                && (
                    Raylib.IsKeyDown(KeyboardKey.LeftControl)
                    || Raylib.IsKeyDown(KeyboardKey.RightControl)
                )
            )
            {
                UndoLastAction();
            }

            // File Management
            if (Raylib.IsKeyPressed(KeyboardKey.RightBracket)) // ]
            {
                currentFileIndex = Math.Min(99, currentFileIndex + 1);
                LoadLayout(CurrentFileName);
            }
            if (Raylib.IsKeyPressed(KeyboardKey.LeftBracket)) // [
            {
                currentFileIndex = Math.Max(0, currentFileIndex - 1);
                LoadLayout(CurrentFileName);
            }

            // Save / Load
            if (Raylib.IsKeyPressed(KeyboardKey.F5))
                SaveLayout(CurrentFileName);
            if (Raylib.IsKeyPressed(KeyboardKey.F6))
                LoadLayout(CurrentFileName);

            // Clear
            if (Raylib.IsKeyPressed(KeyboardKey.Delete))
                ClearLayout();
        }

        private void UndoLastAction()
        {
            if (editorObjects.Count > 0)
            {
                // Remove visual object
                var lastObj = editorObjects[editorObjects.Count - 1];
                editorObjects.RemoveAt(editorObjects.Count - 1);

                // Remove from data
                // This is tricky because we don't track which list the last object came from easily
                // For now, let's just rebuild the visual list from data or track history.
                // Simpler: Check the last added item in each list and see which one matches the visual object position?
                // Actually, let's just remove from the corresponding list based on what the visual object represents.
                // But we don't store that metadata on the GameObject easily.

                // Alternative: Just remove the last added item from the most recently used list?
                // No, that's error prone.

                // Let's just implement a simple "Clear All" for now and maybe "Remove Last Platform" etc if needed.
                // Or better: Raycast to select and delete? Too complex for now.

                // Let's stick to "Clear" for now, and maybe just remove the last added element from the active tool's list if we track it.

                // Let's try to remove the last element from the list that corresponds to the last visual object.
                // We can tag the visual objects.
            }
        }

        private Vector3 GetPlacementPosition()
        {
            // Simple placement: 5 units in front of camera, snapped to grid
            Vector3 forward = Vector3.Normalize(editorCamera.Target - editorCamera.Position);
            Vector3 pos = editorCamera.Position + forward * 5.0f;

            // Snap to 1.0 grid
            pos.X = MathF.Round(pos.X);
            pos.Y = MathF.Round(pos.Y);
            pos.Z = MathF.Round(pos.Z);

            return pos;
        }

        private void PlaceObject()
        {
            if (currentRoom == null)
                return;

            Vector3 worldPos = GetPlacementPosition();
            Vector3 relativePos = worldPos - currentRoom.WorldPosition;

            switch (currentTool)
            {
                case EditorTool.Platform:
                    currentLayout.Platforms.Add(
                        new PlatformData
                        {
                            X = relativePos.X,
                            Y = relativePos.Y,
                            Z = relativePos.Z,
                            W = previewSize.X,
                            H = previewSize.Y,
                            D = previewSize.Z,
                        }
                    );
                    AddVisual(worldPos, previewSize, Color.Gray);
                    break;

                case EditorTool.Spawner:
                    currentLayout.Spawners.Add(
                        new SpawnerData
                        {
                            X = relativePos.X,
                            Y = relativePos.Y,
                            Z = relativePos.Z,
                            Type = (int)previewEnemyType,
                        }
                    );
                    AddVisual(worldPos, new Vector3(1, 2, 1), Color.Red);
                    break;

                case EditorTool.Light:
                    currentLayout.Lights.Add(
                        new LightData
                        {
                            X = relativePos.X,
                            Y = relativePos.Y,
                            Z = relativePos.Z,
                            R = previewLightColor.R,
                            G = previewLightColor.G,
                            B = previewLightColor.B,
                            Intensity = previewLightIntensity,
                        }
                    );
                    AddVisual(worldPos, new Vector3(0.5f, 0.5f, 0.5f), Color.Yellow);
                    break;
            }
        }

        private void AddVisual(Vector3 pos, Vector3 size, Color color)
        {
            var go = new GameObject(pos);
            go.AddComponent(new MeshRendererComponent(color, size));
            editorObjects.Add(go);
        }

        private void ClearLayout()
        {
            currentLayout = new RoomLayout();
            editorObjects.Clear();
        }

        private void SaveLayout(string name)
        {
            string path = Path.Combine("resources/rooms", name + ".json");
            string json = JsonSerializer.Serialize(
                currentLayout,
                new JsonSerializerOptions { WriteIndented = true }
            );
            File.WriteAllText(path, json);
            Console.WriteLine($"Saved layout to {path}");
        }

        private void LoadLayout(string name)
        {
            string path = Path.Combine("resources/rooms", name + ".json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                currentLayout = JsonSerializer.Deserialize<RoomLayout>(json) ?? new RoomLayout();
                RebuildVisuals();
                Console.WriteLine($"Loaded layout from {path}");
            }
        }

        private void RebuildVisuals()
        {
            editorObjects.Clear();
            if (currentRoom == null)
                return;

            foreach (var p in currentLayout.Platforms)
            {
                Vector3 pos = currentRoom.WorldPosition + new Vector3(p.X, p.Y, p.Z);
                AddVisual(pos, new Vector3(p.W, p.H, p.D), Color.Gray);
            }
            foreach (var s in currentLayout.Spawners)
            {
                Vector3 pos = currentRoom.WorldPosition + new Vector3(s.X, s.Y, s.Z);
                AddVisual(pos, new Vector3(1, 2, 1), Color.Red);
            }
            foreach (var l in currentLayout.Lights)
            {
                Vector3 pos = currentRoom.WorldPosition + new Vector3(l.X, l.Y, l.Z);
                AddVisual(pos, new Vector3(0.5f, 0.5f, 0.5f), Color.Yellow);
            }
        }

        public void Render()
        {
            if (!IsActive)
                return;

            Raylib.BeginMode3D(editorCamera);

            // Draw existing editor objects
            foreach (var obj in editorObjects)
            {
                var mesh = obj.GetComponent<MeshRendererComponent>();
                if (mesh != null)
                {
                    Raylib.DrawCube(
                        obj.Position,
                        mesh.Scale.X,
                        mesh.Scale.Y,
                        mesh.Scale.Z,
                        mesh.Color
                    );
                    Raylib.DrawCubeWires(
                        obj.Position,
                        mesh.Scale.X,
                        mesh.Scale.Y,
                        mesh.Scale.Z,
                        Color.White
                    );
                }
            }

            // Draw Preview
            Vector3 previewPos = GetPlacementPosition();
            Color previewColor = Color.Green;
            Vector3 size = Vector3.One;

            if (currentTool == EditorTool.Platform)
            {
                size = previewSize;
            }
            else if (currentTool == EditorTool.Spawner)
            {
                size = new Vector3(1, 2, 1);
                previewColor = previewEnemyType switch
                {
                    SoulType.Beast => Color.Orange,
                    SoulType.Undead => Color.Green,
                    SoulType.Demon => Color.Red,
                    _ => Color.White,
                };
            }
            else if (currentTool == EditorTool.Light)
            {
                size = new Vector3(0.5f, 0.5f, 0.5f);
                previewColor = previewLightColor;
            }

            Raylib.DrawCubeWires(previewPos, size.X, size.Y, size.Z, previewColor);

            // Draw Room Grid
            if (currentRoom != null)
            {
                float roomSize = GameConfig.RoomSize;
                float wallHeight = GameConfig.WallHeight;
                Vector3 center = currentRoom.WorldPosition;

                Color gridColor = new Color(50, 50, 50, 100);
                int lines = (int)roomSize;
                float halfSize = roomSize / 2;

                // Floor
                for (int i = 0; i <= lines; i++)
                {
                    float offset = -halfSize + i;
                    Raylib.DrawLine3D(
                        center + new Vector3(offset, 0.1f, -halfSize),
                        center + new Vector3(offset, 0.1f, halfSize),
                        gridColor
                    );
                    Raylib.DrawLine3D(
                        center + new Vector3(-halfSize, 0.1f, offset),
                        center + new Vector3(halfSize, 0.1f, offset),
                        gridColor
                    );
                }

                // Ceiling
                for (int i = 0; i <= lines; i++)
                {
                    float offset = -halfSize + i;
                    Raylib.DrawLine3D(
                        center + new Vector3(offset, wallHeight, -halfSize),
                        center + new Vector3(offset, wallHeight, halfSize),
                        gridColor
                    );
                    Raylib.DrawLine3D(
                        center + new Vector3(-halfSize, wallHeight, offset),
                        center + new Vector3(halfSize, wallHeight, offset),
                        gridColor
                    );
                }

                // Walls (North/South)
                for (int i = 0; i <= lines; i++)
                {
                    float offset = -halfSize + i;
                    // North Wall (Z = -halfSize)
                    Raylib.DrawLine3D(
                        center + new Vector3(offset, 0, -halfSize),
                        center + new Vector3(offset, wallHeight, -halfSize),
                        gridColor
                    );
                    // South Wall (Z = halfSize)
                    Raylib.DrawLine3D(
                        center + new Vector3(offset, 0, halfSize),
                        center + new Vector3(offset, wallHeight, halfSize),
                        gridColor
                    );
                }
                // Horizontal lines on walls
                for (int i = 0; i <= (int)wallHeight; i++)
                {
                    Raylib.DrawLine3D(
                        center + new Vector3(-halfSize, i, -halfSize),
                        center + new Vector3(halfSize, i, -halfSize),
                        gridColor
                    );
                    Raylib.DrawLine3D(
                        center + new Vector3(-halfSize, i, halfSize),
                        center + new Vector3(halfSize, i, halfSize),
                        gridColor
                    );
                }
            }

            Raylib.EndMode3D();

            // UI
            Raylib.DrawText(
                $"EDITOR MODE - File: {CurrentFileName}.json",
                10,
                10,
                20,
                Color.Yellow
            );
            Raylib.DrawText($"Tool (1-3): {currentTool}", 10, 40, 20, Color.White);

            if (currentTool == EditorTool.Platform)
            {
                Raylib.DrawText($"Size (Arrows/R/F): {previewSize}", 10, 70, 20, Color.Green);
                Raylib.DrawText(
                    $"Left/Right: Width (X) | Up/Down: Depth (Z) | R/F: Height (Y)",
                    10,
                    95,
                    18,
                    Color.Gray
                );
            }
            if (currentTool == EditorTool.Spawner)
            {
                Color typeColor = previewEnemyType switch
                {
                    SoulType.Beast => Color.Orange,
                    SoulType.Undead => Color.Green,
                    SoulType.Demon => Color.Red,
                    _ => Color.White,
                };
                Raylib.DrawText($"Enemy Type (Arrows): {previewEnemyType}", 10, 70, 20, typeColor);
            }

            if (currentTool == EditorTool.Light)
            {
                Raylib.DrawText(
                    $"Light Color (Arrows): {previewLightColor}",
                    10,
                    70,
                    20,
                    previewLightColor
                );
                Raylib.DrawText(
                    $"Intensity (R/F): {previewLightIntensity:F1}",
                    10,
                    95,
                    20,
                    Color.White
                );
            }

            Raylib.DrawText(
                "WASD: Move | Click: Place | F5: Save | F6: Load | Del: Clear | [ ]: Change File",
                10,
                Raylib.GetScreenHeight() - 30,
                20,
                Color.Gray
            );
        }

        public Camera3D GetCamera() => editorCamera;
    }
}
