using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using KeserKnight.Entity;

namespace KeserKnight.Map
{
    public static class GameMap
    {
        private static Dictionary<int, List<Rectangle>> allPlatforms = new Dictionary<int, List<Rectangle>>();
        private static Dictionary<int, List<Enemy>> allEnemies = new Dictionary<int, List<Enemy>>();
        private static Dictionary<int, List<Gold>> allGolds = new Dictionary<int, List<Gold>>();

        private static bool isInitialized = false;

        public static void InitializeWorld()
        {
            allPlatforms.Clear();
            allEnemies.Clear();
            allGolds.Clear();

            // ODA 1
            allPlatforms[1] = new List<Rectangle> {
                new Rectangle(0, 850, 550, 230),
                new Rectangle(650, 750, 200, 40),
                new Rectangle(950, 650, 200, 40),
                new Rectangle(1400, 550, 570, 530)
            };
            allEnemies[1] = new List<Enemy> { new Enemy(1500, 420, 130, 130, 80) };
            allGolds[1] = new List<Gold> {
                new Gold(750, 700, 10, Color.Gold),
                new Gold(1050, 600, 10, Color.Gold),
                new Gold(1450, 500, 50, Color.Cyan)
            };

            // ODA 2
            allPlatforms[2] = new List<Rectangle> {
                new Rectangle(-20, 550, 420, 530),
                new Rectangle(550, 750, 800, 50),
                new Rectangle(1500, 650, 420, 630)
            };
            allEnemies[2] = new List<Enemy> {
                new Enemy(700, 690, 60, 60, 120),
                new Enemy(1100, 690, 60, 60, 100)
            };
            allGolds[2] = new List<Gold> {
                new Gold(750, 700, 10, Color.Gold),
                new Gold(950, 700, 50, Color.Cyan),
                new Gold(1150, 700, 10, Color.Gold)
            };

            // ODA 3
            allPlatforms[3] = new List<Rectangle> {
                new Rectangle(0, 650, 400, 430),
                new Rectangle(400, 1020, 1520, 60),
                new Rectangle(480, 850, 350, 40),
                new Rectangle(580, 650, 650, 40),
                new Rectangle(1280, 800, 400, 40),
                new Rectangle(1450, 450, 470, 630)
            };
            allEnemies[3] = new List<Enemy> {
                new Enemy(650, 590, 60, 60, 120),
                new Enemy(950, 590, 60, 60, 100)
            };
            allGolds[3] = new List<Gold> {
                new Gold(550, 800, 10, Color.Gold),
                new Gold(900, 590, 50, Color.Cyan),
                new Gold(1600, 390, 50, Color.Cyan)
            };

            isInitialized = true;
        }

        // Kilit Çözüm: Odayı yüklemeden önce her şeyi temizleyip yenisini bağlıyoruz 
        public static void LoadRoom(int currentRoom, List<Rectangle> activePlatforms, List<Enemy> activeEnemies, List<Gold> activeGolds)
        {
            if (!isInitialized) InitializeWorld();

            activePlatforms.Clear();
            activeEnemies.Clear();
            activeGolds.Clear();

            if (allPlatforms.ContainsKey(currentRoom)) activePlatforms.AddRange(allPlatforms[currentRoom]);
            if (allEnemies.ContainsKey(currentRoom)) activeEnemies.AddRange(allEnemies[currentRoom]);
            if (allGolds.ContainsKey(currentRoom)) activeGolds.AddRange(allGolds[currentRoom]);
        }

        // --- DÜNYA HAFIZASI KAYIT MOTORU ---
        // Oyuncu odadan çıkarken o odanın son temizlenmiş halini (kesilen canavarları, toplanan coinleri) mühürler usta!
        public static void SaveRoomState(int roomNumber, List<Enemy> currentEnemies, List<Gold> currentGolds)
        {
            if (!isInitialized) return;

            // Form1'deki güncel listelerin klonunu hafızaya yazıyoruz ki referans çelişkisi bitsin
            allEnemies[roomNumber] = new List<Enemy>(currentEnemies);
            allGolds[roomNumber] = new List<Gold>(currentGolds);
        }

        public static void ResetWorld()
        {
            InitializeWorld();
        }
    }
}