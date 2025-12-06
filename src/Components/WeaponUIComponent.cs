using System;
using DarkArmsProto.Core;
using Raylib_cs;

namespace DarkArmsProto.Components
{
    public class WeaponUIComponent : Component
    {
        private WeaponComponent? weapon;

        public override void Start()
        {
            weapon = Owner.GetComponent<WeaponComponent>();
        }

        public void RenderUI()
        {
            if (weapon == null)
                return;

            int x = 10;
            int y = Raylib.GetScreenHeight() - 180;

            // Weapon info panel
            Raylib.DrawRectangle(x, y, 400, 170, new Color(0, 0, 0, 200));
            Raylib.DrawText(weapon.WeaponName, x + 10, y + 10, 20, new Color(255, 0, 255, 255));
            Raylib.DrawText(
                $"Stage: {weapon.EvolutionStage}/3",
                x + 10,
                y + 35,
                16,
                new Color(0, 255, 255, 255)
            );

            // Soul bars
            int barY = y + 60;
            DrawSoulBar("Beast", SoulType.Beast, x + 10, barY, new Color(255, 136, 0, 255));
            DrawSoulBar("Undead", SoulType.Undead, x + 10, barY + 25, new Color(0, 255, 0, 255));
            DrawSoulBar("Demon", SoulType.Demon, x + 10, barY + 50, new Color(255, 0, 0, 255));

            // Total progress
            int totalSouls = weapon.TotalSouls;
            int required =
                weapon.EvolutionStage > 3 ? 999 : GetRequiredSouls(weapon.EvolutionStage);
            Raylib.DrawText(
                $"Total Souls: {totalSouls} / {required}",
                x + 10,
                barY + 80,
                14,
                new Color(0, 255, 255, 255)
            );

            if (weapon.CanEvolve)
            {
                Raylib.DrawText("[Press E to Evolve]", x + 10, barY + 100, 16, Color.Green);
            }
        }

        private void DrawSoulBar(string name, SoulType type, int x, int y, Color color)
        {
            if (weapon == null)
                return;

            int count = weapon.AbsorbedSouls[type];
            int maxSouls = Math.Max(10, weapon.TotalSouls);
            float percentage = (float)count / maxSouls;

            Raylib.DrawText(name, x, y, 12, Color.White);
            Raylib.DrawRectangle(x + 70, y, 200, 15, new Color(50, 50, 50, 255));
            Raylib.DrawRectangle(x + 70, y, (int)(200 * percentage), 15, color);
            Raylib.DrawText(count.ToString(), x + 275, y, 12, Color.White);
        }

        private int GetRequiredSouls(int stage)
        {
            return stage switch
            {
                1 => GameConfig.RequiredSoulsStage2,
                2 => GameConfig.RequiredSoulsStage3,
                3 => GameConfig.RequiredSoulsStage4,
                _ => 999,
            };
        }
    }
}
