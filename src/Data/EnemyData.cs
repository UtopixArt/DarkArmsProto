using System.Numerics;
using Raylib_cs;

namespace DarkArmsProto.Data
{
    public class EnemyData
    {
        public string SoulType { get; set; } = "Undead";
        public float Health { get; set; } = 100f;
        public float Speed { get; set; } = 4f;
        public float Damage { get; set; } = 10f;
        public float AttackRange { get; set; } = 1.5f;
        public float DetectionRange { get; set; } = 15f;
        public float AttackCooldown { get; set; } = 1.0f;
        public bool IsFlying { get; set; } = false;
        public bool IsRanged { get; set; } = false;
        public string SpritePath { get; set; } = "";
        public float SpriteSize { get; set; } = 3.5f;
        public int[] Color { get; set; } = { 255, 255, 255, 255 };
        public float[] MeshSize { get; set; } = { 1.5f, 4.5f, 1.5f };
        public float[] ColliderSize { get; set; } = { 0.75f, 2.25f, 0.75f };

        public Raylib_cs.Color GetColor()
        {
            return new Raylib_cs.Color(
                (byte)Color[0],
                (byte)Color[1],
                (byte)Color[2],
                (byte)Color[3]
            );
        }

        public Vector3 GetMeshSize()
        {
            return new Vector3(MeshSize[0], MeshSize[1], MeshSize[2]);
        }

        public Vector3 GetColliderSize()
        {
            return new Vector3(ColliderSize[0], ColliderSize[1], ColliderSize[2]);
        }

        public Core.SoulType GetSoulType()
        {
            return SoulType switch
            {
                "Beast" => Core.SoulType.Beast,
                "Undead" => Core.SoulType.Undead,
                "Demon" => Core.SoulType.Demon,
                _ => Core.SoulType.Undead,
            };
        }
    }

    public class EnemiesConfig
    {
        public System.Collections.Generic.List<EnemyData> Enemies { get; set; } =
            new System.Collections.Generic.List<EnemyData>();
    }
}
