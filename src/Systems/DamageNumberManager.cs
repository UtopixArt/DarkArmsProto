using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Systems
{
    public struct DamageNumber
    {
        public Vector3 Position;
        public float Damage;
        public float Lifetime;
    }

    /// <summary>
    /// Static manager for damage numbers (like VFXHelper).
    /// Centralized so any component can add damage numbers.
    /// </summary>
    public static class DamageNumberManager
    {
        private static List<DamageNumber> damageNumbers = new();

        /// <summary>
        /// Add a damage number at position
        /// </summary>
        public static void AddDamageNumber(Vector3 position, float damage)
        {
            damageNumbers.Add(new DamageNumber
            {
                Position = position,
                Damage = damage,
                Lifetime = 1f
            });
        }

        /// <summary>
        /// Update all damage numbers (call from Game.cs Update)
        /// </summary>
        public static void Update(float deltaTime)
        {
            for (int i = damageNumbers.Count - 1; i >= 0; i--)
            {
                var dn = damageNumbers[i];
                dn.Lifetime -= deltaTime;
                dn.Position += new Vector3(0, deltaTime * 2, 0);
                damageNumbers[i] = dn;

                if (dn.Lifetime <= 0)
                {
                    damageNumbers.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Get all damage numbers for rendering
        /// </summary>
        public static List<DamageNumber> GetAll()
        {
            return damageNumbers;
        }

        /// <summary>
        /// Clear all damage numbers
        /// </summary>
        public static void Clear()
        {
            damageNumbers.Clear();
        }
    }
}
