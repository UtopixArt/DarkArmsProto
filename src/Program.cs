using DarkArmsProto;
using Raylib_cs;

class Program
{
    static void Main()
    {
        // Initialize window
        Raylib.InitWindow(1280, 720, "Dark Arms - Prototype");
        Raylib.SetTargetFPS(60);
        Raylib.DisableCursor();

        // Create game instance
        var game = new Game();
        game.Initialize();

        // Main game loop
        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();

            game.Update(deltaTime);
            game.Render();
        }

        // Cleanup
        game.Cleanup();
        Raylib.CloseWindow();
    }
}
