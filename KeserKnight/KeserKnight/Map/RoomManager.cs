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
        // Geriye 'bool' döndürerek Form1'e "Oda değişti, ekranı tazeleyebilirsin" mesajı verir usta
        public bool Update(Player player, List<Rectangle> platforms, List<Enemy> enemies, List<Gold> roomGolds)
        {
            // --- SAĞDAN ODA GEÇİŞ KONTROLÜ ---
            if (player.X > mapWidth)
            {
                // Gitmeden önce şu anki odanın temizlenmiş halini hafızaya mühürle usta!
                GameMap.SaveRoomState(CurrentRoom, enemies, roomGolds);

                CurrentRoom++;
                player.X = 10;

                // Şimdi yeni odayı yükle
                GameMap.LoadRoom(CurrentRoom, platforms, enemies, roomGolds);
                return true;
            }

            // --- SOLDAN ODA GEÇİŞ KONTROLÜ ---
            else if (player.X + player.Width < 0)
            {
                if (CurrentRoom > 1)
                {
                    // Gitmeden önce şu anki odanın temizlenmiş halini hafızaya mühürle usta!
                    GameMap.SaveRoomState(CurrentRoom, enemies, roomGolds);

                    CurrentRoom--;
                    player.X = mapWidth - player.Width - 15;

                    // Şimdi eski odayı yükle (Bıraktığımız gibi gelecek!)
                    GameMap.LoadRoom(CurrentRoom, platforms, enemies, roomGolds);
                    return true;
                }
                else
                {
                    player.X = 0;
                }
            }

            return false;
        }

    }
}