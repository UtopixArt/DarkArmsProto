using System.Collections.Generic;
using System.Numerics;
using DarkArmsProto.Components;
using DarkArmsProto.Core;
using DarkArmsProto.VFX;
using DarkArmsProto.World;
using Raylib_cs;

namespace DarkArmsProto.Systems
{
    public class CombatSystem
    {
        private GameObject player;
        private RoomManager roomManager;
        private int kills;

        public int Kills => kills;
        public List<DamageNumber> DamageNumbers => DamageNumberManager.GetAll();

        public void IncrementKills()
        {
            kills++;
        }

        public CombatSystem(GameObject player, RoomManager roomManager)
        {
            this.player = player;
            this.roomManager = roomManager;
            this.kills = 0;

            // Subscribe to enemy kill events
            EnemyDeathComponent.OnEnemyKilled += IncrementKills;
        }

        public void TriggerExplosion(Vector3 position, float radius, float damage)
        {
            // Explosion VFX - Let JSON decide color (pass null)
            VFXHelper.SpawnExplosion(position, null, 200);

            // Screen shake
            var screenShake = player.GetComponent<ScreenShakeComponent>();
            if (screenShake != null)
            {
                screenShake.AddTrauma(0.18f);
            }

            // Damage enemies
            var enemies = roomManager.GetCurrentRoomEnemies();
            foreach (var enemy in enemies)
            {
                float dist = Vector3.Distance(position, enemy.Position);

                if (dist <= radius)
                {
                    var health = enemy.GetComponent<HealthComponent>();
                    if (health != null)
                    {
                        // TakeDamage will trigger OnDeath event which handles VFX/souls via EnemyDeathComponent
                        health.TakeDamage(damage);

                        // Manually add damage number since HealthComponent event might not be hooked up globally
                        DamageNumberManager.AddDamageNumber(enemy.Position, damage);
                    }
                }
            }
        }
    }
}
