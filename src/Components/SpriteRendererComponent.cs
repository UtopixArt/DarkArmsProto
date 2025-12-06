using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class SpriteRendererComponent : Component
    {
        private static Dictionary<string, Texture2D> textureCache =
            new Dictionary<string, Texture2D>();

        public Texture2D Texture { get; private set; }
        public Color Color { get; set; } = Color.White;
        public float Size { get; set; } = 1.0f;
        public Vector3 Offset { get; set; } = Vector3.Zero;

        private bool textureLoaded = false;

        public SpriteRendererComponent(string texturePath, float size, Color color)
        {
            if (!textureCache.ContainsKey(texturePath))
            {
                textureCache[texturePath] = Raylib.LoadTexture(texturePath);
            }
            Texture = textureCache[texturePath];
            textureLoaded = true;
            Size = size;
            Color = color;
        }

        public override void Render()
        {
            if (!textureLoaded)
                return;

            // Hit flash effect logic (copied from MeshRendererComponent if needed, or simplified)
            Color drawColor = Color;
            var health = Owner.GetComponent<HealthComponent>();
            if (health != null && health.HitFlashTime > 0)
            {
                // Flash white/red when hit
                drawColor = Color.Red;
            }

            Vector3 drawPos = Owner.Position + Offset;

            // Draw billboard
            // Source rect is full texture
            Rectangle source = new Rectangle(0, 0, Texture.Width, Texture.Height);

            // We can use DrawBillboardRec to specify a source rect if we had a spritesheet
            // But for now DrawBillboard is fine.
            Raylib.DrawBillboard(Game.GameCamera, Texture, drawPos, Size, drawColor);
        }

        // We should probably unload the texture when destroyed, but Component doesn't have OnDestroy yet.
        // For now, we rely on OS cleanup or we can add a destructor/Dispose if needed.
        // Given the prototype nature, it's acceptable.
    }
}
