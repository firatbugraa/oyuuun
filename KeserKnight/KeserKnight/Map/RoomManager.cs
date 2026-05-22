using System;
using System.Collections.Generic;
using System.Drawing;
using KeserKnight.Entity;

namespace KeserKnight.Map
{
    public class RoomManager
    {
        public int CurrentRoom { get; private set; } = 1;

        private readonly int mapWidth;
        private readonly int mapHeight;

        public RoomManager(int targetWidth, int targetHeight)
        {
            this.mapWidth = targetWidth;
            this.mapHeight = targetHeight;
        }

        // Oyun resetlendiğinde odayı başa sarmak için
        public void Reset()
        {
            CurrentRoom = 1;
        }

        // Oda geçiş sınırlarını denetleyen ve haritayı güncelleyen ana fonksiyon
        public void Update(Player player, List<Rectangle> platforms, List<Enemy> enemies, List<Gold> roomGolds)
        {
            // --- SAĞDAN ODA GEÇİŞ KONTROLÜ ---
            if (player.X > mapWidth)
            {
                CurrentRoom++;
                player.X = 1; // Sol köşeden yeni odaya giriş yapar
                GameMap.LoadRoom(CurrentRoom, platforms, enemies, roomGolds);
            }
            // --- SOLDAN ODA GEÇİŞ KONTROLÜ ---
            else if (player.X + player.Width < 0)
            {
                if (CurrentRoom > 1)
                {
                    CurrentRoom--;
                    player.X = mapWidth - player.Width - 1; // Sağ köşeden eski odaya geri giriş yapar
                    GameMap.LoadRoom(CurrentRoom, platforms, enemies, roomGolds);
                }
                else
                {
                    player.X = 0; // İlk odadaysa sol duvara takılır, dışarı çıkamaz
                }
            }
        }
    }
}