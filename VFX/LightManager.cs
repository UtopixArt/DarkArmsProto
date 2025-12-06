using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace DarkArmsProto.VFX
{
    public struct DynamicLight
    {
        public Vector3 Position;
        public Color Color;
        public float Intensity;
        public float Radius;
        public float Lifetime;
        public float MaxLifetime;
        public bool Flicker;
        public bool IsStatic;
    }

    public class LightManager
    {
        private List<DynamicLight> lights = new List<DynamicLight>();
        private Random random = new Random();
        public Shader LightingShader { get; private set; }
        private bool shaderLoaded = false;
        private int viewPosLoc;
        private int ambientLoc;

        // Shader light locations
        private const int MAX_LIGHTS = 32;
        private int[] enabledLocs = new int[MAX_LIGHTS];
        private int[] typeLocs = new int[MAX_LIGHTS];
        private int[] posLocs = new int[MAX_LIGHTS];
        private int[] targetLocs = new int[MAX_LIGHTS];
        private int[] colorLocs = new int[MAX_LIGHTS];

        public void Initialize()
        {
            try
            {
                // Load shader
                LightingShader = Raylib.LoadShader(
                    "resources/shaders/lighting.vs",
                    "resources/shaders/lighting.fs"
                );

                // Get standard locations
                // LightingShader.Locs[(int)ShaderLocationIndex.VectorView] = Raylib.GetShaderLocation(LightingShader, "viewPos");
                viewPosLoc = Raylib.GetShaderLocation(LightingShader, "viewPos");
                ambientLoc = Raylib.GetShaderLocation(LightingShader, "ambient");

                // Set ambient light
                float[] ambient = new float[] { 0.05f, 0.05f, 0.05f, 1.0f }; // Darker ambient (was 0.2)
                Raylib.SetShaderValue(
                    LightingShader,
                    ambientLoc,
                    ambient,
                    ShaderUniformDataType.Vec4
                );

                // Get light locations
                for (int i = 0; i < MAX_LIGHTS; i++)
                {
                    enabledLocs[i] = Raylib.GetShaderLocation(
                        LightingShader,
                        $"lights[{i}].enabled"
                    );
                    typeLocs[i] = Raylib.GetShaderLocation(LightingShader, $"lights[{i}].type");
                    posLocs[i] = Raylib.GetShaderLocation(LightingShader, $"lights[{i}].position");
                    targetLocs[i] = Raylib.GetShaderLocation(LightingShader, $"lights[{i}].target");
                    colorLocs[i] = Raylib.GetShaderLocation(LightingShader, $"lights[{i}].color");
                }

                shaderLoaded = true;
                Console.WriteLine("Lighting shader loaded successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to load lighting shader: {e.Message}");
                shaderLoaded = false;
            }
        }

        public void UpdateShader(Camera3D camera)
        {
            if (!shaderLoaded)
                return;

            // Update view position
            float[] cameraPos = new float[]
            {
                camera.Position.X,
                camera.Position.Y,
                camera.Position.Z,
            };
            Raylib.SetShaderValue(
                LightingShader,
                viewPosLoc,
                cameraPos,
                ShaderUniformDataType.Vec3
            );

            // Update lights
            // Always keep a directional light at index 0 (Sun/Moon)
            UpdateLight(
                0,
                true,
                0,
                new Vector3(0, 10, 0),
                Vector3.Zero,
                new Color(20, 20, 30, 255)
            );

            // Update dynamic lights
            for (int i = 0; i < MAX_LIGHTS - 1; i++)
            {
                int shaderIndex = i + 1; // Start from 1

                if (i < lights.Count)
                {
                    var light = lights[i];
                    float intensity = light.Intensity;

                    if (!light.IsStatic)
                        intensity *= (light.Lifetime / light.MaxLifetime);

                    if (light.Flicker)
                        intensity *= 0.8f + (float)random.NextDouble() * 0.4f;

                    Color finalColor = new Color(
                        (int)Math.Min(255, light.Color.R * intensity),
                        (int)Math.Min(255, light.Color.G * intensity),
                        (int)Math.Min(255, light.Color.B * intensity),
                        255
                    );

                    UpdateLight(shaderIndex, true, 1, light.Position, Vector3.Zero, finalColor);
                }
                else
                {
                    // Disable unused lights
                    UpdateLight(shaderIndex, false, 1, Vector3.Zero, Vector3.Zero, Color.Black);
                }
            }
        }

        private void UpdateLight(
            int index,
            bool enabled,
            int type,
            Vector3 pos,
            Vector3 target,
            Color color
        )
        {
            Raylib.SetShaderValue(
                LightingShader,
                enabledLocs[index],
                enabled ? 1 : 0,
                ShaderUniformDataType.Int
            );
            Raylib.SetShaderValue(LightingShader, typeLocs[index], type, ShaderUniformDataType.Int);

            float[] posVal = { pos.X, pos.Y, pos.Z };
            Raylib.SetShaderValue(
                LightingShader,
                posLocs[index],
                posVal,
                ShaderUniformDataType.Vec3
            );

            float[] targetVal = { target.X, target.Y, target.Z };
            Raylib.SetShaderValue(
                LightingShader,
                targetLocs[index],
                targetVal,
                ShaderUniformDataType.Vec3
            );

            float[] colorVal = { color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f };
            Raylib.SetShaderValue(
                LightingShader,
                colorLocs[index],
                colorVal,
                ShaderUniformDataType.Vec4
            );
        }

        /// <summary>
        /// Add a temporary light (for explosions, impacts, etc.)
        /// </summary>
        public void AddLight(
            Vector3 position,
            Color color,
            float intensity,
            float radius,
            float lifetime,
            bool flicker = false
        )
        {
            if (lights.Count >= MAX_LIGHTS - 1)
                return; // Reserve 1 for directional

            lights.Add(
                new DynamicLight
                {
                    Position = position,
                    Color = color,
                    Intensity = intensity,
                    Radius = radius,
                    Lifetime = lifetime,
                    MaxLifetime = lifetime,
                    Flicker = flicker,
                    IsStatic = false,
                }
            );
        }

        public void AddStaticLight(
            Vector3 position,
            Color color,
            float intensity,
            float radius,
            bool flicker = false
        )
        {
            if (lights.Count >= MAX_LIGHTS - 1)
                return;

            lights.Add(
                new DynamicLight
                {
                    Position = position,
                    Color = color,
                    Intensity = intensity,
                    Radius = radius,
                    Lifetime = 1.0f,
                    MaxLifetime = 1.0f,
                    Flicker = flicker,
                    IsStatic = true,
                }
            );
        }

        public void ClearStaticLights()
        {
            lights.RemoveAll(l => l.IsStatic);
        }

        /// <summary>
        /// Add an explosion light (intense, orange/red glow)
        /// </summary>
        public void AddExplosionLight(Vector3 position, Color baseColor)
        {
            AddLight(position, baseColor, 5.0f, 10f, 0.8f, flicker: true); // Reduced from 8.0/30.0/1.2
        }

        /// <summary>
        /// Add a muzzle flash light (quick, bright flash)
        /// </summary>
        public void AddMuzzleFlash(Vector3 position, Color color)
        {
            AddLight(position, color, 5.0f, 4f, 0.1f, flicker: true); // Reduced from 10.0/15.0/0.2
        }

        /// <summary>
        /// Add an impact light (medium intensity, short duration)
        /// </summary>
        public void AddImpactLight(Vector3 position, Color color)
        {
            AddLight(position, color, 3.0f, 5f, 0.3f, flicker: false); // Reduced from 5.0/12.0/0.5
        }

        public void Update(float deltaTime)
        {
            for (int i = lights.Count - 1; i >= 0; i--)
            {
                var light = lights[i];
                if (!light.IsStatic)
                {
                    light.Lifetime -= deltaTime;

                    if (light.Lifetime <= 0)
                    {
                        lights.RemoveAt(i);
                    }
                    else
                    {
                        lights[i] = light;
                    }
                }
            }
        }

        /// <summary>
        /// Render all dynamic lights with volumetric effect
        /// </summary>
        public void Render()
        {
            // Enable additive blending for bright glow effect
            Raylib.BeginBlendMode(BlendMode.Additive);

            foreach (var light in lights)
            {
                float fade = light.IsStatic ? 1.0f : (light.Lifetime / light.MaxLifetime);
                float currentIntensity = light.Intensity * fade;

                if (light.Flicker)
                {
                    currentIntensity *= 0.7f + (float)random.NextDouble() * 0.6f;
                }

                // Core bright sphere (Only for non-static lights like projectiles/explosions)
                /*
                if (!light.IsStatic)
                {
                    byte coreAlpha = (byte)(255 * fade * currentIntensity);
                    Color coreColor = new Color(
                        Math.Min(255, (int)(light.Color.R * 1.5f)),
                        Math.Min(255, (int)(light.Color.G * 1.5f)),
                        Math.Min(255, (int)(light.Color.B * 1.5f)),
                        coreAlpha
                    );
                    Raylib.DrawSphere(light.Position, 0.3f * currentIntensity, coreColor);

                    // Multiple glow layers for volumetric effect (Only for non-static)
                    for (int i = 1; i <= 8; i++)
                    {
                        float sizeMult = 1.0f + i * 0.5f;
                        byte alpha = (byte)(180 * fade * currentIntensity / (i * 0.7f));

                        if (alpha > 5) // Skip very transparent layers
                        {
                            Color glowColor = new Color(
                                light.Color.R,
                                light.Color.G,
                                light.Color.B,
                                alpha
                            );

                            float radius = light.Radius * currentIntensity * 0.15f * sizeMult;
                            Raylib.DrawSphere(light.Position, radius, glowColor);
                        }
                    }
                }
                */
            }

            // Reset to normal blending
            Raylib.EndBlendMode();
        }

        public void Cleanup()
        {
            if (shaderLoaded)
            {
                Raylib.UnloadShader(LightingShader);
            }
        }

        public int GetLightCount() => lights.Count;
    }
}
