using System.Collections.Generic;
using System.Numerics;

namespace DarkArmsProto
{
    public class SoulManager
    {
        private List<Soul> souls;
        private WeaponSystem weaponSystem;

        public SoulManager(WeaponSystem weaponSystem)
        {
            souls = new List<Soul>();
            this.weaponSystem = weaponSystem;
        }

        public void SpawnSoul(Vector3 position, SoulType type)
        {
            souls.Add(new Soul(position, type));
        }

        public void Update(float deltaTime, Vector3 playerPosition)
        {
            for (int i = souls.Count - 1; i >= 0; i--)
            {
                souls[i].Update(deltaTime, playerPosition);

                if (souls[i].IsCollected)
                {
                    // Feed soul to weapon
                    weaponSystem.FeedSoul(souls[i].Type);
                    souls.RemoveAt(i);
                }
            }
        }

        public void Render()
        {
            foreach (var soul in souls)
            {
                soul.Render();
            }
        }
    }
}