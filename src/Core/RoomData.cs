using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace DarkArmsProto.Core
{
    public class RoomLayout
    {
        public List<PlatformData> Platforms { get; set; } = new List<PlatformData>();
        public List<SpawnerData> Spawners { get; set; } = new List<SpawnerData>();
        public List<LightData> Lights { get; set; } = new List<LightData>();
    }

    public class PlatformData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }
        public float H { get; set; }
        public float D { get; set; }
    }

    public class SpawnerData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public int Type { get; set; } // Cast to SoulType
    }

    public class LightData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public float Intensity { get; set; } = 2.0f;
        public float Radius { get; set; } = 2.0f;
    }
}
