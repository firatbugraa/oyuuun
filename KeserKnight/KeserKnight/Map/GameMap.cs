using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using KeserKnight.Entity;

namespace KeserKnight.Map
{
    public static class GameMap
    {
        public static void LoadRoom(int currentRoom, List<Rectangle> platforms, List<Enemy> enemies, List<Gold> roomGolds)
        {
            platforms.Clear();
            enemies.Clear();
            roomGolds.Clear();

            if (currentRoom == 1)
            {
                platforms.Add(new Rectangle(0, 850, 550, 230));
                platforms.Add(new Rectangle(650, 750, 200, 40));
                platforms.Add(new Rectangle(950, 650, 200, 40));
                platforms.Add(new Rectangle(1400, 550, 570, 530));

                enemies.Add(new Enemy(1500, 490, 60, 60, 80));
                roomGolds.Add(new Gold(750, 700, 10, Color.Gold));
                roomGolds.Add(new Gold(1050, 600, 10, Color.Gold));
                roomGolds.Add(new Gold(1450, 500, 50, Color.Cyan));
            }
            else if (currentRoom == 2)
            {
                platforms.Add(new Rectangle(-20, 550, 420, 530));
                platforms.Add(new Rectangle(550, 750, 800, 50));
                platforms.Add(new Rectangle(1500, 650, 420, 630));

                enemies.Add(new Enemy(700, 690, 60, 60, 120));
                enemies.Add(new Enemy(1100, 690, 60, 60, 100));
                roomGolds.Add(new Gold(750, 700, 10, Color.Gold));
                roomGolds.Add(new Gold(950, 700, 50, Color.Cyan));
                roomGolds.Add(new Gold(1150, 700, 10, Color.Gold));
            }
            else if (currentRoom == 3)
            {
                platforms.Add(new Rectangle(0, 450, 400, 630));
                platforms.Add(new Rectangle(400, 1000, 1520, 80));
                platforms.Add(new Rectangle(500, 800, 400, 40));
                platforms.Add(new Rectangle(1200, 800, 500, 40));
                platforms.Add(new Rectangle(600, 550, 700, 40));
                platforms.Add(new Rectangle(1400, 300, 520, 780));

                enemies.Add(new Enemy(600, 740, 60, 60, 80));
                enemies.Add(new Enemy(900, 490, 60, 60, 150));
                roomGolds.Add(new Gold(1450, 750, 10, Color.Gold));
                roomGolds.Add(new Gold(950, 490, 50, Color.Cyan));
                roomGolds.Add(new Gold(1600, 240, 50, Color.Cyan));
            }
        }
    }
}
