using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace DarkArmsProto.Data
{
    public class ParticleEffectData
    {
        public string Name { get; set; } = "NewEffect";
        public List<ParticleEmitterData> Emitters { get; set; } = new List<ParticleEmitterData>();
    }

    public class ParticleEmitterData
    {
        public int Count { get; set; } = 10;

        // Speed
        public float MinSpeed { get; set; } = 1f;
        public float MaxSpeed { get; set; } = 5f;

        // Lifetime
        public float MinLifetime { get; set; } = 0.5f;
        public float MaxLifetime { get; set; } = 1.0f;

        // Size
        public float MinSize { get; set; } = 0.1f;
        public float MaxSize { get; set; } = 0.3f;

        // Physics
        public float Gravity { get; set; } = -5f;
        public float Drag { get; set; } = 0.95f;

        // Appearance
        public SerializedColor Color { get; set; } = new SerializedColor(255, 255, 255, 255);
        public bool UseRandomColors { get; set; } = false;

        // Emission Shape
        public SerializedVector3 Spread { get; set; } = new SerializedVector3(1, 1, 1);
        public float InitialRadius { get; set; } = 0.1f; // Radius of the emission sphere
        public bool IsBurst { get; set; } = true; // All at once vs over time (for now assume burst)
    }

    public class SerializedVector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public SerializedVector3() { }

        public SerializedVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static implicit operator Vector3(SerializedVector3 v) => new Vector3(v.X, v.Y, v.Z);

        public static implicit operator SerializedVector3(Vector3 v) =>
            new SerializedVector3(v.X, v.Y, v.Z);
    }

    public class SerializedColor
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        public SerializedColor() { }

        public SerializedColor(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color ToRaylib() => new Color(R, G, B, A);
    }
}
